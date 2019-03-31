namespace DriverTool

module DellCatalog =

    open DriverTool.Cab
    open DriverTool.SdpUpdates
    open System
    
    let expandExe =
        System.IO.Path.Combine(DriverTool.Environment.nativeSystemFolder,"expand.exe")

    let expandCabFile (cabFilePath:FileSystem.Path, destinationFolderPath:FileSystem.Path) =
        result{
            let! expandExePath = FileSystem.path expandExe
            let arguments = sprintf "\"%s\" -F:* -R \"%s\"" (FileSystem.pathValue cabFilePath) (FileSystem.pathValue destinationFolderPath)
            let! expandExitCode = ProcessOperations.startConsoleProcess (expandExePath, arguments, FileSystem.pathValue destinationFolderPath,-1,null,null,false)
            let! expandResult = expandExeExitCodeToResult cabFilePath expandExitCode
            return expandResult
        }

    let downloadSmsSdpCatalog () =
        result{
            let! destinationFolderPath = FileSystem.path DriverTool.Configuration.downloadCacheDirectoryPath
            let! hpCatalogDestinationFolderPath = FileSystem.path (System.IO.Path.Combine(DriverTool.Configuration.downloadCacheDirectoryPath,DriverTool.DellSettings.smsSdpCatalogFolderName))
            let! nonExistingHpCatalogDestinationFolderPath = DirectoryOperations.deleteDirectory true hpCatalogDestinationFolderPath
            let! existingHpCatalogDestinationFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmpty (nonExistingHpCatalogDestinationFolderPath,true) 
            let! destinationCabFile = PathOperations.combine2Paths (FileSystem.pathValue destinationFolderPath,DriverTool.DellSettings.smsSdpCatalogCabFileName)
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist true destinationCabFile
            let! downloadResult = Web.downloadFile (new Uri(DriverTool.DellSettings.smsSdpCatalog), true, nonExistingDestinationCabFile)
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)            
            let! expandResult = expandSmsSdpCabFile (existingDestinationCabFile, existingHpCatalogDestinationFolderPath)                        
            return existingHpCatalogDestinationFolderPath
        }

    let getLocalDriverPackageCatalogCabFilePath =
        FileSystem.path (System.IO.Path.Combine(DriverTool.Configuration.downloadCacheDirectoryPath,"DriverPackCatalog.cab"))

    let getLocalDriverPackageXmlFilePath =
        FileSystem.path (System.IO.Path.Combine(DriverTool.Configuration.downloadCacheDirectoryPath,"DriverPackCatalog.xml"))

    let downloadDriverPackageCatalog () =
        result {
            let! destinationCabFile = getLocalDriverPackageCatalogCabFilePath
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist true destinationCabFile
            let! downloadResult = Web.downloadFile (new Uri(DriverTool.DellSettings.driverPackageCatalogCab), true, nonExistingDestinationCabFile)
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)
            let! destinationFolderPath = FileSystem.path DriverTool.Configuration.downloadCacheDirectoryPath
            let! expandResult = expandCabFile (existingDestinationCabFile, destinationFolderPath)
            let! driverPackageCatalogXmlPath = getLocalDriverPackageXmlFilePath
            let! existingDriverPackageCatalogXmlPath = FileOperations.ensureFileExists driverPackageCatalogXmlPath            
            return existingDriverPackageCatalogXmlPath
        }

