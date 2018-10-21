namespace DriverTool
open F

module CommandProviders =

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
    
    let createDriverPackageSimple (modelCodeString,operatingSystemString,destinationFolder, logDirectory) =
        match (result {
                let! modelCode = ModelCode.create modelCodeString true
                let! operatingSystemCode = OperatingSystemCode.create operatingSystemString true
                let! destinationFolderPath = Path.create destinationFolder
                let! createDriverPackageResult = DriverTool.CreateDriverPackage.createDriverPackage (modelCode, operatingSystemCode, destinationFolderPath, logDirectory)
                return createDriverPackageResult
            }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let createDriverPackage (modelCodeString,operatingSystemString,destinationFolder,logDirectory) =
        Logging.debugLogger createDriverPackageSimple (modelCodeString, operatingSystemString, destinationFolder, logDirectory)