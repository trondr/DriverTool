namespace DriverTool.Library

open InstallXml

module Messages =
    open System
    open DriverTool.Library.Logging
    open DriverTool.Library.PackageXml
    open DriverTool.Library.UpdatesContext
    open DriverTool.Library.ManufacturerTypes
    open DriverTool.Library.PathOperations
    open DriverTool.Library.PackageDefinition
    open DriverTool.Library.WebDownload

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

    type Progress = { Total:int; Value:int; Name:string}

    let getPercent progress =
        if(progress.Total=0) then 
            100.0
        else
            (float progress.Value / float progress.Total) * 100.0
    
    let toProgressMessage progress =
        sprintf "%i / %i (%f %%) (%s)" progress.Value progress.Total (getPercent progress) progress.Name

    type PackagingProgress = {
            Started:bool
            PackageDownloads:Progress
            SccmPackageDownloads:Progress
            PackageExtracts:Progress
            SccmPackageExtracts:Progress            
            Finished:bool
        }

    type PackagingContext =
        {            
            Manufacturer:Manufacturer
            PackagePublisher:string
            Model:ModelCode
            SystemFamily:SystemFamily
            OperatingSystem:OperatingSystemCode
            CacheFolderPath:FileSystem.Path
            LogDirectory:FileSystem.Path
            PackageName:string            
            PackageFolderPath:FileSystem.Path
            ReleaseDate:DateTime
            SccmReleaseDate:DateTime
            PackagingProgress:PackagingProgress            
            ExtractFolderPrefix:int
        }

    let startProgress progress =
        {progress with Total = progress.Total + 1}

    let doneProgress progress =
        {progress with Progress.Value = progress.Value + 1}

    let startPackageDownload' packagingProgress =
        {packagingProgress with PackageDownloads = startProgress packagingProgress.PackageDownloads; Started=true}
    let startPackageDownload (packagingContext:PackagingContext) =
        {packagingContext with PackagingProgress = startPackageDownload' packagingContext.PackagingProgress}

    let donePackageDownload' packagingProgress =
        {packagingProgress with PackageDownloads = doneProgress packagingProgress.PackageDownloads}        
    let donePackageDownload (packagingContext:PackagingContext) =
        {packagingContext with PackagingProgress = donePackageDownload' packagingContext.PackagingProgress}

    let startSccmPackageDownload' packagingProgress  =
        {packagingProgress with SccmPackageDownloads = startProgress packagingProgress.SccmPackageDownloads;Started=true}
    let startSccmPackageDownload packagingContext sccmReleaseDate =
        {packagingContext with PackagingProgress = startSccmPackageDownload' packagingContext.PackagingProgress;SccmReleaseDate=sccmReleaseDate}

    let doneSccmPackageDownload' packagingProgress =
        {packagingProgress with SccmPackageDownloads = doneProgress packagingProgress.SccmPackageDownloads}        
    let doneSccmPackageDownload (packagingContext:PackagingContext) =
        {packagingContext with PackagingProgress = doneSccmPackageDownload' packagingContext.PackagingProgress}

    let startPackageExtract' packagingProgress =
        {packagingProgress with PackageExtracts = startProgress packagingProgress.PackageExtracts; Started=true}
    let startPackageExtract (packagingContext:PackagingContext) =
        {packagingContext with PackagingProgress = startPackageExtract' packagingContext.PackagingProgress;}

    let donePackageExtract' packagingProgress =
        {packagingProgress with PackageExtracts = doneProgress packagingProgress.PackageExtracts}
    let donePackageExtract (packagingContext:PackagingContext) =
        {packagingContext with PackagingProgress = donePackageExtract' packagingContext.PackagingProgress}

    let startSccmPackageExtract' packagingProgress =
        {packagingProgress with SccmPackageExtracts = startProgress packagingProgress.SccmPackageExtracts; Started=true}
    let startSccmPackageExtract (packagingContext:PackagingContext) =
        {packagingContext with PackagingProgress = startSccmPackageExtract' packagingContext.PackagingProgress}

    let doneSccmPackageExtract' packagingProgress =
        {packagingProgress with SccmPackageExtracts = doneProgress packagingProgress.SccmPackageExtracts}
    let doneSccmPackageExtract (packagingContext:PackagingContext) =
        {packagingContext with PackagingProgress = doneSccmPackageExtract' packagingContext.PackagingProgress}

    let toInitialPackagingContext manufacturer packagePublisher model systemFamily operatingSystem logDirectory cacheFolderPath packageName packageFolderPath releaseDate sccmReleaseDate =
        {
            Manufacturer=manufacturer
            PackagePublisher=packagePublisher
            Model=model
            SystemFamily=systemFamily
            OperatingSystem=operatingSystem
            CacheFolderPath=cacheFolderPath
            LogDirectory=logDirectory
            PackageName=packageName
            PackageFolderPath=packageFolderPath
            ReleaseDate=releaseDate      
            SccmReleaseDate=sccmReleaseDate
            PackagingProgress =
                {
                    Started=false
                    PackageDownloads={Total=0;Value=0;Name="Package Downloads"}
                    SccmPackageDownloads={Total=0;Value=0;Name="Sccm Package Downloads"}
                    PackageExtracts={Total=0;Value=0;Name="Package Extracts"}
                    SccmPackageExtracts={Total=0;Value=0;Name="Sccm Package Extracts"}
                    Finished=false;
                }
            ExtractFolderPrefix=10
        }

    let getPackageNameAndPath (releaseDate:DateTime) (dpcc:DriverPackageCreationContext) =
        result{
            let releaseDateString = releaseDate.ToString("yyyy-MM-dd")
            let manufacturerName = manufacturerToName dpcc.Manufacturer
            let systemFamilyName = dpcc.SystemFamily.Value.Replace(manufacturerName,"").Trim()                
            let osBuild = OperatingSystem.getOsBuildForCurrentSystem
            let packageName = sprintf "%s %s %s %s %s %s %s" manufacturerName systemFamilyName dpcc.Model.Value dpcc.OperatingSystem.Value osBuild dpcc.PackageTypeName releaseDateString
            let! packageFolderPath = (combine4Paths (FileSystem.pathValue dpcc.DestinationFolderPath, dpcc.Model.Value, releaseDateString + "-1.0", "Script"))                         
            return (packageName,packageFolderPath)
        }

    let createPackagingContext (releaseDate:DateTime) (dpcc:DriverPackageCreationContext) =
        result{
            let! (packageName, packageFolderPath) = getPackageNameAndPath releaseDate dpcc
            let packagingContext = toInitialPackagingContext dpcc.Manufacturer dpcc.PackagePublisher dpcc.Model dpcc.SystemFamily dpcc.OperatingSystem dpcc.LogDirectory dpcc.CacheFolderPath packageName packageFolderPath releaseDate releaseDate             
            return packagingContext
        }

    let updatePackagingContext packagingContext releaseDate dpcc =
        result{
            let! (packageName, packageFolderPath) = getPackageNameAndPath releaseDate dpcc        
            let updatePackagingContext = {packagingContext with ReleaseDate=(max releaseDate packagingContext.ReleaseDate);PackageName=packageName;PackageFolderPath=packageFolderPath}
            return updatePackagingContext
        }

    let updatePackaginContextReleaseDate (packagingContext:PackagingContext) releaseDate =
        {packagingContext with ReleaseDate=(max packagingContext.ReleaseDate releaseDate)}
