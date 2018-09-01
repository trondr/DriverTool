namespace DriverTool

open System.Xml.Linq

module DriverTool = 
    open System
    
    type PackageXmlInfo = 
        {
            Location:string;
            Category:string
        }
    
    type DownloadedPackageXmlInfo = 
        {
            Location:string; 
            Category:string;
            FilePath:Path;
            BaseUrl:string
        }

    type PackageInfo = 
        {
            Name:string;
            Title:string;
            Version:string;
            BaseUrl:string;
            InstallerName:string;
            ExtractCommandLine:string;
            InstallCommandLine:string;
            Category:string;
            ReadmeName:string;
            ReleaseDate:string;
        }

    let getPackageInfoUnsafe (downloadedPackageInfo : DownloadedPackageXmlInfo) =
        let packageXDocument = XDocument.Load(downloadedPackageInfo.FilePath.Value)
        let packageXElement = packageXDocument.Root
        let name = packageXElement.Attribute(XName.Get("name")).Value
        let version = packageXElement.Attribute(XName.Get("version")).Value
        let title = (packageXElement.Element(XName.Get("Title")).Descendants(XName.Get("Desc")) |> Seq.head).Value
        let installerName = packageXElement.Element(XName.Get("Files"))
                                .Element(XName.Get("Installer"))
                                .Element(XName.Get("File"))
                                .Element(XName.Get("Name")).Value
        let readmeName = packageXElement.Element(XName.Get("Files"))
                                .Element(XName.Get("Readme"))
                                .Element(XName.Get("File"))
                                .Element(XName.Get("Name")).Value
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
            BaseUrl = baseUrl;
            ReadmeName = readmeName;
            ExtractCommandLine = extractCommandLine;
            InstallCommandLine = installCommandLine;                
            Category = category;
            ReleaseDate = releaseDate;
        }        
    
