namespace DriverTool

module InstallDriverPackage =
    let logger = Logging.getLoggerByName("InstallDriverPackage")
    
    open System
    open InstallXml
    open DriverTool.Util.FSharp
    
    let getInstallXmlPath (driverPackagePath:Path) =
        Path.create (System.IO.Path.Combine(driverPackagePath.Value,"Install.xml"))

    type SystemInfo={Model:string; OperatingSystem:string}
    
    let getSystemInfo =
        result{
            let! currentComputerModel = (WmiHelper.getWmiProperty  "Win32_ComputerSystem" "Model")
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

    let isAdministrator () =
        let windowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent()
        let windowsPrincipal= new System.Security.Principal.WindowsPrincipal(windowsIdentity)
        let administratorRole=System.Security.Principal.WindowsBuiltInRole.Administrator
        windowsPrincipal.IsInRole(administratorRole)        

    let assertIsAdministrator (message) =
        let isAdministrator = isAdministrator()
        match isAdministrator with
        |true -> Result.Ok true
        |false-> Result.Error (new Exception(message))

    let getApplicationRegistryPath companyName applicationName =
        String.Format("HKLM\SOFTWARE\{0}\Applications\{1}",companyName,applicationName)
    
    open DriverTool.RegistryOperations
    
    let unRegisterSccmApplication (installConfiguration:InstallConfigurationData) =        
        let applicationRegistryKeyPath = (getApplicationRegistryPath installConfiguration.Publisher installConfiguration.PackageName)
        logger.Info("Unregister application: " + applicationRegistryKeyPath)
        match (regKeyExists applicationRegistryKeyPath) with
        | true -> deleteRegKey applicationRegistryKeyPath
        | _ -> ()        

    let registerSccmApplication (installConfiguration:InstallConfigurationData) =        
        let applicationRegistryKeyPath = (getApplicationRegistryPath installConfiguration.Publisher installConfiguration.PackageName)
        logger.Info("Register application: " + applicationRegistryKeyPath)
        use regKey = createRegKey applicationRegistryKeyPath
        regKey.SetValue("InstallRevision","000")
    
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
                        let! robocopyResult = Robocopy.roboCopy (sourceDriversFolderPath,destinationDriversFolderPath,"*.* /MIR")
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

    let installDrivers driverPackagePath =
        result{
            let! installXmlPath = getInstallXmlPath driverPackagePath
            let! installConfiguration = InstallXml.loadInstallXml installXmlPath
            let driverPackageName = getDriverPackageName installConfiguration
            let localDriversFolder = getLocalDriversPackageFolder driverPackageName
            let! localDriversFolderPath = Path.create localDriversFolder
            let! copyResult = copyDrivers (driverPackagePath, localDriversFolderPath)
            let activeDriverFolders = 
                System.IO.Directory.GetDirectories(localDriversFolderPath.Value)
                |>Seq.filter (fun x-> not (x.StartsWith("_")))
            logger.Info("Getting active drivers...")
            let activeDriverFolders = 
                System.IO.Directory.GetDirectories(localDriversFolderPath.Value)
                |>Seq.filter (fun path-> not (path.StartsWith("_")))
                |>Seq.map (fun path -> 
                                logger.Info("Will be processed: " + path)
                                path
                           )
                |>Seq.toArray
            logger.Info("Getting inactive drivers...")
            let inactiveDriverFolders = 
                System.IO.Directory.GetDirectories(localDriversFolderPath.Value)
                |>Seq.filter (fun path-> (path.StartsWith("_")))
                |>Seq.map (fun path -> 
                                logger.Info("Will NOT be processed: " + path)
                                path
                           )
                |>Seq.toArray
            logger.Info("Verifying that active driver install scripts exists...")
            let! existingInstallScripts =
                activeDriverFolders
                |> Seq.map (fun p -> Path.create (System.IO.Path.Combine(p,DriverTool.CreateDriverPackage.dtInstallPackageCmd)))
                |> Seq.map (fun pr -> 
                                match pr with
                                |Ok p -> FileOperations.ensureFileExistsWithMessage (String.Format("It is required that a install script '{0}' exists in each active driver folder.",CreateDriverPackage.dtInstallPackageCmd)) p
                                |Error _ -> pr
                            )
                |> toAccumulatedResult
            logger.Info("Installing drivers...")
            let! cmdExePath = 
                Path.create (System.IO.Path.Combine(Environment.systemFolder,"cmd.exe"))
            let! existingCmdExePath = FileOperations.ensureFileExists cmdExePath

            let! installedDriverExitCodes =
                existingInstallScripts
                |>Seq.map(fun script ->
                        let parentDirectory = (new System.IO.FileInfo(script.Value)).Directory
                        let scriptLogFile = System.IO.Path.Combine(System.Environment.ExpandEnvironmentVariables(installConfiguration.LogDirectory),String.Format("DT-Driver-Install-{0}-{1}.log",driverPackageName,parentDirectory.Name.Replace(".","_")))                        
                        ProcessOperations.startConsoleProcess (existingCmdExePath.Value,String.Format("/c \"{0}\"", script.Value),parentDirectory.FullName,scriptLogFile,true)
                    )
                |>toAccumulatedResult
                
            let adjustedExitCode = 
                let exitCodeSum = 
                    installedDriverExitCodes
                    |>Seq.sum
                match exitCodeSum with
                | ec when ec > 3010 -> 3010
                | ec -> ec
            return adjustedExitCode
        }

    let installDriverPackage (driverPackagePath:Path) =
        result{
            let! installXmlPath = getInstallXmlPath driverPackagePath
            let! installConfiguration = InstallXml.loadInstallXml installXmlPath
            let! systemInfo = getSystemInfo
            logger.Info("Checking if driver package is supported...")
            let! isSupported = assertIsSupported installConfiguration systemInfo
            logger.Info("Driver package is supported: " + isSupported.ToString())
            let! isAdministrator = assertIsAdministrator "Administrative privileges are required. Please run driver package install from an elevated command prompt."
            logger.Info("Installation is running with admin privileges: " + isAdministrator.ToString())            
            logger.Info("Process is 64 bit: " + (IntPtr.Size = 8).ToString())
            let unregisterSccmApplication = unRegisterSccmApplication installConfiguration
            let! bitLockerSuspendExitCode = suspendBitLockerProtection()
            let! installDriversExitCode = installDrivers driverPackagePath
            let exitCodeResult = Result.Ok 3010
            let! res = 
                match exitCodeResult with
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
            logger.Info("Checking if driver package is supported...")
            let! isSupported = assertIsSupported installConfiguration systemInfo
            logger.Info("Driver package is supported: " + isSupported.ToString())
            let! isAdministrator = assertIsAdministrator "Administrative privileges are required. Please run driver package install from an elevated command prompt."
            logger.Info("Uninstallation is running with admin privileges: " + isAdministrator.ToString())
            //let exitCodeResult = unInstallDrivers driverPackagePath
            let exitCodeResult = Result.Ok 3010
            let! res = 
                match exitCodeResult with
                |Ok ec -> 
                    match ec with
                    |0|3010 -> 
                        unRegisterSccmApplication installConfiguration
                        Result.Ok ec
                    |_ -> Result.Ok ec
                |Error ex -> Result.Error ex
            return res
        }
