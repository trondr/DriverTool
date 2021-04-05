namespace DriverTool

open System

open DriverTool.Library
open DriverTool.Library.ManufacturerTypes
open DriverTool.Library.PackageXml
open DriverTool.Library.Web

module Updates =

    let getUpdatesFunc (logger, manufacturer:Manufacturer,baseOnLocallyInstalledUpdates:bool) =
        match manufacturer with
        |Manufacturer.Dell _ -> 
            match baseOnLocallyInstalledUpdates with
            |true -> DellUpdates.getLocalUpdates logger
            |false -> DellUpdates.getRemoteUpdates logger
        |Manufacturer.HP _ -> 
            match baseOnLocallyInstalledUpdates with
            |true -> HpUpdates.getLocalUpdates logger
            |false -> HpUpdates.getRemoteUpdates logger
        |Manufacturer.Lenovo _ ->        
            match baseOnLocallyInstalledUpdates with
            |true -> LenovoUpdates.getLocalUpdates logger
            |false -> LenovoUpdates.getRemoteUpdates logger

    let updateDownloadedPackageInfoFunc (manufacturer:Manufacturer) =
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.updateDownloadedPackageInfo
        |Manufacturer.HP _ -> HpUpdates.updateDownloadedPackageInfo
        |Manufacturer.Lenovo _ -> LenovoUpdates.updateDownloadedPackageInfo

    let getSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.getSccmDriverPackageInfo
        |Manufacturer.HP _ -> HpUpdates.getSccmDriverPackageInfo
        |Manufacturer.Lenovo _ -> LenovoUpdates.getSccmDriverPackageInfo

    let getSccmPackagesFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.getSccmDriverPackageInfos
        |Manufacturer.HP _ -> HpUpdates.getSccmDriverPackageInfos
        |Manufacturer.Lenovo _ -> LenovoUpdates.getSccmDriverPackageInfos
        
    let downloadSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.downloadSccmPackage
        |Manufacturer.HP _ -> HpUpdates.downloadSccmPackage
        |Manufacturer.Lenovo _ -> LenovoUpdates.downloadSccmPackage

    let downloadCmPackage (cacheDirectory, cmPackage:CmPackage) =
        result{
            let! installerdestinationFilePath = PathOperations.combinePaths2 cacheDirectory cmPackage.InstallerFile.FileName
            let! installerUri = toUri cmPackage.InstallerFile.Url
            let installerDownloadInfo = { SourceUri = installerUri;SourceChecksum = cmPackage.InstallerFile.Checksum;SourceFileSize = 0L;DestinationFile = installerdestinationFilePath}
            let! installerInfo = Web.downloadIfDifferent (logger,installerDownloadInfo,false)
            let installerPath = FileSystem.pathValue installerInfo.DestinationFile
            let! readmePath =
                match cmPackage.ReadmeFile with
                |Some readmeFile ->
                    result{
                        let! readmeDestinationFilePath = PathOperations.combinePaths2 cacheDirectory readmeFile.FileName
                        let! readmeUri = toUri readmeFile.Url
                        let readmeDownloadInfo = { SourceUri = readmeUri;SourceChecksum = readmeFile.Checksum;SourceFileSize = 0L;DestinationFile = readmeDestinationFilePath}
                        let! readmeInfo = Web.downloadIfDifferent (logger,readmeDownloadInfo,false)
                        let readmePath = FileSystem.pathValue readmeInfo.DestinationFile
                        return Some readmePath
                    }
                |None -> Result.Ok None
            return {
                InstallerPath = installerPath
                ReadmePath = readmePath
                CmPackage = cmPackage;
            }            
        }

    let downloadCmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> downloadCmPackage
        |Manufacturer.HP _ -> downloadCmPackage
        |Manufacturer.Lenovo _ -> downloadCmPackage
        
    let extractSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.extractSccmPackage
        |Manufacturer.HP _ -> HpUpdates.extractSccmPackage
        |Manufacturer.Lenovo _->  LenovoUpdates.extractSccmPackage
        
    let extractUpdateFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.extractUpdate
        |Manufacturer.HP _ -> HpUpdates.extractUpdate
        |Manufacturer.Lenovo _ -> LenovoUpdates.extractUpdate
