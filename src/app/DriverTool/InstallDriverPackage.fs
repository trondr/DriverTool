namespace DriverTool

module InstallDriverPackage =
    open System
    open InstallXml
    open DriverTool.PackageDefinition
    open DriverTool.Requirements
    open Microsoft.FSharp.Core.Operators

    let logger = Logging.getLoggerByName("InstallDriverPackage")
        
    let getInstallXmlPath (driverPackagePath:FileSystem.Path) =
        FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue driverPackagePath,"Install.xml"))

    type SystemInfo={Model:string; OperatingSystem:string}
    
    let getSystemInfo =
        result{
            let! currentComputerModel = DriverTool.SystemInfo.getModelCodeForCurrentSystem()
            let currentOsShortName = OperatingSystem.getOsShortName
            return {Model=currentComputerModel;OperatingSystem=currentOsShortName}
        }

    let assertIsSupported (installConfiguration:InstallConfigurationData) (systemInfo: SystemInfo) = 
        result {
            let isSupportedComputerModel = systemInfo.Model.ToUpper().StartsWith(installConfiguration.ComputerModel.ToUpper())
            let isSupportedOperatingSystem = systemInfo.OperatingSystem.ToUpper().Equals(installConfiguration.OsShortName.ToUpper())
            let supportedCheckArray = 
                [|
                    (isSupportedComputerModel, sprintf "Computer model '%s' is not supported by this driver package. Supported model: '%s'." systemInfo.Model  installConfiguration.ComputerModel);
                    (isSupportedOperatingSystem,sprintf "Operating system '%s' is not supported by this driver package. Supported operating system: '%s'." systemInfo.OperatingSystem installConfiguration.OsShortName);
                |]
            let notSupportedMessageArray =
                supportedCheckArray
                |>Seq.filter(fun (isSupported,_)-> not isSupported)
                |>Seq.map (fun (_,message)-> message)
                |>Seq.toArray
            let isSupportedResult =
                match notSupportedMessageArray.Length with
                |0 -> Result.Ok true
                |_ -> Result.Error (new Exception(String.Join(" ",notSupportedMessageArray)))
            return! isSupportedResult
       }
    
    let unRegisterSccmApplication (installConfiguration:InstallConfigurationData) =        
        let applicationRegistryValue = (getApplicationRegistryValue installConfiguration)
        logger.Info("Unregister application: " + applicationRegistryValue.Path)
        match (DriverTool.RegistryOperations.regKeyExists applicationRegistryValue.Path) with
        | true -> DriverTool.RegistryOperations.deleteRegKey applicationRegistryValue.Path
        | _ -> ()

    let registerSccmApplication (installConfiguration:InstallConfigurationData) =        
        let applicationRegistryValue = (getApplicationRegistryValue installConfiguration)
        logger.Info("Register application: " + applicationRegistryValue.Path)
        use regKey = DriverTool.RegistryOperations.createRegKey applicationRegistryValue.Path
        regKey.SetValue(applicationRegistryValue.ValueName,applicationRegistryValue.Value)
    
    let getDriverPackageName (installConfiguration:InstallConfigurationData) =
        sprintf "%s_%s_%s" installConfiguration.ComputerVendor installConfiguration.ComputerModel installConfiguration.OsShortName
    
    let windowsFolder =
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows)

    let getLocalDriversPackageFolder driverPackageName =
        System.IO.Path.Combine(windowsFolder,"Drivers", driverPackageName)
    
    let copyDrivers (driverPackagePath:FileSystem.Path, destinationDriversFolderPath:FileSystem.Path) = 
        result{
            let! sourceDriversFolderPath = PathOperations.combine2Paths(FileSystem.pathValue driverPackagePath,"Drivers")            
            let! sourceDriversZipFilePath = PathOperations.combine2Paths(FileSystem.pathValue driverPackagePath,"Drivers.zip")            
            let! copyResult =   
                match (System.IO.Directory.Exists(FileSystem.pathValue sourceDriversFolderPath)) with
                |true ->
                    logger.Info("Copy Drivers folder: " + FileSystem.pathValue sourceDriversFolderPath)
                    result{
                        let! robocopyResult = Robocopy.roboCopy (sourceDriversFolderPath,destinationDriversFolderPath,"*.* /MIR /NP")
                        return robocopyResult
                    }
                            
                |false ->
                    result{                        
                        let! existingSourceDriversZipFilePath = FileOperations.ensureFileExists sourceDriversZipFilePath
                        logger.Info("Unzip Drivers.zip: " + FileSystem.pathValue existingSourceDriversZipFilePath)
                        let! nonExistingDestinationDriversFolderPath = DirectoryOperations.deleteDirectory true destinationDriversFolderPath
                        let! unzipResult = Compression.unzipFile (existingSourceDriversZipFilePath, destinationDriversFolderPath)
                        return unzipResult                    
                    }
            return copyResult
        }

    let getGetActiveDriverFolders driversFolderPath =
        result{
            let! subDirectoryPaths = DirectoryOperations.getSubDirectoryPaths driversFolderPath
            let activeDriverFolders =
                    subDirectoryPaths
                    |>Seq.filter (fun path-> not ((FileSystem.pathValue path).StartsWith("_")))
                    |>Seq.toArray
            return activeDriverFolders
        }
     
    let getGetInActiveDriverFolders driversFolderPath =
        result{
            let! subDirectoryPaths = DirectoryOperations.getSubDirectoryPaths driversFolderPath
            let activeDriverFolders =
                    subDirectoryPaths
                    |>Seq.filter (fun path-> ((FileSystem.pathValue path).StartsWith("_")))
                    |>Seq.toArray
            return activeDriverFolders
        }

    let getExistingScripts (activeDriverFolders:FileSystem.Path[], installScriptName) =
        activeDriverFolders
                |> Seq.map (fun p -> PathOperations.combine2Paths(FileSystem.pathValue p,installScriptName))
                |> Seq.map (fun spr -> 
                                match spr with
                                |Ok p -> FileOperations.ensureFileExistsWithMessage (sprintf "It is required that the script '%s' exists in each active driver folder. Not found: %s. " installScriptName (FileSystem.pathValue p)) p
                                |Error _ -> spr
                            )
                |> Seq.toArray
                |> toAccumulatedResult

    let executeScripts (existingInstallScripts:seq<FileSystem.Path>,installScriptName,installConfiguration,driverPackageName)  =
        result{
            let! cmdExePath = PathOperations.combine2Paths (Environment.nativeSystemFolder,"cmd.exe")
            let! existingCmdExePath = FileOperations.ensureFileExists cmdExePath
            let! installedDriverExitCodes =
                existingInstallScripts
                |>Seq.map(fun script ->
                        let parentDirectory = (new System.IO.FileInfo(FileSystem.pathValue script)).Directory
                        let scriptLogFile = System.IO.Path.Combine(System.Environment.ExpandEnvironmentVariables(installConfiguration.LogDirectory),sprintf "%s-%s-%s.log" installScriptName driverPackageName (parentDirectory.Name.Replace(".","_")))                        
                        ProcessOperations.startConsoleProcess (existingCmdExePath,sprintf "/c \"%s\"" (FileSystem.pathValue script),parentDirectory.FullName,-1,null,scriptLogFile,true)
                    )
                |>Seq.toArray
                |>toAccumulatedResult
            return installedDriverExitCodes
        }   
        
    let getAdjustedExitCode installedDriverExitCodes =
        let exitCodeSum = installedDriverExitCodes |> Seq.map abs |> Seq.sum
        match exitCodeSum with
        | ec when ec > 0 -> 3010
        | _ -> 0

    let installDrivers (localDriversFolderPath:FileSystem.Path) installScriptName installConfiguration driverPackageName =
        result{            
            let! existingLocalDriversFolderPath = DirectoryOperations.ensureDirectoryExistsWithMessage false (sprintf "Can not run install scripts '%s' due to the local driver folder '%s', where the scripts should be located, does not exist. " installScriptName (FileSystem.pathValue localDriversFolderPath)) localDriversFolderPath
            
            logger.Info("Getting active drivers...")
            let! activeDriverFolders = getGetActiveDriverFolders existingLocalDriversFolderPath    
            activeDriverFolders |> DriverTool.Logging.logSeqWithFormatString logger  (sprintf "Will be processed: %s")|>ignore
            
            logger.Info("Getting inactive drivers...")
            let! inactiveDriverFolders = getGetInActiveDriverFolders existingLocalDriversFolderPath
            inactiveDriverFolders |> DriverTool.Logging.logSeqWithFormatString logger  (sprintf "Will NOT be processed: %s")|>ignore
            
            logger.Info("Verifying that active scripts exists...")
            let! existingInstallScripts = getExistingScripts (activeDriverFolders, installScriptName)
            existingInstallScripts |> DriverTool.Logging.logSeqWithFormatString logger (sprintf "Script verified: %s")|>Seq.toArray|>ignore

            logger.Info(sprintf "Executing '%s' for each driver folder..." installScriptName)
            let! installedDriverExitCodes = executeScripts (existingInstallScripts,installScriptName,installConfiguration,driverPackageName)
            existingInstallScripts |>Seq.zip installedDriverExitCodes |> DriverTool.Logging.logSeqWithFormatString logger (sprintf "Script execution result: %s") |> ignore
            logger.Info(sprintf "Finished executing '%s' for each driver folder!" installScriptName)
            let adjustedExitCode = getAdjustedExitCode installedDriverExitCodes
            logger.Info(sprintf "Adjusted exit code: %i" adjustedExitCode)
            return adjustedExitCode
        }
    
    let assertDriverInstallRequirements installConfiguration systemInfo =
        result{
                logger.Info("Checking if driver package is supported...")
                let! isSupported = assertIsSupported installConfiguration systemInfo
                logger.Info(sprintf "Driver package is supported: %b" isSupported)
                let! isAdministrator = assertIsAdministrator "Administrative privileges are required. Please run driver package install from an elevated command prompt."
                logger.Info(sprintf "Installation is running with admin privileges: %b" isAdministrator)
                let! isRunningNativeProcess = assertIsRunningNativeProcess (sprintf "Driver install must be run in native process (64-bit on a x64 operating system, 32-bit on a x86 operating system). The current process is %s. Contact the developer or use CoreFlags.exe (in the .NET SDK) to change the prefered execution bit on the current assembly." Environment.processBit)
                logger.Info(sprintf "Installation is running in native process: {%b} (%s)" isRunningNativeProcess Environment.processBit)
                return (isSupported && isAdministrator && isRunningNativeProcess)
        }
    
    let resetConfigFlagsUnsafe (_:unit) =
        logger.Info("Reset all ConfigFlag's having value 131072 to 0. This will avoid UAC prompts due driver initialization at standard user logon.")
        let regKeyPath = @"HKLM\SYSTEM\CurrentControlSet\Enum"
        DriverTool.RegistryOperations.getRegistrySubKeyPaths regKeyPath true
        |> Seq.filter(fun p -> (DriverTool.RegistryOperations.regValueExists p "ConfigFlags"))
        |> Seq.filter(fun p -> (DriverTool.RegistryOperations.regValueIs p "ConfigFlags" 131072))
        |> Seq.map (fun p ->
                        //The ConfigFlag value 131072 signals a driver initialization, 
                        //which we do not want for a standard user user at logon, so set 
                        //ConfigFlags to 0                        
                        (DriverTool.RegistryOperations.setRegValue p "ConfigFlags" 0) |> ignore
                        logger.Info(sprintf "ConfigFlag value in '[%s]' was reset to 0." p)
                    )
        |>Seq.toArray
        |>ignore
        ()
    
    let resetConfigFlags (): Result<unit,Exception> =
        tryCatchWithMessage resetConfigFlagsUnsafe (()) "Failed to reset config flags."

    let installDriverPackage (driverPackagePath:FileSystem.Path) =
        result{
            let! installXmlPath = getInstallXmlPath driverPackagePath
            let! installConfiguration = InstallXml.loadInstallXml installXmlPath
            let! systemInfo = getSystemInfo
            let! requirementsAreFullfilled = assertDriverInstallRequirements installConfiguration systemInfo
            logger.Info(sprintf "All install requirements are fullfilled: %b" requirementsAreFullfilled)            
            let unregisterSccmApplication = unRegisterSccmApplication installConfiguration
            let! bitLockerSuspendExitCode = DriverTool.BitLockerOperations.suspendBitLockerProtection()            
            let driverPackageName = getDriverPackageName installConfiguration
            let localDriversFolder = getLocalDriversPackageFolder driverPackageName
            let! localDriversFolderPath = FileSystem.path localDriversFolder
            let! copyResult = copyDrivers (driverPackagePath, localDriversFolderPath)
            let! installDriversExitCode = installDrivers localDriversFolderPath DriverTool.CreateDriverPackage.dtInstallPackageCmd installConfiguration driverPackageName 
            let registerApplication =
                match installDriversExitCode with
                |0|3010 -> 
                    registerSccmApplication installConfiguration
                    ()
                |_ -> ()
            let! resetConfigFlagsResult = resetConfigFlags ()            
            return installDriversExitCode
        }
        
    let unInstallDriverPackage (driverPackagePath:FileSystem.Path) =
        result{
            let! installXmlPath = getInstallXmlPath driverPackagePath
            let! installConfiguration = InstallXml.loadInstallXml installXmlPath
            let! systemInfo = getSystemInfo
            let! requirementsAreFullfilled = assertDriverInstallRequirements installConfiguration systemInfo
            logger.Info(sprintf "All install requirements are fullfilled: %b" requirementsAreFullfilled)
            let driverPackageName = getDriverPackageName installConfiguration
            let localDriversFolder = getLocalDriversPackageFolder driverPackageName
            let! localDriversFolderPath = FileSystem.path localDriversFolder
            let! copyResult = copyDrivers (driverPackagePath, localDriversFolderPath)
            let! uninstallDriversExitCode = installDrivers localDriversFolderPath DriverTool.CreateDriverPackage.dtUnInstallPackageCmd installConfiguration driverPackageName
            let! nonExistingLocalDriversFolderPath = DirectoryOperations.deleteDirectory true localDriversFolderPath
            let registerApplication =
                match uninstallDriversExitCode with
                |0|3010 -> 
                    unRegisterSccmApplication installConfiguration
                    ()
                |_ -> ()                        
            return uninstallDriversExitCode
        }
