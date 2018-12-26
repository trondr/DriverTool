namespace DriverTool

open System.Xml.Linq

module PackageXml = 
    open System
    
    type PackageXmlInfo = 
        {
            Location:string;
            Category:string;
            CheckSum:string;
        }
    
    type DownloadedPackageXmlInfo = 
        {
            Location:string; 
            Category:string;
            FilePath:Path;
            BaseUrl:string
            CheckSum:string;
        }

    type PackageInfo = 
        {
            Name:string;
            Title:string;
            Version:string;
            BaseUrl:string;
            InstallerName:string;
            InstallerCrc:string;
            InstallerSize:Int64;
            ExtractCommandLine:string;
            InstallCommandLine:string;
            Category:string;
            ReadmeName:string;
            ReadmeCrc:string;
            ReadmeSize:Int64;
            ReleaseDate:string;
            PackageXmlName:string
        }

    type DownloadJob = 
        {
            SourceUri:Uri;
            DestinationFile:string;
            Checksum:string;
            Size:Int64;
            Package:PackageInfo;
        }

    type DownloadedPackageInfo =
        {
            InstallerPath:string;
            ReadmePath:string;
            PackageXmlPath:string;
            Package:PackageInfo;
        }

    type ExtractedPackageInfo =
        {
            ExtractedDirectoryPath:string;
            DownloadedPackage:DownloadedPackageInfo;
        }    

    open DriverTool.Web
    type SccmPackageInfo = {        
        ReadmeFile:WebFile
        InstallerUrl:string;
        InstallerChecksum:string;
        InstallerFileName:string;
        Released:DateTime;
        Os:string;
        OsBuild:string
    }

    type DownloadedSccmPackageInfo = { InstallerPath:string; ReadmePath:string; SccmPackage:SccmPackageInfo}

    let getDestinationReadmePath destinationDirectory packageInfo =
        if(String.IsNullOrWhiteSpace(packageInfo.ReadmeName)) then
            String.Empty
        else
            System.IO.Path.Combine(destinationDirectory, packageInfo.ReadmeName)

    let getDestinationInstallerPath destinationDirectory packageInfo =
        if(String.IsNullOrWhiteSpace(packageInfo.InstallerName)) then
            String.Empty
        else
            System.IO.Path.Combine(destinationDirectory, packageInfo.InstallerName)

    let getDestinationPackageXmlPath destinationDirectory packageInfo =
        System.IO.Path.Combine(destinationDirectory, packageInfo.PackageXmlName)

    open DriverTool.Web

    /// <summary>
    /// Get files to download. As it is possible for two packages to share a readme file this function will return DownloadJobs with uniqe destination files.
    /// </summary>
    /// <param name="destinationDirectory"></param>
    /// <param name="packageInfos"></param>
    let packageInfosToDownloadJobs destinationDirectory packageInfos =
        seq{
            for packageInfo in packageInfos do
                let sourceReadmeUrl = String.Format("{0}/{1}", packageInfo.BaseUrl, packageInfo.ReadmeName)
                let sourceReadmeUri = new Uri(sourceReadmeUrl)
                match (Path.create (getDestinationReadmePath destinationDirectory packageInfo)) with
                |Ok p -> yield {SourceUri = sourceReadmeUri;SourceChecksum = packageInfo.ReadmeCrc; SourceFileSize = packageInfo.ReadmeSize; DestinationFile = p; }
                |Error ex -> Result.Error ex |> ignore

                let sourceInstallerUrl = String.Format("{0}/{1}", packageInfo.BaseUrl, packageInfo.InstallerName)
                let sourceInstallerUri = new Uri(sourceInstallerUrl)
                match Path.create (getDestinationInstallerPath destinationDirectory packageInfo) with
                |Ok p -> yield {SourceUri = sourceInstallerUri;SourceChecksum = packageInfo.InstallerCrc; SourceFileSize = packageInfo.InstallerSize; DestinationFile = p; }
                |Error ex -> Result.Error ex |> ignore
        }        
        //Make sure destination file is unique
        |> Seq.groupBy (fun p -> p.DestinationFile) 
        |> Seq.map (fun (k,v) -> v |>Seq.head)

    let getPackageInfoUnsafe (downloadedPackageInfo : DownloadedPackageXmlInfo) =
        let packageXDocument = XDocument.Load(downloadedPackageInfo.FilePath.Value)
        let packageXElement = packageXDocument.Root
        let name = packageXElement.Attribute(XName.Get("name")).Value
        let version = packageXElement.Attribute(XName.Get("version")).Value
        let title = (packageXElement.Element(XName.Get("Title")).Descendants(XName.Get("Desc")) |> Seq.head).Value
        let installerName = 
            packageXElement.Element(XName.Get("Files"))
                        .Element(XName.Get("Installer"))
                        .Element(XName.Get("File"))
                        .Element(XName.Get("Name")).Value
        let installerCrc =
            packageXElement.Element(XName.Get("Files"))
                        .Element(XName.Get("Installer"))
                        .Element(XName.Get("File"))
                        .Element(XName.Get("CRC")).Value
        
        let installerSize =
            packageXElement.Element(XName.Get("Files"))
                        .Element(XName.Get("Installer"))
                        .Element(XName.Get("File"))
                        .Element(XName.Get("Size")).Value |> int64

        let readmeName = 
            packageXElement.Element(XName.Get("Files"))
                        .Element(XName.Get("Readme"))
                        .Element(XName.Get("File"))
                        .Element(XName.Get("Name")).Value
        
        let readmeCrc =
            packageXElement.Element(XName.Get("Files"))
                        .Element(XName.Get("Readme"))
                        .Element(XName.Get("File"))
                        .Element(XName.Get("CRC")).Value
        
        let readmeSize =
            packageXElement.Element(XName.Get("Files"))
                        .Element(XName.Get("Readme"))
                        .Element(XName.Get("File"))
                        .Element(XName.Get("Size")).Value |> int64

        let extractCommandLine = 
            match packageXElement.Element(XName.Get("ExtractCommand")) with
            | null -> String.Empty
            | v -> v.Value
        let installCommandLine = 
            match packageXElement.Element(XName.Get("Install")) with
            | null -> String.Empty
            | v -> 
                match v.Element(XName.Get("Cmdline")) with
                | null -> String.Empty
                | v -> v.Value
        let releaseDate = packageXElement.Element(XName.Get("ReleaseDate")).Value
        let baseUrl = downloadedPackageInfo.BaseUrl
        let category = downloadedPackageInfo.Category
        {
            Name = name;
            Title = title;
            Version = version;
            InstallerName = installerName;
            InstallerCrc = installerCrc;
            InstallerSize = installerSize;
            BaseUrl = baseUrl;
            ReadmeName = readmeName;
            ReadmeCrc = readmeCrc;
            ReadmeSize = readmeSize;
            ExtractCommandLine = extractCommandLine;
            InstallCommandLine = installCommandLine;                
            Category = category;
            ReleaseDate = releaseDate;
            PackageXmlName = ((new System.IO.FileInfo(downloadedPackageInfo.FilePath.Value)).Name)
        }        
    
