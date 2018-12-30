namespace DriverTool

module HpCatalog =
    let soruceDriverPackCatalogCab = "https://ftp.hp.com/pub/caps-softpaq/cmit/HPClientDriverPackCatalog.cab"
    let platformListCab = "https://ftp.hp.com/pub/caps-softpaq/cmit/imagepal/ref/platformList.cab"
    let softPackSource = "https://ftp.hp.com/pub/softpaq/"
    let sccmPackageCatalog = "https://ftp.hp.com/pub/softlib/software/sms_catalog/HpCatalogForSms.latest.cab"

    open System
    open DriverTool.Configuration

    let expandExe =
        System.IO.Path.Combine(DriverTool.Environment.nativeSystemFolder,"expand.exe")

    let expandCabFile (cabFilePath:Path, destinationFolderPath:Path, destinationFilePath:Path) =
        result{
            let! expandResult = ProcessOperations.startConsoleProcess (expandExe, String.Format("\"{0}\" -F:* \"{1}\"", cabFilePath.Value, destinationFilePath.Value), destinationFolderPath.Value,-1,null,null,false)            
            return expandResult
        }

    let downloadDriverPackCatalog () =
        result{
            let! destinationFolderPath = Path.create getDownloadCacheDirectoryPath
            let! destinationCabFile = PathOperations.combine2Paths (destinationFolderPath.Value,"HPClientDriverPackCatalog.cab")
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist (true, destinationCabFile)
            let! downloadResult = Web.downloadFile (new Uri(soruceDriverPackCatalogCab), true, nonExistingDestinationCabFile)
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)
            let! destinationFilePath = PathOperations.combine2Paths (destinationFolderPath.Value,"HPClientDriverPackCatalog.xml")
            let! expandResult = expandCabFile (existingDestinationCabFile, destinationFolderPath,destinationFilePath)
            let! existingDriverPackageCatalogXmlPath = FileOperations.ensureFileExists destinationFilePath            
            return existingDriverPackageCatalogXmlPath
        }
