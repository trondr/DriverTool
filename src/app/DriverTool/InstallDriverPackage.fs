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
            let unregisterSccmApplication = unRegisterSccmApplication installConfiguration
            //suspendBitLockerProtection
            //let exitCodeResult = installDrivers driverPackagePath
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
