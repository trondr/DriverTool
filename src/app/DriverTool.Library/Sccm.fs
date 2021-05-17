namespace DriverTool.Library

module Sccm =
    open System
    open System.Collections.Generic
    open DriverTool.Library.PowerShellHelper 

    ///Get assigned SCCM site
    let getAssignedSite () =
        let script = 
                [|
                    "$SCCMSiteCode = New-Object -ComObject \"Microsoft.SMS.Client\""
                    "$SCCMSiteCode.GetAssignedSite()"
                |]|>String.concat Environment.NewLine
        getFirstStringValue script
    
    let getSiteServer () =
        let script = 
            [|
                "$SCCMSiteCode = New-Object -ComObject \"Microsoft.SMS.Client\""
                "$SCCMSiteCode.GetCurrentManagementPoint()"
            |]|>String.concat Environment.NewLine
        getFirstStringValue script

    ///Load ConfigurationManager module
    let loadCmPowerShellModule () = 
        raise (new NotImplementedException())

    ///Build CM PowerShell startup script
    let cmPowerShellStartupScript siteCode siteServer =
        seq{             
            yield sprintf "$SiteCode=\"%s\"" siteCode
            yield sprintf "$SiteServer=\"%s\"" siteServer
            yield "$initParams = @{}"
            if(logger.IsDebugEnabled) then
                yield "$initParams.Add(\"Verbose\", $true)"
                yield "$initParams.Add(\"ErrorAction\", \"Stop\")"
            yield "if((Get-Module ConfigurationManager) -eq $null) {"
            yield "    Import-Module \"$($ENV:SMS_ADMIN_UI_PATH)\..\ConfigurationManager.psd1\" @initParams"
            yield "}"
            yield "if((Get-PSDrive -Name $SiteCode -PSProvider CMSite -ErrorAction SilentlyContinue) -eq $null) {"
            yield "New-PSDrive -Name $SiteCode -PSProvider CMSite -Root $SiteServer @initParams"
            yield "}"
            yield "Set-Location \"$($SiteCode):\\\" @initParams"            
        }
        |>Seq.toArray
        |>String.concat Environment.NewLine


    open System.Management.Automation
    open System.Management.Automation.Runspaces
    open System.Collections.ObjectModel

    let createRunSpaceUnsafe startupScript =
        let initialSessionState = System.Management.Automation.Runspaces.InitialSessionState.CreateDefault2()
        initialSessionState.StartupScripts.Add(startupScript)|> ignore        
        let runspace = RunspaceFactory.CreateRunspace(initialSessionState)
        runspace.Open()
        use pipeline = runspace.CreatePipeline()
        let preparedStartupScript = 
            startupScript
            |> removeComments
            |> addTranscriptLogging
        pipeline.Commands.AddScript(preparedStartupScript)
        pipeline.Invoke() |> toSeq |> Seq.toArray |>ignore
        runspace

    let createRunSpace startupScript =
        tryCatch (Some (sprintf "Failed to create PowerShell runspace.")) createRunSpaceUnsafe startupScript

    let runPowerShellScriptInRunspaceUnsafe (runspace:Runspace) script (variables:Dictionary<string,obj>) =        
        use pipeline = runspace.CreatePipeline()
        let preparedScript = script|> removeComments
        pipeline.Commands.AddScript(preparedScript)
        variables |> Seq.map (fun kvp -> 
                runspace.SessionStateProxy.SetVariable(kvp.Key,kvp.Value)
            ) |> Seq.iter id
        let output = pipeline.Invoke() |> toSeq |> Seq.toArray        
        output
    
    let runPowerShellScriptInRunspace (runspace:Runspace) script (variables:Dictionary<string,obj>) =
        tryCatch3 (Some "Failed to run PowerShell script.") runPowerShellScriptInRunspaceUnsafe runspace script variables

    type CMPowerShellSession private () =
        let mutable runSpace :Runspace = null
        static let instance = CMPowerShellSession()
        static member Instance = instance        
        member this.RunSpace
            with get() =
                match runSpace with
                |null ->
                    result{
                        let! siteCode = getAssignedSite()
                        let! siteServer = getSiteServer()
                        let startupScript = cmPowerShellStartupScript siteCode siteServer
                        let! rs = createRunSpace startupScript
                        runSpace <- rs                        
                        return runSpace
                    }
                |_ -> Result.Ok runSpace
            
        member this.RunScriptEx script (variables:Dictionary<string,obj>) =
            result{
                let! runSpace = this.RunSpace
                let! output = runPowerShellScriptInRunspace runSpace script variables
                return output
            }

        member this.RunScript script =
            result{
                let! runSpace = this.RunSpace
                let variables = new Dictionary<string, obj>()
                let! output = runPowerShellScriptInRunspace runSpace script variables
                return output
            }

    open DriverTool.Library.PackageDefinitionSms

    ///Check if package exists
    let cmPackageExists packageName =
        match(result{                        
            let! out = CMPowerShellSession.Instance.RunScript (sprintf "Get-CMPackage -Name '%s' -Fast" packageName)
            return out            
        })with
        |Result.Ok packages -> (packages.Length > 0)
        |Result.Error ex ->
            logger.Warn(sprintf "Failed to check if package '%s' exists in Configuration Manager due to: %s" packageName (getAccumulatedExceptionMessages ex))
            false

    let ensureCmPackageExists packageName =
        match(cmPackageExists packageName)with
        |true -> Result.Ok true
        |false -> Result.Error (toException (sprintf "CM Package does not exist: %s" packageName) None)

    let ensureCmPackageDoesNotExist packageName =
        match(cmPackageExists packageName)with
        |true -> Result.Error (toException (sprintf "CM Package allready exist: %s" packageName) None)
        |false -> Result.Ok true

    ///Build New-CMProgram PowerShell command
    let toNewCmProgramPSCommand packageId (program:SmsProgram) =        
        let newCMProgramPSCommand =
            seq{
                yield "New-CMProgram"
                yield (sprintf "-PackageId \"%s\"" packageId)
                yield (sprintf "-StandardProgramName '%s'" (WrappedString.value program.Name))
                yield (sprintf "-CommandLine '%s'" (WrappedString.value program.Commandline))
                yield   match (WrappedString.value program.StartIn) with
                        |"" -> ""
                        |_ -> (sprintf "-WorkingDirectory '%s'" (WrappedString.value program.StartIn))
                yield (sprintf "-UserInteraction:$%b" (program.UserInputRequired))
                yield   match program.AdminRightsRequired with
                        |true -> "-RunMode RunWithAdministrativeRights"
                        |false -> "-RunMode RunWithUserRights"
                yield   match program.Run with
                        |Some runType -> 
                            (sprintf "-RunType %s" (smsProgramModeToString runType))
                        |None -> "-RunType Normal"
                yield   match program.CanRunWhen with
                        |AnyUserStatus -> "-ProgramRunType WhetherOrNotUserIsLoggedOn"
                        |NoUserLoggedOn -> "-ProgramRunType OnlyWhenNoUserIsLoggedOn"
                        |UserLoggedOn -> "-ProgramRunType OnlyWhenUserIsLoggedOn"
                
                yield   match program.DriveLetterConnection with
                        |true -> 
                            match program.SpecifyDrive with
                            |Some drive ->
                                match program.ReconnectDriveAtLogon with
                                |true -> (sprintf "-DriveMode RequiresSpecificDriveLetter -DriveLetter '%s' -Reconnect" drive)
                                |false -> (sprintf "-DriveMode RequiresSpecificDriveLetter -DriveLetter '%s'" drive)
                            |None ->
                                match program.ReconnectDriveAtLogon with
                                |true -> "-DriveMode RequiresDriveLetter -Reconnect"
                                |false -> "-DriveMode RequiresDriveLetter"
                        |false -> "-DriveMode RunWithUnc"
                
                match program.Comment with
                |Some c -> yield (sprintf "| Set-CMProgram -Comment '%s' -StandardProgram" (WrappedString.value c))
                |None -> yield String.Empty
            }
            |>Seq.toArray
            |>Seq.filter (fun s -> not (String.IsNullOrWhiteSpace(s)))
            |>String.concat " "
        newCMProgramPSCommand

    ///Build New-CMPackage PowerShell command
    let toCmPackagePSCommand (packageDefinition:SmsPackageDefinition) sourceFolderPath =
        let newCMPackagePSCommand =
            seq{
                yield "New-CMPackage"
                yield (sprintf "-Name '%s'" (WrappedString.value packageDefinition.Name))
                yield (sprintf "-Description '%s'" (WrappedString.value packageDefinition.Comment))
                yield (sprintf "-Path '%s'" (sourceFolderPath))
                yield   match packageDefinition.Version with
                        |Some v -> (sprintf "-Version '%s'" (WrappedString.value v))
                        |None -> String.Empty
                yield (sprintf "-Manufacturer '%s'" (WrappedString.value packageDefinition.Publisher))
                yield (sprintf "-Language '%s'" (WrappedString.value packageDefinition.Language))
            }
            |>Seq.toArray
            |>String.concat " "
        newCMPackagePSCommand

    ///Create SCCM package from package definition sms file
    let createPackageFromDefinition sourceFolderPath packageDefinition =
        result{                        
            let! ensurePackageNotAllreadyExists = ensureCmPackageDoesNotExist (WrappedString.value packageDefinition.Name)
            let script = 
                seq{                    
                    yield (sprintf "$package = %s" (toCmPackagePSCommand packageDefinition (FileSystem.pathValue sourceFolderPath)))
                    yield ("$package | Set-CMPackage -EnableBinaryDeltaReplication $true")
                    for program in packageDefinition.Programs do
                        yield toNewCmProgramPSCommand "$($package.PackageId)" program
                }
                |>Seq.toArray
                |>String.concat Environment.NewLine             
            let! out = CMPowerShellSession.Instance.RunScript script
            return out
        }

    ///Check if task sequence exists
    let cmTaskSequenceExists name =
        match(result{            
            let! out = CMPowerShellSession.Instance.RunScript (sprintf "Get-CMTaskSequence -Name '%s'" name)
            return out            
        })with
        |Result.Ok packages -> (packages.Length > 0)
        |Result.Error ex ->
            logger.Warn(sprintf "Failed to check if task sequence '%s' exists in Configuration Manager due to: %s" name (getAccumulatedExceptionMessages ex))
            false

    let ensureCmTaskSequenceExists name =
        match(cmTaskSequenceExists name)with
        |true -> Result.Ok true
        |false -> Result.Error (toException (sprintf "CM Task Sequence does not exist: %s" name) None)

    let ensureCmTaskSequenceDoesNotExist name =
        match(cmTaskSequenceExists name)with
        |true -> Result.Error (toException (sprintf "CM Task Sequence allready exist: %s" name) None)
        |false -> Result.Ok true

    ///Check if package definition has a program with name as defined by parameter programName
    let packageDefinitionHasProgramName programName packageDefition =
        packageDefition.Programs
        |>Array.filter(fun program -> (WrappedString.value program.Name) = programName)
        |>Array.tryHead
        |>optionToBoolean

    /// Build PowerShell script to create custom task sequence from package definitions. The program name must exist in all package definitions. Example: toCustomTaskSequenceScript "TS Driver Install" "Install device drivers" "INSTALL-OFFLINE-OS" packageDefinitions
    let toCustomTaskSequenceScript name description programName packageDefinitions =
        result{
            
            let! ensureThatAllPackageDefinitionsContainsProgramName = 
                packageDefinitions
                |>Array.map (fun p-> (p,packageDefinitionHasProgramName programName p))
                |>Array.map(fun (package,hasProgramName) -> booleanToResult (sprintf "Package '%s' does not have program name '%s'. Unable to create custom task sequence '%s'." (WrappedString.value package.Name) programName name) hasProgramName)
                |>toAccumulatedResult
            let ensureThatAllPackageDefinitionsContainsProgramName = ensureThatAllPackageDefinitionsContainsProgramName|>Seq.toArray|>Array.forall(fun p->p)
            logger.Info(sprintf "All packages has program '%s': %b" programName ensureThatAllPackageDefinitionsContainsProgramName)

            let uniqueManufacturerWmiQueries = 
                packageDefinitions
                |>Seq.map(fun package ->package.ManufacturerWmiQuery)                
                |>Seq.distinctBy(fun m-> m.Name)
                |>Seq.toArray

            let script =
                seq{
                    yield "$ManufacturerGroups = @()"
                    for wmiQuery in uniqueManufacturerWmiQueries do                        
                        let vendorPackages = packageDefinitions|>Array.filter(fun p -> p.ManufacturerWmiQuery.Name = wmiQuery.Name)
                        yield "$ModelGroups = @()"
                        for package in vendorPackages do
                            yield sprintf "$package = Get-CMPackage -Name '%s' -Fast" (WrappedString.value package.Name)
                            let installProgram = package.Programs|>Array.filter(fun program -> (WrappedString.value program.Name) = programName) |>Array.head
                            yield   match installProgram.Comment with
                                    |Some c -> 
                                        sprintf "$commandLineStep = New-CMTSStepRunCommandLine -PackageId $($Package.PackageID) -Name \"%s\" -CommandLine '%s' -SuccessCode @(0,3010) -Description \"%s\"" (WrappedString.value package.Name) (WrappedString.value installProgram.Commandline) (WrappedString.value c)
                                    |None ->
                                        sprintf "$commandLineStep = New-CMTSStepRunCommandLine -PackageId $($Package.PackageID) -Name \"%s\"  -CommandLine '%s' -SuccessCode @(0,3010)" (WrappedString.value package.Name) (WrappedString.value installProgram.Commandline)
                            yield "$restartStep = New-CMTSStepReboot -Name \"Restart\" -RunAfterRestart \"HardDisk\" -NotificationMessage \"A new Microsoft Windows operating system is being installed. The computer must restart to continue.\" -MessageTimeout 3"
                            
                            yield sprintf "$ModelGroupCondition = New-CMTSStepConditionQueryWMI -Namespace \"%s\" -Query \"%s\"" package.ModelWmiQuery.NameSpace package.ModelWmiQuery.Query
                            yield sprintf "$ModelGroups += New-CMTaskSequenceGroup -Name '%s' -Condition @($ModelGroupCondition) -Step @($commandLineStep,$restartStep)" package.ModelWmiQuery.Name

                        yield sprintf "$GroupCondition = New-CMTSStepConditionQueryWMI -Namespace \"%s\" -Query \"%s\"" wmiQuery.NameSpace wmiQuery.Query
                        yield sprintf "$ManufacturerGroups += New-CMTaskSequenceGroup -Name '%s' -Description 'Manufacturer %s' -Condition @($GroupCondition) -Step @($ModelGroups)" wmiQuery.Name wmiQuery.Name
                    yield "$ApplyDriversGroup = New-CMTaskSequenceGroup -Name 'Apply Drivers' -Description 'Apply drivers to the offline operating system.' -Step @($ManufacturerGroups)"
                    yield sprintf "$taskSequence = New-CMTaskSequence -CustomTaskSequence -Name '%s' -Description '%s'" name description
                    yield sprintf "Add-CMTaskSequenceStep -TaskSequenceName '%s' -Step @($ApplyDriversGroup)" name
                }
                |>Seq.toArray
                |>String.concat Environment.NewLine
            return script        
        }

    ///Create custom task sequence from package definitions.
    let createCustomTaskSequence name description programName (packageDefinitionSmsFilePaths:FileSystem.Path[]) =
        result{            
            let! ensureCustomTaskSequenceNotAllreadyExists = ensureCmTaskSequenceDoesNotExist name
            logger.Info(sprintf "Task sequence '%s' does not allready exists: %b" name ensureCustomTaskSequenceNotAllreadyExists)
            let! packageDefinitions = 
                packageDefinitionSmsFilePaths
                |>Array.map PackageDefinitionSms.readFromFile
                |>toAccumulatedResult
            let packageDefinitions = packageDefinitions |> Seq.toArray
            let! ensureCmPackagesExists =
                packageDefinitions                
                |>Array.map (fun p -> ensureCmPackageExists (WrappedString.value p.Name))
                |>toAccumulatedResult
            logger.Info(sprintf "All packages exist: %b" (ensureCmPackagesExists|>Seq.forall(fun p -> p)))            
            let! script = toCustomTaskSequenceScript name description programName packageDefinitions                
            let! out = CMPowerShellSession.Instance.RunScript script
            return out
        }
     