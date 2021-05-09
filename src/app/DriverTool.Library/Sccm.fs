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
                |]|>linesToText
        getFirstStringValue script
    
    let getSiteServer () =
        let script = 
            [|
                "$SCCMSiteCode = New-Object -ComObject \"Microsoft.SMS.Client\""
                "$SCCMSiteCode.GetCurrentManagementPoint()"
            |]|>linesToText
        getFirstStringValue script

    ///Load ConfigurationManager module
    let loadCmPowerShellModule () = 
        raise (new NotImplementedException())

    ///Load ConfigurationManager module and run CM powershell script
    let runCmPowerShellScript' script siteCode siteServer =
        result{
            let cmScript = 
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
                    yield script
                }
                |>Seq.toArray
                |> linesToText
            let variables = new Dictionary<string, obj>()
            let! out = PowerShellHelper.runPowerShellScript cmScript variables
            return out        
        }

    let runCmPowerShellScript script =
        result{
            let! siteCode = getAssignedSite()
            let! siteServer = getSiteServer()
            let! out = runCmPowerShellScript' script siteCode siteServer
            return out
        }

    open DriverTool.Library.PackageDefinitionSms

    ///Check if package exists
    let cmPackageExists packageName =
        match(result{            
            let! out = runCmPowerShellScript (sprintf "Get-CMPackage -Name '%s' -Fast" packageName)
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
                |>linesToText            
            let! out = runCmPowerShellScript script
            return out        
        }

    ///Check if task sequence exists
    let cmTaskSequenceExists name =
        match(result{            
            let! out = runCmPowerShellScript (sprintf "Get-CMTaskSequence -Name '%s'" name)
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


    ///Create custom task sequence from package definitions.
    let createCustomTaskSequence name description (packageDefinitionSmsFilePaths:FileSystem.Path[]) =
        result{
            let! ensureCustomTaskSequenceNotAllreadyExists = ensureCmTaskSequenceDoesNotExist name
            let! packageDefinitions = 
                packageDefinitionSmsFilePaths
                |>Array.map(fun f -> 
                        result{
                            let! sourceFolderPath = FileOperations.getParentPath f
                            let! pdf = PackageDefinitionSms.readFromFile f
                            return (sourceFolderPath,pdf)
                        }                        
                    )
                |>toAccumulatedResult
            let! ensureCmPackagesExists =
                packageDefinitions
                |>Seq.toArray
                |>Array.map (fun (_,p) -> ensureCmPackageExists (WrappedString.value p.Name))
                |>toAccumulatedResult
            
            let script =
                seq{
                    yield sprintf "$taskSequence = New-CMTaskSequence -CustomTaskSequence -Name '%s' -Description '%s'" name description
                }
                |>Seq.toArray
                |>String.concat Environment.NewLine
            let! out = runCmPowerShellScript script
            return out

        }

        //New-CMTaskSequence
        //[-BootImagePackageId <String>]
        //[-CustomTaskSequence]
        //[-Description <String>]
        //[-HighPerformance <Boolean>]
        //-Name <String>
        //[-DisableWildcardHandling]
        //[-ForceWildcardHandling]
        //[-WhatIf]
        //[-Confirm]
        //[<CommonParameters>]
        