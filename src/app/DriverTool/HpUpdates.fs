namespace DriverTool

module HpUpdates =
    open System
    open PackageXml
    open DriverTool.Web
    open DriverTool.HpCatalog
    
    let getRemoteUpdates (modelCode: ModelCode, operatingSystemCode: OperatingSystemCode, overwrite,logDirectory:string) =
        raise (new NotImplementedException("HpUpdates.getRemoteUpdates"))

    let getSccmDriverPackageInfo (modelCode: ModelCode, operatingSystemCode:OperatingSystemCode) =
        result{
            let! driverPackCatalogXmlFilePath = downloadDriverPackCatalog()
            let! sccmPackageInfo = getSccmDriverPackageInfoBase (driverPackCatalogXmlFilePath, modelCode, operatingSystemCode)
            return sccmPackageInfo
        }

    let downloadSccmPackage (cacheDirectory, sccmPackage:SccmPackageInfo) =
        result{                        
            let! installerdestinationFilePath = Path.create (System.IO.Path.Combine(cacheDirectory,sccmPackage.InstallerFileName))
            let! installerUri = DriverTool.Web.toUri sccmPackage.InstallerUrl
            let installerDownloadInfo = { SourceUri = installerUri;SourceChecksum = sccmPackage.InstallerChecksum;SourceFileSize = 0L;DestinationFile = installerdestinationFilePath}
            let! installerInfo = Web.downloadIfDifferent (installerDownloadInfo,false)
            let installerPath = installerInfo.DestinationFile.Value

            return {
                InstallerPath = installerPath
                ReadmePath = String.Empty
                SccmPackage = sccmPackage;
            }
        } 

    let extractSccmPackage (downloadedSccmPackage:DownloadedSccmPackageInfo, destinationPath:Path) =
        raise (new NotImplementedException("HpUpdates.extractSccmPackage"))

    let extractUpdate (rootDirectory:Path, (prefix,downloadedPackageInfo:DownloadedPackageInfo)) =
        raise (new NotImplementedException("HpUpdates.extractUpdate"))