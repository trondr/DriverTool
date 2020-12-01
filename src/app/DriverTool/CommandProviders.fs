﻿namespace DriverTool

module CommandProviders =       
    open DriverTool.Library.F0
    open DriverTool.Library.Logging
    open DriverTool.Library.F
    open DriverTool.Library

    let exportRemoteUdateInfoBase (manufacturerString,modelCodeString, operatingSystemString, csvFilePathString, overwrite, excludeUpdatePatterns) = 
        match (result {
                let! manufacturer = DriverTool.Library.ManufacturerTypes.manufacturerStringToManufacturer (manufacturerString, true)
                let! modelCode = ModelCode.create modelCodeString true
                let! operatingSystemCode = OperatingSystemCode.create operatingSystemString true
                let! csvFilePath = FileSystem.path csvFilePathString
                let! cacheFolderPath = FileSystem.path DriverTool.Library.Configuration.downloadCacheDirectoryPath
                let! existingCacheFolderPath = DirectoryOperations.ensureDirectoryExists true cacheFolderPath
                let logger = getLoggerByName "exportRemoteUdateInfo"
                let! result = DriverTool.ExportRemoteUpdates.exportRemoteUpdates existingCacheFolderPath logger manufacturer modelCode operatingSystemCode csvFilePath overwrite excludeUpdatePatterns
                return result
            }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Result.Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let exportRemoteUdateInfo (manufacturerString,modelCodeString, operatingSystemString, csvFilePathString, overwrite, excludeUpdatePatterns) =
        genericLogger LogLevel.Debug exportRemoteUdateInfoBase (manufacturerString,modelCodeString, operatingSystemString, csvFilePathString, overwrite, excludeUpdatePatterns)

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
        | Result.Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let exportLocalUdateInfo (csvFilePathString, overwrite, excludeUpdatePatterns) =
        genericLogger LogLevel.Debug exportLocalUdateInfoBase (csvFilePathString, overwrite, excludeUpdatePatterns)
    
    let toDateTime dateString = 
        try
            Result.Ok (System.DateTime.Parse(dateString))
        with
            | ex -> Result.Error (toException (sprintf "Failed to convert date string '%s' to DateTime" dateString) (Some ex))

    let createDriverPackageBase (packagePublisher,manufacturerString, systemFamilyString,modelCodeString,operatingSystemString,destinationFolder,baseOnLocallyInstalledUpdates,excludeUpdatePatterns, packageTypeName,excludeSccmPackage,doNotDownloadSccmPackage,sccmPackageInstaller,sccmPackageReadme, sccmPackageReleased) =
        match (result {
                let! manufacturer = DriverTool.Library.ManufacturerTypes.manufacturerStringToManufacturer (manufacturerString,true)
                let! systemFamily = SystemFamily.create systemFamilyString true
                let! modelCode = ModelCode.create modelCodeString true
                let! operatingSystemCode = OperatingSystemCode.create operatingSystemString true
                let! destinationFolderPath = FileSystem.path destinationFolder
                let! logDirectory = FileSystem.path DriverTool.Library.Configuration.getDriverPackageLogDirectoryPath
                let! excludeUpdateRegexPatterns = RegExp.toRegexPatterns excludeUpdatePatterns true
                let! released = toDateTime sccmPackageReleased
                let! cacheFolderPath = FileSystem.path DriverTool.Library.Configuration.downloadCacheDirectoryPath
                let! existingCacheFolderPath = DirectoryOperations.ensureDirectoryExists true cacheFolderPath
                let driverPackageCreationContext = DriverTool.CreateDriverPackage.toDriverPackageCreationContext packagePublisher manufacturer systemFamily modelCode operatingSystemCode destinationFolderPath existingCacheFolderPath baseOnLocallyInstalledUpdates logDirectory excludeUpdateRegexPatterns packageTypeName excludeSccmPackage doNotDownloadSccmPackage sccmPackageInstaller sccmPackageReadme released
                let! createDriverPackageResult = DriverTool.CreateDriverPackage.createDriverPackage driverPackageCreationContext
                return createDriverPackageResult
            }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Result.Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let createDriverPackage (packagePublisher, manufacturer, systemFamily, modelCodeString, operatingSystemString, destinationFolder, baseOnLocallyInstalledUpdates, excludeUpdatePatterns, packageTypeName, excludeSccmPackage, doNotDownloadSccmPackage, sccmPackageInstaller, sccmPackageReadme, sccmPackageReleased) =
        DriverTool.Library.x86Host.startx86HostProcess()
        let result = genericLogger LogLevel.Debug createDriverPackageBase (packagePublisher,manufacturer, systemFamily,modelCodeString, operatingSystemString, destinationFolder,baseOnLocallyInstalledUpdates, excludeUpdatePatterns, packageTypeName, excludeSccmPackage, doNotDownloadSccmPackage,sccmPackageInstaller,sccmPackageReadme, sccmPackageReleased)
        DriverTool.Library.x86Host.stopx86HostProcess()
        result
    
    let installDriverPackageBase (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = FileSystem.path driverPackagePathString            
                    let! installDriverPackageResult = InstallDriverPackage.installDriverPackage (driverPackagePath)
                    return installDriverPackageResult
        }) with        
        | Ok exitCode -> NCmdLiner.Result.Ok(exitCode)
        | Result.Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let installDriverPackage(driverPackagePath) =
        genericLogger LogLevel.Debug installDriverPackageBase (driverPackagePath)
    
    let unInstallDriverPackageBase (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = FileSystem.path driverPackagePathString            
                    let! unInstallDriverPackageResult = InstallDriverPackage.unInstallDriverPackage (driverPackagePath)
                    return unInstallDriverPackageResult
        }) with        
        | Ok exitCode -> NCmdLiner.Result.Ok(exitCode)
        | Result.Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let unInstallDriverPackage(driverPackagePath) =
        genericLogger LogLevel.Debug unInstallDriverPackageBase (driverPackagePath)
    
    let compressDriverPackageBase  (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = FileSystem.path driverPackagePathString            
                    let! compressDriverPackageResult = CompressDriverPackage.compressDriverPackage driverPackagePath
                    return compressDriverPackageResult
        }) with        
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Result.Error ex -> NCmdLiner.Result.Fail<int>(ex)
    
    let compressDriverPackage(driverPackagePath) =
        genericLogger LogLevel.Debug compressDriverPackageBase (driverPackagePath)

    let decompressDriverPackageBase  (driverPackagePathString) =
        match( result {
                    let! driverPackagePath = FileSystem.path driverPackagePathString            
                    let! decompressDriverPackageResult = CompressDriverPackage.decompressDriverPackage driverPackagePath
                    return decompressDriverPackageResult
        }) with        
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Result.Error ex -> NCmdLiner.Result.Fail<int>(ex)
    
    let decompressDriverPackage(driverPackagePath) =
        genericLogger LogLevel.Debug decompressDriverPackageBase (driverPackagePath)

    let downloadLenovoUpdatePackageXmls() =
        match(result{
            let! manufacturer = DriverTool.Library.ManufacturerTypes.getManufacturerForCurrentSystem()
            let! cacheFolderPath = FileSystem.path @"C:\Temp\LenovoUpdatePackagesXml2"
            let! existingCacheFolderPath = DirectoryOperations.ensureDirectoryExists true cacheFolderPath
            let! catalogXmlPath = DriverTool.LenovoCatalog.downloadCatalog existingCacheFolderPath
            let! lenovoCatalogProducts = DriverTool.LenovoCatalogXml.loadLenovoCatalog catalogXmlPath
            let allmodelCodes = DriverTool.LenovoCatalog.getAllLenovoModels lenovoCatalogProducts
            let! operatingSystemCode = OperatingSystemCode.create "WIN10X64" false
            let! logDirectory = FileSystem.path @"c:\temp"
            let! patterns = (RegExp.toRegexPatterns [||] true)            
            let logger = DriverTool.Library.Logging.getLoggerByName "DownloadLenovoUpdatePackageXmls"
            let packageInfoResults =
                allmodelCodes|>Array.map(fun modelCode -> 
                    let updatesRetrievalContext = DriverTool.Library.UpdatesContext.toUpdatesRetrievalContext manufacturer modelCode operatingSystemCode true logDirectory existingCacheFolderPath false patterns
                    let packageInfos = DriverTool.LenovoUpdates.getRemoteUpdates logger existingCacheFolderPath updatesRetrievalContext
                    packageInfos
                    )
            return packageInfoResults        
        })with
        |Result.Ok v -> NCmdLiner.Result.Ok(0)
        |Result.Error ex -> NCmdLiner.Result.Ok(1)

    let callWcfDemo () =
        DriverTool.Library.x86Host.startx86HostProcess()        
        logger.Warn("TO BE IMPLEMENTED. Start x86 host and call x86 Service methods. Demo:")
        let url = "https://support.lenovo.com/no/en/downloads/ds112090";
        printf "%s" (DriverTool.x86.Client.ToolService.GetWebPageContent(url))
        DriverTool.Library.x86Host.stopx86HostProcess()
        NCmdLiner.Result.Ok(0)

        