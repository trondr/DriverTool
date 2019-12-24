namespace DriverTool
open F

module CommandProviders =
    open ManufacturerTypes
    open FileSystem
    open DriverTool.Library.F0

    let exportRemoteUdateInfoBase (manufacturerString,modelCodeString, operatingSystemString, csvFilePathString, overwrite, excludeUpdatePatterns) = 
        match (result {
                let! manufacturer = DriverTool.ManufacturerTypes.manufacturerStringToManufacturer (manufacturerString, true)
                let! modelCode = ModelCode.create modelCodeString true
                let! operatingSystemCode = OperatingSystemCode.create operatingSystemString true
                let! csvFilePath = FileSystem.path csvFilePathString
                let! cacheFolderPath = FileSystem.path DriverTool.Library.Configuration.downloadCacheDirectoryPath
                let! existingCacheFolderPath = DirectoryOperations.ensureDirectoryExists true cacheFolderPath
                let logger = DriverTool.Logging.getLoggerByName "exportRemoteUdateInfo"
                let! result = DriverTool.ExportRemoteUpdates.exportRemoteUpdates existingCacheFolderPath logger manufacturer modelCode operatingSystemCode csvFilePath overwrite excludeUpdatePatterns
                return result
            }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let exportRemoteUdateInfo (manufacturerString,modelCodeString, operatingSystemString, csvFilePathString, overwrite, excludeUpdatePatterns) =
        Logging.genericLogger Logging.LogLevel.Debug exportRemoteUdateInfoBase (manufacturerString,modelCodeString, operatingSystemString, csvFilePathString, overwrite, excludeUpdatePatterns)

    let exportLocalUdateInfoBase (csvFilePathString, overwrite, excludeUpdatePatterns) = 
        match (result{
                let! csvFilePath = FileSystem.path csvFilePathString
                let! nonExistingCsvFilePath = FileOperations.ensureFileDoesNotExist overwrite csvFilePath
                let! csvFilePath = FileSystem.path csvFilePathString
                let! cacheFolderPath = FileSystem.path DriverTool.Library.Configuration.downloadCacheDirectoryPath
                let! existingCacheFolderPath = DirectoryOperations.ensureDirectoryExists true cacheFolderPath
                let! exportResult = DriverTool.ExportLocalUpdates.exportLocalUpdates existingCacheFolderPath nonExistingCsvFilePath excludeUpdatePatterns
                return exportResult        
        }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let exportLocalUdateInfo (csvFilePathString, overwrite, excludeUpdatePatterns) =
        Logging.genericLogger Logging.LogLevel.Debug exportLocalUdateInfoBase (csvFilePathString, overwrite, excludeUpdatePatterns)
    
    let toDateTime dateString = 
        try
            Result.Ok (System.DateTime.Parse(dateString))
        with
            | ex -> Result.Error (toException (sprintf "Failed to convert date string '%s' to DateTime" dateString) (Some ex))

    let createDriverPackageBase (packagePublisher,manufacturerString, systemFamilyString,modelCodeString,operatingSystemString,destinationFolder,baseOnLocallyInstalledUpdates,excludeUpdatePatterns, packageTypeName,excludeSccmPackage,doNotDownloadSccmPackage,sccmPackageInstaller,sccmPackageReadme, sccmPackageReleased) =
        match (result {
                let! manufacturer = DriverTool.ManufacturerTypes.manufacturerStringToManufacturer (manufacturerString,true)
                let! systemFamily = SystemFamily.create systemFamilyString true
                let! modelCode = ModelCode.create modelCodeString true
                let! operatingSystemCode = OperatingSystemCode.create operatingSystemString true
                let! destinationFolderPath = FileSystem.path destinationFolder
                let! logDirectory = FileSystem.path DriverTool.Library.Configuration.getDriverPackageLogDirectoryPath
                let! excludeUpdateRegexPatterns = RegExp.toRegexPatterns excludeUpdatePatterns true
                let! released = toDateTime sccmPackageReleased
                let driverPackageCreationContext = DriverTool.CreateDriverPackage.toDriverPackageCreationContext packagePublisher manufacturer systemFamily modelCode operatingSystemCode destinationFolderPath baseOnLocallyInstalledUpdates logDirectory excludeUpdateRegexPatterns packageTypeName excludeSccmPackage doNotDownloadSccmPackage sccmPackageInstaller sccmPackageReadme released
                let! createDriverPackageResult = DriverTool.CreateDriverPackage.createDriverPackage driverPackageCreationContext
                return createDriverPackageResult
            }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let createDriverPackage (packagePublisher, manufacturer, systemFamily, modelCodeString, operatingSystemString, destinationFolder, baseOnLocallyInstalledUpdates, excludeUpdatePatterns, packageTypeName, excludeSccmPackage, doNotDownloadSccmPackage, sccmPackageInstaller, sccmPackageReadme, sccmPackageReleased) =
        Logging.genericLogger Logging.LogLevel.Debug createDriverPackageBase (packagePublisher,manufacturer, systemFamily,modelCodeString, operatingSystemString, destinationFolder,baseOnLocallyInstalledUpdates, excludeUpdatePatterns, packageTypeName, excludeSccmPackage, doNotDownloadSccmPackage,sccmPackageInstaller,sccmPackageReadme, sccmPackageReleased)
    
    let installDriverPackageBase (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = FileSystem.path driverPackagePathString            
                    let! installDriverPackageResult = InstallDriverPackage.installDriverPackage (driverPackagePath)
                    return installDriverPackageResult
        }) with        
        | Ok exitCode -> NCmdLiner.Result.Ok(exitCode)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let installDriverPackage(driverPackagePath) =
        Logging.genericLogger Logging.LogLevel.Debug installDriverPackageBase (driverPackagePath)
    
    let unInstallDriverPackageBase (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = FileSystem.path driverPackagePathString            
                    let! unInstallDriverPackageResult = InstallDriverPackage.unInstallDriverPackage (driverPackagePath)
                    return unInstallDriverPackageResult
        }) with        
        | Ok exitCode -> NCmdLiner.Result.Ok(exitCode)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let unInstallDriverPackage(driverPackagePath) =
        Logging.genericLogger Logging.LogLevel.Debug unInstallDriverPackageBase (driverPackagePath)
    
    let compressDriverPackageBase  (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = FileSystem.path driverPackagePathString            
                    let! compressDriverPackageResult = CompressDriverPackage.compressDriverPackage driverPackagePath
                    return compressDriverPackageResult
        }) with        
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)
    
    let compressDriverPackage(driverPackagePath) =
        Logging.genericLogger Logging.LogLevel.Debug compressDriverPackageBase (driverPackagePath)

    let decompressDriverPackageBase  (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = FileSystem.path driverPackagePathString            
                    let! decompressDriverPackageResult = CompressDriverPackage.decompressDriverPackage driverPackagePath
                    return decompressDriverPackageResult
        }) with        
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)
    
    let decompressDriverPackage(driverPackagePath) =
        Logging.genericLogger Logging.LogLevel.Debug decompressDriverPackageBase (driverPackagePath)