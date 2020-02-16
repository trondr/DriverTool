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

    type LogMessage =
        |ErrorMsg of Exception
        |InfoMsg of string
        |WarnMsg of string
        |DebugMsg of string

    type LogItem = {TimeStamp:DateTime;LoggerName:string;ThreadId:int;LogMessage:LogMessage}
        
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

    type Progress = { Total:int; Value:int; Name:string}

    let getPercent progress =
        if(progress.Total=0) then 
            100.0
        else
            (float progress.Value / float progress.Total) * 100.0
    
    let toProgressMessage progress =
        sprintf "%i / %i (%f %%) (%s)" progress.Value progress.Total (getPercent progress) progress.Name

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
            Started:bool
            PackageDownloads:Progress
            SccmPackageDownloads:Progress
            PackageExtracts:Progress
            SccmPackageExtracts:Progress            
            Finished:bool
        }

    let startProgress progress =
        {progress with Total = progress.Total + 1}

    let doneProgress progress =
        {progress with Progress.Value = progress.Value + 1}


    let startPackageDownload packagingContext =
        {packagingContext with PackageDownloads = startProgress packagingContext.PackageDownloads}
    let donePackageDownload packagingContext =
        {packagingContext with PackageDownloads = doneProgress packagingContext.PackageDownloads}
    let startSccmPackageDownload packagingContext sccmReleaseDate =
        {packagingContext with SccmPackageDownloads = startProgress packagingContext.SccmPackageDownloads;SccmReleaseDate=sccmReleaseDate}
    let doneSccmPackageDownload packagingContext =
        {packagingContext with SccmPackageDownloads = doneProgress packagingContext.SccmPackageDownloads}

    let startPackageExtract packagingContext =
        {packagingContext with PackageExtracts = startProgress packagingContext.PackageExtracts}
    let donePackageExtract packagingContext =
        {packagingContext with PackageExtracts = doneProgress packagingContext.PackageExtracts}
    let startSccmPackageExtract packagingContext =
        {packagingContext with SccmPackageExtracts = startProgress packagingContext.SccmPackageExtracts}
    let doneSccmPackageExtract packagingContext =
        {packagingContext with SccmPackageExtracts = doneProgress packagingContext.SccmPackageExtracts}

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
            Started=false
            PackageDownloads={Total=0;Value=0;Name="Package Downloads"}
            SccmPackageDownloads={Total=0;Value=0;Name="Sccm Package Downloads"}
            PackageExtracts={Total=0;Value=0;Name="Package Extracts"}
            SccmPackageExtracts={Total=0;Value=0;Name="Sccm Package Extracts"}
            Finished=false;
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
            let packagingContext = toInitialPackagingContext dpcc.Manufacturer dpcc.PackagePublisher dpcc.Model dpcc.SystemFamily dpcc.OperatingSystem dpcc.CacheFolderPath dpcc.LogDirectory packageName packageFolderPath releaseDate releaseDate             
            return packagingContext
        }

    let updatePackagingContext packagingContext releaseDate dpcc =
        result{
            let! (packageName, packageFolderPath) = getPackageNameAndPath releaseDate dpcc        
            let updatePackagingContext = {packagingContext with ReleaseDate=releaseDate;PackageName=packageName;PackageFolderPath=packageFolderPath}
            return updatePackagingContext
        }

    type CreateDriverPackageMessage =
        |Start        
        |InitializePackaging of PackagingContext
        |RetrieveUpdateInfos of UpdatesRetrievalContext
        |UpdateInfosRetrieved of PackageInfo array
        |RetrieveSccmPackageInfo of SccmPackageInfoRetrievalContext
        |SccmPackageInfoRetrieved of SccmPackageInfo
        |DownloadPackage of PackageInfo*PackagingContext
        |PackageDownloaded of DownloadedPackageInfo option
        |DownloadSccmPackage of SccmPackageInfoDownloadContext
        |SccmPackageDownloaded of DownloadedSccmPackageInfo
        |StartDownload of WebFileDownload
        |DownloadFinished of WebFileDownload
        |ExtractPackage of PackagingContext*DownloadedPackageInfo
        |PackageExtracted of ExtractedPackageInfo        
        |ExtractSccmPackage of PackagingContext*DownloadedSccmPackageInfo
        |SccmPackageExtracted of ExtractedSccmPackageInfo        
        |PackagingProgress of PackagingContext
        |FinalizePackaging of PackagingContext
        |UpdatePackagingContext of PackagingContext
        |PackagingContextUpdated of PackagingContext*PackagingContext        
        |MovePackaging of PackagingContext*PackagingContext
        |PackagagingMoved
        |UpdateInstallXml of PackagingContext
        |InstallXmlUpdated of InstallConfigurationData
        |CreatePackageDefinition of PackagingContext*InstallConfigurationData
        |PackageDefinitionCreated of PackageDefinition*InstallConfigurationData
        |CreateDismPackageDefinition of PackagingContext*InstallConfigurationData
        |DismPackageDefinitionCreated of FileSystem.Path
        |PackagingFinalized
        |Finished
        |Error of Exception
        |Info of string
        |Warning of string
