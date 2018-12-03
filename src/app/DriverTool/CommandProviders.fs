namespace DriverTool
open F

module CommandProviders =
    open System

    let exportRemoteUdateInfoSimple (modelCodeString, operatingSystemString, csvFilePathString, overwrite) = 
        match (result {
                let! modelCode = ModelCode.create modelCodeString true
                let! operatingSystemCode = OperatingSystemCode.create operatingSystemString true
                let! csvFilePath = Path.create csvFilePathString
                let result = DriverTool.ExportRemoteUpdates.exportRemoteUpdates modelCode operatingSystemCode csvFilePath overwrite
                return result
            }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let exportRemoteUdateInfo (modelCodeString, operatingSystemString, csvFilePathString, overwrite) =
        Logging.debugLogger exportRemoteUdateInfoSimple (modelCodeString, operatingSystemString, csvFilePathString, overwrite)
    
    let createDriverPackageSimple (packagePublisher,manufacturerString, systemFamilyString,modelCodeString,operatingSystemString,destinationFolder, logDirectory) =
        match (result {
                let! manufacturer = Manufacturer.create manufacturerString true
                let! systemFamily = SystemFamily.create systemFamilyString true
                let! modelCode = ModelCode.create modelCodeString true
                let! operatingSystemCode = OperatingSystemCode.create operatingSystemString true
                let! destinationFolderPath = Path.create destinationFolder
                let! createDriverPackageResult = DriverTool.CreateDriverPackage.createDriverPackage (packagePublisher,manufacturer,systemFamily,modelCode, operatingSystemCode, destinationFolderPath, logDirectory)
                return createDriverPackageResult
            }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let createDriverPackage (packagePublisher, manufacturer, systemFamily,modelCodeString,operatingSystemString,destinationFolder,logDirectory) =
        Logging.debugLogger createDriverPackageSimple (packagePublisher,manufacturer, systemFamily,modelCodeString, operatingSystemString, destinationFolder, logDirectory)
    
    let installDriverPackageBase (driverPackagePath) =
        NCmdLiner.Result.Fail<int>(new NotImplementedException())

    let installDriverPackage(driverPackagePath) =
        Logging.debugLogger installDriverPackageBase (driverPackagePath)