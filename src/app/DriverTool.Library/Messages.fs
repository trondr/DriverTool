namespace DriverTool.Library

module Messages =
    open System
    open DriverTool.Library.Logging
    open DriverTool.Library.PackageXml
    open DriverTool.Library.UpdatesContext
    open DriverTool.Library.ManufacturerTypes

    type SccmPackageInfoDownloadContext = {
        Manufacturer:Manufacturer
        CacheFolderPath:FileSystem.Path
        SccmPackage:SccmPackageInfo
    }

    let toSccmPackageInfoDownloadContext manufacturer cacheFolderPath sccmPackageInfo =
        {
            Manufacturer=manufacturer
            CacheFolderPath=cacheFolderPath
            SccmPackage=sccmPackageInfo
        }

    type DownloadMessage =
        |DownloadPackage of PackageInfo
        |DownloadSccmPackage of SccmPackageInfoDownloadContext
        |DownloadedSccmPackage of DownloadedSccmPackageInfo
        |Error of Exception

    //let toDownloadMessage message =
    //    match(box message) with
    //    | :? PackageInfo as p -> DownloadMessage.DownloadPackage p
    //    | :? SccmPackageInfo as sp -> DownloadMessage.DownloadSccmPackage sp
    //    | _ -> failwith (sprintf "Unknown download message: %s" (valueToString message))

    type PackagingMessage = 
        |DownloadedPackage of DownloadedPackageInfo
        |DownloadedSccmPackage of DownloadedSccmPackageInfo

    //let toDownloadedMessage message =
    //    match(box message) with
    //    | :? DownloadedPackageInfo as dp -> PackagingMessage.DownloadedPackage dp
    //    | :? DownloadedSccmPackageInfo as dsp -> PackagingMessage.DownloadedSccmPackage dsp
    //    | _ -> failwith (sprintf "Unknown downloaded message: %s" (valueToString message))

    type DriverPackageCreationContext =
        {
            PackagePublisher:string
            Manufacturer:Manufacturer
            SystemFamily:SystemFamily
            Model:ModelCode
            OperatingSystem:OperatingSystemCode
            DestinationFolderPath:FileSystem.Path
            CacheFolderPath:FileSystem.Path
            BaseOnLocallyInstalledUpdates:bool
            LogDirectory:FileSystem.Path
            ExcludeUpdateRegexPatterns: System.Text.RegularExpressions.Regex[]
            PackageTypeName:string
            ExcludeSccmPackage:bool            
            DoNotDownloadSccmPackage:bool
            SccmPackageInstaller:string
            SccmPackageReadme:string
            SccmPackageReleased:DateTime
        }

    type CreateDriverPackageMessage =
        |Start        
        |RetrieveUpdateInfos of UpdatesRetrievalContext
        |UpdateInfosRetrieved of PackageInfo array
        |RetrieveSccmPackageInfo of SccmPackageInfoRetrievalContext
        |SccmPackageInfoRetrieved of SccmPackageInfo
        |DownloadPackage of PackageInfo
        |DownloadedPackage of DownloadedPackageInfo
        |DownloadSccmPackage of SccmPackageInfoDownloadContext
        |DownloadedSccmPackage of DownloadedSccmPackageInfo
        |Error of Exception
        |Finished        
    
    //let toCreateDriverPackageMessage message =
    //    match(box message) with
    //    | :? DriverPackageCreationContext as context -> CreateDriverPackageMessage.Initialize context
    //    | :? UpdatesRetrievalContext as context -> CreateDriverPackageMessage.RetrieveUpdateInfos context
    //    | :? (PackageInfo array) as packageInfos -> CreateDriverPackageMessage.UpdateInfosRetrieved packageInfos
    //    | :? SccmPackageInfoRetrievalContext as context -> CreateDriverPackageMessage.RetrieveSccmPackageInfo context
    //    | :? PackageInfo as packageInfo -> CreateDriverPackageMessage.DownloadPackage packageInfo
    //    | :? DownloadedPackageInfo as downloadedPackageInfo -> CreateDriverPackageMessage.DownloadedPackage downloadedPackageInfo
    //    | :? SccmPackageInfo as sccmPackageInfo -> CreateDriverPackageMessage.DownloadSccmPackage sccmPackageInfo
    //    | :? DownloadedSccmPackageInfo as downloadedSccmPackageInfo -> CreateDriverPackageMessage.DownloadedSccmPackage downloadedSccmPackageInfo
    //    | :? Exception as ex -> CreateDriverPackageMessage.Error ex
    //    | _ -> failwith (sprintf "Unknown downloaded message: %s" (valueToString message))