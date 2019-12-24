namespace DriverTool

module DellCatalog =

    open DriverTool.Cab
    open DriverTool.SdpUpdates
    open System
    open DriverTool.Library.F
    open DriverTool.Library
    open DriverTool.Library.Environment
    
    let expandExe =
        System.IO.Path.Combine(nativeSystemFolder,"expand.exe")

    let expandCabFile (cabFilePath:FileSystem.Path, destinationFolderPath:FileSystem.Path) =
        result{
            let! expandExePath = FileSystem.path expandExe
            let arguments = sprintf "\"%s\" -F:* -R \"%s\"" (FileSystem.pathValue cabFilePath) (FileSystem.pathValue destinationFolderPath)
            let! expandExitCode = ProcessOperations.startConsoleProcess (expandExePath, arguments, FileSystem.pathValue destinationFolderPath,-1,null,null,false)
            let! expandResult = expandExeExitCodeToResult cabFilePath expandExitCode
            return expandResult
        }

    let downloadSmsSdpCatalog cachedFolderPath =
        result{
            
            let! hpCatalogDestinationFolderPath = PathOperations.combinePaths2 cachedFolderPath DriverTool.DellSettings.smsSdpCatalogFolderName
            let! nonExistingHpCatalogDestinationFolderPath = DirectoryOperations.deleteDirectory true hpCatalogDestinationFolderPath
            let! existingHpCatalogDestinationFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmpty (nonExistingHpCatalogDestinationFolderPath,true) 
            let! destinationCabFile = PathOperations.combine2Paths (FileSystem.pathValue cachedFolderPath,DriverTool.DellSettings.smsSdpCatalogCabFileName)
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist true destinationCabFile
            let! downloadResult = Web.downloadFile (new Uri(DriverTool.DellSettings.smsSdpCatalog), true, nonExistingDestinationCabFile)
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)            
            let! expandResult = expandSmsSdpCabFile (existingDestinationCabFile, existingHpCatalogDestinationFolderPath)                        
            return existingHpCatalogDestinationFolderPath
        }

    let getLocalDriverPackageCatalogCabFilePath cachedFolderPath =
        PathOperations.combinePaths2 cachedFolderPath "DriverPackCatalog.cab"

    let getLocalDriverPackageXmlFilePath cachedFolderPath =
        PathOperations.combinePaths2 cachedFolderPath "DriverPackCatalog.xml"

    let downloadDriverPackageCatalog cachedFolderPath =
        result {
            let! destinationCabFile = getLocalDriverPackageCatalogCabFilePath cachedFolderPath
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist true destinationCabFile
            let! downloadResult = Web.downloadFile (new Uri(DriverTool.DellSettings.driverPackageCatalogCab), true, nonExistingDestinationCabFile)
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)            
            let! expandResult = expandCabFile (existingDestinationCabFile, cachedFolderPath)
            let! driverPackageCatalogXmlPath = getLocalDriverPackageXmlFilePath cachedFolderPath
            let! existingDriverPackageCatalogXmlPath = FileOperations.ensureFileExists driverPackageCatalogXmlPath            
            return existingDriverPackageCatalogXmlPath
        }

