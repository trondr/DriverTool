﻿namespace DriverTool

module InstallDriverPackage =
    let logger = Logging.getLoggerByName("InstallDriverPackage")
    
    open System
    open InstallXml
        
    let getInstallXmlPath (driverPackagePath:Path) =
        Path.create (System.IO.Path.Combine(driverPackagePath.Value,"Install.xml"))

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
                    (isSupportedComputerModel, String.Format("Computer model '{0}' is not supported by this driver package. Supported model: '{1}'.", systemInfo.Model, installConfiguration.ComputerModel));
                    (isSupportedOperatingSystem,String.Format("Operating system '{0}' is not supported by this driver package. Supported operating system: '{1}'.", systemInfo.OperatingSystem, installConfiguration.OsShortName));
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

    open DriverTool.RegistryOperations
    open DriverTool.PackageDefinition
    
    let unRegisterSccmApplication (installConfiguration:InstallConfigurationData) =        
        let applicationRegistryValue = (getApplicationRegistryValue installConfiguration)
        logger.Info("Unregister application: " + applicationRegistryValue.Path)
        match (regKeyExists applicationRegistryValue.Path) with
        | true -> deleteRegKey applicationRegistryValue.Path
        | _ -> ()

    let registerSccmApplication (installConfiguration:InstallConfigurationData) =        
        let applicationRegistryValue = (getApplicationRegistryValue installConfiguration)
        logger.Info("Register application: " + applicationRegistryValue.Path)
        use regKey = createRegKey applicationRegistryValue.Path
        regKey.SetValue(applicationRegistryValue.ValueName,applicationRegistryValue.Value)
    
    open DriverTool.BitLockerOperations
    
    let getDriverPackageName (installConfiguration:InstallConfigurationData) =
        String.Format("{0}_{1}_{2}",installConfiguration.ComputerVendor,installConfiguration.ComputerModel,installConfiguration.OsShortName)
    
    let windowsFolder =
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows)

    let getLocalDriversPackageFolder driverPackageName =
        System.IO.Path.Combine(windowsFolder,"Drivers", driverPackageName)
    
    let copyDrivers (driverPackagePath:Path, destinationDriversFolderPath:Path) = 
        result{
            let! sourceDriversFolderPath = PathOperations.combine2Paths(driverPackagePath.Value,"Drivers")            
            let! sourceDriversZipFilePath = PathOperations.combine2Paths(driverPackagePath.Value,"Drivers.zip")            
            let copyResult =   
                match (System.IO.Directory.Exists(sourceDriversFolderPath.Value)) with
                |true ->
                    //Copy Drivers folder
                    result{
                        let! robocopyResult = Robocopy.roboCopy (sourceDriversFolderPath,destinationDriversFolderPath,"*.* /MIR /NP")
                        return robocopyResult
                    }
                            
                |false ->
                    result{
                        //Unzip Drivers.zip
                        let! existingSourceDriversZipFilePath = FileOperations.ensureFileExists sourceDriversZipFilePath
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
                    |>Seq.filter (fun path-> not (path.Value.StartsWith("_")))
                    |>Seq.toArray
            return activeDriverFolders
        }
     
    let getGetInActiveDriverFolders driversFolderPath =
        result{
            let! subDirectoryPaths = DirectoryOperations.getSubDirectoryPaths driversFolderPath
            let activeDriverFolders =
                    subDirectoryPaths
                    |>Seq.filter (fun path-> (path.Value.StartsWith("_")))
                    |>Seq.toArray
            return activeDriverFolders
        }

    let getExistingScripts (activeDriverFolders:Path[], installScriptName) =
        activeDriverFolders
                |> Seq.map (fun p -> PathOperations.combine2Paths(p.Value,installScriptName))
                |> Seq.map (fun spr -> 
                                match spr with
                                |Ok p -> FileOperations.ensureFileExistsWithMessage (String.Format("It is required that the script '{0}' exists in each active driver folder. Not found: {1}. ",installScriptName, p.Value)) p
                                |Error _ -> spr
                            )
                |> Seq.toArray
                |> toAccumulatedResult

    let executeScripts (existingInstallScripts:seq<Path>,installScriptName,installConfiguration,driverPackageName)  =
        result{
            let! cmdExePath = PathOperations.combine2Paths (Environment.nativeSystemFolder,"cmd.exe")
            let! existingCmdExePath = FileOperations.ensureFileExists cmdExePath
            let! installedDriverExitCodes =
                existingInstallScripts
                |>Seq.map(fun script ->
                        let parentDirectory = (new System.IO.FileInfo(script.Value)).Directory
                        let scriptLogFile = System.IO.Path.Combine(System.Environment.ExpandEnvironmentVariables(installConfiguration.LogDirectory),String.Format("{0}-{1}-{2}.log",installScriptName,driverPackageName,parentDirectory.Name.Replace(".","_")))                        
                        ProcessOperations.startConsoleProcess (existingCmdExePath.Value,String.Format("/c \"{0}\"", script.Value),parentDirectory.FullName,-1,null,scriptLogFile,true)
                    )
                |>Seq.toArray
                |>toAccumulatedResult
            return installedDriverExitCodes
        }   
        
    let getAdjustedExitCode installedDriverExitCodes =
        let exitCodeSum = installedDriverExitCodes |>Seq.sum
        match exitCodeSum with
        | ec when ec > 3010 -> 3010
        | ec -> ec

    let installDrivers (localDriversFolderPath:Path) installScriptName installConfiguration driverPackageName =
        result{            
            let! existingLocalDriversFolderPath = DirectoryOperations.ensureDirectoryExistsWithMessage (String.Format("Can not run install scripts '{0}' due to the local driver folder '{1}', where the scripts should be located, does not exist. ",installScriptName,localDriversFolderPath.Value),localDriversFolderPath,false)
            
            logger.Info("Getting active drivers...")
            let! activeDriverFolders = getGetActiveDriverFolders existingLocalDriversFolderPath    
            activeDriverFolders |> DriverTool.Logging.logSeqWithFormatString logger  "Will be processed: %s"|>ignore
            
            logger.Info("Getting inactive drivers...")
            let! inactiveDriverFolders = getGetInActiveDriverFolders existingLocalDriversFolderPath
            inactiveDriverFolders |> DriverTool.Logging.logSeqWithFormatString logger  "Will NOT be processed: %s"|>ignore
            
            logger.Info("Verifying that active scripts exists...")
            let! existingInstallScripts = getExistingScripts (activeDriverFolders, installScriptName)
            existingInstallScripts |> DriverTool.Logging.logSeqWithFormatString logger "Script verified: %s"|>Seq.toArray|>ignore

            logger.Info(String.Format("Executing '{0}' for each driver folder...", installScriptName))
            let! installedDriverExitCodes = executeScripts (existingInstallScripts,installScriptName,installConfiguration,driverPackageName)
            existingInstallScripts |>Seq.zip installedDriverExitCodes |> DriverTool.Logging.logSeqWithFormatString logger "Script execution result: %s" |> ignore
            logger.Info(String.Format("Finished executing '{0}' for each driver folder!", installScriptName))
            let adjustedExitCode = getAdjustedExitCode installedDriverExitCodes
            return adjustedExitCode
        }
    
    open DriverTool.Requirements

    let assertDriverInstallRequirements installConfiguration systemInfo =
        result{
                logger.Info("Checking if driver package is supported...")
                let! isSupported = assertIsSupported installConfiguration systemInfo
                logger.Info("Driver package is supported: " + isSupported.ToString())
                let! isAdministrator = assertIsAdministrator "Administrative privileges are required. Please run driver package install from an elevated command prompt."
                logger.Info("Installation is running with admin privileges: " + isAdministrator.ToString())
                let! isRunningNativeProcess = assertIsRunningNativeProcess (String.Format("Driver install must be run in native process (64-bit on a x64 operating system, 32-bit on a x86 operating system). The current process is {0}. Contact the developer or use CoreFlags.exe (in the .NET SDK) to change the prefered execution bit on the current assembly.", Environment.processBit))
                logger.Info(String.Format("Installation is running in native process: {0} ({1})",isRunningNativeProcess.ToString(), Environment.processBit))
                return (isSupported && isAdministrator && isRunningNativeProcess)
        }
    
    let resetConfigFlagsUnsafe (_:unit) =
        logger.Info("Reset all ConfigFlag's having value 131072 to 0. This will avoid UAC prompts due driver initialization at standard user logon.")
        let regKeyPath = @"HKLM\SYSTEM\CurrentControlSet\Enum"
        getRegistrySubKeyPaths regKeyPath true
        |> Seq.filter(fun p -> (regValueExists p "ConfigFlags"))
        |> Seq.filter(fun p -> (regValueIs p "ConfigFlags" 131072))
        |> Seq.map (fun p ->
                        //The ConfigFlag value 131072 signals a driver initialization, 
                        //which we do not want for a standard user user at logon, so set 
                        //ConfigFlags to 0                        
                        (setRegValue p "ConfigFlags" 0) |> ignore
                        logger.Info(String.Format("ConfigFlag value in '[{0}]' was reset to 0.",p))
                    )
        |>Seq.toArray
        |>ignore
        ()
    
    let resetConfigFlags (): Result<unit,Exception> =
        tryCatchWithMessage resetConfigFlagsUnsafe (()) "Failed to reset config flags."

    let installDriverPackage (driverPackagePath:Path) =
        result{
            let! installXmlPath = getInstallXmlPath driverPackagePath
            let! installConfiguration = InstallXml.loadInstallXml installXmlPath
            let! systemInfo = getSystemInfo
            let! requirementsAreFullfilled = assertDriverInstallRequirements installConfiguration systemInfo
            logger.Info("All install requirements are fullfilled: " + requirementsAreFullfilled.ToString())            
            let unregisterSccmApplication = unRegisterSccmApplication installConfiguration
            let! bitLockerSuspendExitCode = suspendBitLockerProtection()            
            let driverPackageName = getDriverPackageName installConfiguration
            let localDriversFolder = getLocalDriversPackageFolder driverPackageName
            let! localDriversFolderPath = Path.create localDriversFolder
            let! copyResult = copyDrivers (driverPackagePath, localDriversFolderPath)
            let installDriversExitCode = installDrivers localDriversFolderPath DriverTool.CreateDriverPackage.dtInstallPackageCmd installConfiguration driverPackageName 
            let! resetConfigFlagsResult = resetConfigFlags ()
            let! res = 
                match installDriversExitCode with
                |Ok ec -> 
                    match ec with
                    |0|3010 -> 
                        registerSccmApplication installConfiguration
                        Result.Ok ec
                    |_ -> Result.Ok ec
                |Error ex -> Result.Error ex
            return res
        }
        
    let unInstallDriverPackage (driverPackagePath:Path) =
        result{
            let! installXmlPath = getInstallXmlPath driverPackagePath
            let! installConfiguration = InstallXml.loadInstallXml installXmlPath
            let! systemInfo = getSystemInfo
            let! requirementsAreFullfilled = assertDriverInstallRequirements installConfiguration systemInfo
            logger.Info("All install requirements are fullfilled: " + requirementsAreFullfilled.ToString())
            let driverPackageName = getDriverPackageName installConfiguration
            let localDriversFolder = getLocalDriversPackageFolder driverPackageName
            let! localDriversFolderPath = Path.create localDriversFolder
            let! copyResult = copyDrivers (driverPackagePath, localDriversFolderPath)
            let uninstallDriversExitCode = installDrivers localDriversFolderPath DriverTool.CreateDriverPackage.dtUnInstallPackageCmd installConfiguration driverPackageName
            let! deleteDirectoryResult = DirectoryOperations.deleteDirectory true localDriversFolderPath
            let! res = 
                match uninstallDriversExitCode with
                |Ok ec -> 
                    match ec with
                    |0|3010 -> 
                        unRegisterSccmApplication installConfiguration
                        Result.Ok ec
                    |_ -> Result.Ok ec
                |Error ex -> Result.Error ex
            return res
        }