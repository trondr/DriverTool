namespace DriverTool

module DellUpdates =
    
    let softwareCatalogCab = "http://downloads.dell.com/catalog/CatalogPC.cab"
    let driverPackageCatalogCab = "http://downloads.dell.com/catalog/DriverPackCatalog.cab"
    
    let dupExitCode2ExitCode dupExitCode =
        match dupExitCode with
        |0 -> 0
        |1 -> 1603 // Fatal error
        |2 -> 3010 // Reboot required
        |3 -> 1638 // Another version of this product is already installed. 
        |4 -> 1654 // Install rejected 
        |5 -> 1633 // Platform unsupported
        |6 -> 1641 // Reboot in progress
        |_ -> 1 //Uknown error

    let dupExitCode2Message dupExitCode =
        match dupExitCode with
        |0 -> "The update was successful."
        |1 -> "An error occurred during the update process; the update was not successful." // Fatal error
        |2 -> "You must restart the system to apply the updates." // Reboot required
        |3 -> "You attempted to update to the same version of the software, or you tried to downgrade to a previous version of the software." // Another version of this product is already installed. 
        |4 -> "The required prerequisite software was not found on your system. The update was unsuccessful because the server did not meet BIOS, driver, or firmware prerequisites for the update to be applied, or because no supported device was found on the target system." // Install rejected 
        |5 -> "The operating system is not supported by the DUP, or the system is not supported by the DUP, or the DUP is not compatible with the devices found in your system." // Platform unsupported
        |6 -> "The system is being rebooted." // Reboot in progress
        |_ -> "Unknown error." //Uknown error

    open FSharp.Data
    open System

    type DellSoftwareCatalogXmlProvider = XmlProvider<"E:\\Dev\\github.trondr\\DriverTool\\example_data\\Dell_CatalogPC.xml">

    let getCacheDirectory =
        DriverTool.Configuration.getDownloadCacheDirectoryPath

    let getLocalDellSoftwareCatalogCabFilePath =
        Path.create (System.IO.Path.Combine(getCacheDirectory,"CatalogPC.cab"))

    let getLocalDellSoftwareCatalogXmlFilePath =
        Path.create (System.IO.Path.Combine(getCacheDirectory,"CatalogPC.xml"))
    
    let expandExe =
        System.IO.Path.Combine(DriverTool.Environment.nativeSystemFolder,"expand.exe")

    let expandCabFile (cabFilePath:Path, destinationFolderPath:Path) =
        result{
            let! expandResult = ProcessOperations.startConsoleProcess (expandExe,String.Format("\"{0}\" -F:* -R \"{1}\"",cabFilePath.Value,destinationFolderPath.Value), destinationFolderPath.Value,-1,null,null,false)
            
            return expandResult
        }

    let downloadAndLoadSoftwareCatalog () =
        result {
            let! destinationCabFile = getLocalDellSoftwareCatalogCabFilePath
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist (true, destinationCabFile)
            let! downloadResult = Web.downloadFile (new Uri(softwareCatalogCab), true, nonExistingDestinationCabFile)
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)
            let! destinationFolderPath = Path.create getCacheDirectory
            let! expandResult = expandCabFile (existingDestinationCabFile, destinationFolderPath)
            let! softwareCatalogXmlPath = getLocalDellSoftwareCatalogXmlFilePath
            let! existingSoftwareCatalogXmlPath = FileOperations.ensureFileExists softwareCatalogXmlPath
            let softwareCatalogXml = DellSoftwareCatalogXmlProvider.Load(existingSoftwareCatalogXmlPath.Value)
            return softwareCatalogXml
        }
    
    let getUpdates (modelCode:ModelCode, operatingSystemCode:OperatingSystemCode) =
        result{
            
            return Result.Error (new Exception("Not implemented"))
        }