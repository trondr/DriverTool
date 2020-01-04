namespace DriverTool.Library

module Messages =
    open System
    open DriverTool.Library.Logging
    open DriverTool.Library.PackageXml
    open DriverTool.Library.UpdatesContext
    open DriverTool.Library.ManufacturerTypes
    open DriverTool.Library.PathOperations

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

    //let releaseDate= (max latestRelaseDate (downloadedSccmPackage.SccmPackage.Released.ToString("yyyy-MM-dd")))
    //let manufacturerName = manufacturerToName dpcc.Manufacturer
    //let systemFamilyName = dpcc.SystemFamily.Value.Replace(manufacturerName,"").Trim()                
    //let osBuild = OperatingSystem.getOsBuildForCurrentSystem
    //let packageName = sprintf "%s %s %s %s %s %s %s" manufacturerName systemFamilyName dpcc.Model.Value dpcc.OperatingSystem.Value osBuild dpcc.PackageTypeName releaseDate
    //let! versionedPackagePath = combine4Paths (FileSystem.pathValue dpcc.DestinationFolderPath, dpcc.Model.Value, releaseDate + "-1.0", "Script")

    type PackagingContext =
        {
            PackageName:string            
            PackageFolderPath:FileSystem.Path
        }

    let toPackagingContext packageName packageFolderPath =
        {
            PackageName=packageName
            PackageFolderPath=packageFolderPath        
        }

    let createPackagingContext (releaseDate:DateTime) (dpcc:DriverPackageCreationContext) =
        result{
            let releaseDateString = releaseDate.ToString("yyyy-MM-dd")        
            let manufacturerName = manufacturerToName dpcc.Manufacturer
            let systemFamilyName = dpcc.SystemFamily.Value.Replace(manufacturerName,"").Trim()                
            let osBuild = OperatingSystem.getOsBuildForCurrentSystem
            let packageName = sprintf "%s %s %s %s %s %s %s" manufacturerName systemFamilyName dpcc.Model.Value dpcc.OperatingSystem.Value osBuild dpcc.PackageTypeName releaseDateString
            let! packageFolderPath = (combine4Paths (FileSystem.pathValue dpcc.DestinationFolderPath, dpcc.Model.Value, releaseDateString + "-1.0", "Script"))
            let packagingContext = toPackagingContext packageName packageFolderPath                
            return packagingContext
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
        |InitializePackaging of PackagingContext
        |MovePackaging of PackagingContext*PackagingContext
        |PackagingMoved of PackagingContext*PackagingContext
        |ExtractPackage of DownloadedPackageInfo
        |PackageExtracted of ExtractedPackageInfo        
        |ExtractSccmPackage of DownloadedSccmPackageInfo
        |SccmPackageExtracted of ExtractedSccmPackageInfo
        |Error of Exception
        |Info of string
        |Finished        
