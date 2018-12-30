namespace DriverTool

module HpUpdates =
    open System
    open PackageXml
    
    let getRemoteUpdates (modelCode: ModelCode, operatingSystemCode: OperatingSystemCode, overwrite,logDirectory:string) =
        raise (new NotImplementedException("HpUpdates.getRemoteUpdates"))

    let getSccmDriverPackageInfo (modelCode:ModelCode, operatingSystemCode:OperatingSystemCode)  : Result<SccmPackageInfo,Exception> =
        raise (new NotImplementedException("HpUpdates.getSccmDriverPackageInfo"))

    let downloadSccmPackage (cacheDirectory, sccmPackage:SccmPackageInfo) =
        raise (new NotImplementedException("HpUpdates.downloadSccmPackage"))

    let extractSccmPackage (downloadedSccmPackage:DownloadedSccmPackageInfo, destinationPath:Path) =
        raise (new NotImplementedException("HpUpdates.extractSccmPackage"))

    let extractUpdate (rootDirectory:Path, (prefix,downloadedPackageInfo:DownloadedPackageInfo)) =
        raise (new NotImplementedException("HpUpdates.extractUpdate"))