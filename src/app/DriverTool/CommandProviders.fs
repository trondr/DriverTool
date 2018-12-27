﻿namespace DriverTool
open F

module CommandProviders =

    let exportRemoteUdateInfoBase (modelCodeString, operatingSystemString, csvFilePathString, overwrite) = 
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
        Logging.genericLogger Logging.LogLevel.Debug exportRemoteUdateInfoBase (modelCodeString, operatingSystemString, csvFilePathString, overwrite)
    

    let exportLocalUdateInfoBase (csvFilePathString, overwrite) = 
        match (result{
                let! csvFilePath = Path.create csvFilePathString
                let! nonExistingCsvFilePath = FileOperations.ensureFileDoesNotExist (overwrite, csvFilePath)
                let! exportResult = DriverTool.ExportLocalUpdates.exportLocalUpdates nonExistingCsvFilePath
                return exportResult        
        }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let exportLocalUdateInfo (csvFilePathString, overwrite) =
        Logging.genericLogger Logging.LogLevel.Debug exportLocalUdateInfoBase (csvFilePathString, overwrite)

    let createDriverPackageBase (packagePublisher,manufacturerString, systemFamilyString,modelCodeString,operatingSystemString,destinationFolder,baseOnLocallyInstalledUpdates) =
        match (result {
                let! manufacturer = Manufacturer.create manufacturerString true
                let! systemFamily = SystemFamily.create systemFamilyString true
                let! modelCode = ModelCode.create modelCodeString true
                let! operatingSystemCode = OperatingSystemCode.create operatingSystemString true
                let! destinationFolderPath = Path.create destinationFolder
                let logDirectory = DriverTool.Configuration.getDriverPackageLogDirectoryPath
                let! createDriverPackageResult = DriverTool.CreateDriverPackage.createDriverPackage (packagePublisher,manufacturer,systemFamily,modelCode, operatingSystemCode, destinationFolderPath,baseOnLocallyInstalledUpdates, logDirectory)
                return createDriverPackageResult
            }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let createDriverPackage (packagePublisher, manufacturer, systemFamily,modelCodeString,operatingSystemString,destinationFolder,baseOnLocallyInstalledUpdates) =
        Logging.genericLogger Logging.LogLevel.Debug createDriverPackageBase (packagePublisher,manufacturer, systemFamily,modelCodeString, operatingSystemString, destinationFolder,baseOnLocallyInstalledUpdates)
    
    let installDriverPackageBase (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = Path.create driverPackagePathString            
                    let! installDriverPackageResult = InstallDriverPackage.installDriverPackage driverPackagePath
                    return installDriverPackageResult
        }) with        
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let installDriverPackage(driverPackagePath) =
        Logging.genericLogger Logging.LogLevel.Debug installDriverPackageBase (driverPackagePath)
    
    let unInstallDriverPackageBase (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = Path.create driverPackagePathString            
                    let! unInstallDriverPackageResult = InstallDriverPackage.unInstallDriverPackage driverPackagePath
                    return unInstallDriverPackageResult
        }) with        
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let unInstallDriverPackage(driverPackagePath) =
        Logging.genericLogger Logging.LogLevel.Debug unInstallDriverPackageBase (driverPackagePath)
    
    let compressDriverPackageBase  (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = Path.create driverPackagePathString            
                    let! compressDriverPackageResult = CompressDriverPackage.compressDriverPackage driverPackagePath
                    return compressDriverPackageResult
        }) with        
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)
    
    let compressDriverPackage(driverPackagePath) =
        Logging.genericLogger Logging.LogLevel.Debug compressDriverPackageBase (driverPackagePath)

    let decompressDriverPackageBase  (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = Path.create driverPackagePathString            
                    let! decompressDriverPackageResult = CompressDriverPackage.decompressDriverPackage driverPackagePath
                    return decompressDriverPackageResult
        }) with        
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)
    
    let decompressDriverPackage(driverPackagePath) =
        Logging.genericLogger Logging.LogLevel.Debug decompressDriverPackageBase (driverPackagePath)