namespace DriverTool

open System.Xml.Linq

module PackageXml = 
    let logger = Logging.getLoggerByName("PackageXml")
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
            FilePath:FileSystem.Path;
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
            DestinationFile:FileSystem.Path;
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
    
    type SccmPackageInfo = {        
        ReadmeFile:DriverTool.Web.WebFile
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
                let readmeDownloadInfo = 
                    result{
                        let sourceReadmeUrl = String.Format("{0}/{1}", packageInfo.BaseUrl, packageInfo.ReadmeName)
                        let! sourceReadmeUri = DriverTool.Web.toUri (sourceReadmeUrl)
                        let! destinationReadmeFilePath = FileSystem.path (getDestinationReadmePath destinationDirectory packageInfo)
                        return {SourceUri = sourceReadmeUri;SourceChecksum = packageInfo.ReadmeCrc; SourceFileSize = packageInfo.ReadmeSize; DestinationFile = destinationReadmeFilePath;}
                    }
                match readmeDownloadInfo with
                |Ok d -> yield d
                |Error ex -> logger.Error("Failed to get download info for readme file. " + ex.Message)

                let installerDownloadInfo = 
                    result{
                        let sourceInstallerUrl = String.Format("{0}/{1}", packageInfo.BaseUrl, packageInfo.InstallerName)
                        let! sourceInstallerUri = toUri (sourceInstallerUrl)
                        let! destinationInstallerFilePath = FileSystem.path (getDestinationInstallerPath destinationDirectory packageInfo)
                        return {SourceUri = sourceInstallerUri;SourceChecksum = packageInfo.ReadmeCrc; SourceFileSize = packageInfo.ReadmeSize; DestinationFile = destinationInstallerFilePath; }
                    }
                match installerDownloadInfo with
                |Ok d -> yield d
                |Error ex -> logger.Error("Failed to get download info for installer file. " + ex.Message)
        }        
        //Make sure destination file is unique
        |> Seq.groupBy (fun p -> p.DestinationFile) 
        |> Seq.map (fun (k,v) -> v |>Seq.head)

    let getPackageInfoUnsafe (downloadedPackageInfo : DownloadedPackageXmlInfo) =
        let packageXDocument = XDocument.Load(FileSystem.pathValue downloadedPackageInfo.FilePath)
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
            PackageXmlName = ((new System.IO.FileInfo(FileSystem.pathValue downloadedPackageInfo.FilePath)).Name)
        }        
        
    open System.Linq
    open System.Text.RegularExpressions
    open NCmdLiner.Exceptions
    
    let getPackageFolderName (packageInfo:PackageInfo) =         
        let postfix = packageInfo.ReleaseDate
        let prefix = (packageInfo.Category |? "Unknown_Category")
        let packageFolderName = 
            String.Format("{0}_{1}",prefix,postfix).Replace("__", "_").Replace("__", "_");
        packageFolderName
    
    let downloadedPackageInfoToExtractedPackageInfo (packageFolderPath:FileSystem.Path,downloadedPackageInfo) =
        {
            ExtractedDirectoryPath = FileSystem.pathValue packageFolderPath;
            DownloadedPackage = downloadedPackageInfo;
        }

    let copyFile (sourceFilePath, destinationFilePath) =
        try
            System.IO.File.Copy(sourceFilePath, destinationFilePath, true)
            Result.Ok destinationFilePath
        with
        | ex -> Result.Error (new Exception(String.Format("Failed to copy file '{0}'->'{1}'.", sourceFilePath, destinationFilePath), ex))
    
    let extractPackageXml (downloadedPackageInfo, packageFolderPath:FileSystem.Path)  =
        let destinationFilePath = System.IO.Path.Combine(FileSystem.pathValue packageFolderPath,downloadedPackageInfo.Package.PackageXmlName)
        match FileSystem.existingFilePath downloadedPackageInfo.PackageXmlPath with
        |Ok filePath -> 
            match (copyFile (FileSystem.existingFilePathValue filePath, destinationFilePath)) with
            |Ok _ -> 
                Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let extractReadme (downloadedPackageInfo, packageFolderPath:FileSystem.Path)  =
        let destinationReadmeFilePath = System.IO.Path.Combine(FileSystem.pathValue packageFolderPath,downloadedPackageInfo.Package.ReadmeName)
        match FileSystem.existingFilePath downloadedPackageInfo.ReadmePath with
        |Ok readmeFilePath -> 
            match (copyFile (FileSystem.existingFilePathValue readmeFilePath, destinationReadmeFilePath)) with
            |Ok _ -> 
                Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let getFileNameFromCommandLine (commandLine:string) = 
        let fileName = commandLine.Split(' ').[0];
        fileName;

    let extractInstaller (downloadedPackageInfo, packageFolderPath:FileSystem.Path) =
        if(String.IsNullOrWhiteSpace(downloadedPackageInfo.Package.ExtractCommandLine)) then
           logger.Info("Installer does not support extraction, copy the installer directly to package folder...")
           let destinationInstallerFilePath = System.IO.Path.Combine(FileSystem.pathValue packageFolderPath,downloadedPackageInfo.Package.InstallerName)
           match FileSystem.existingFilePath downloadedPackageInfo.InstallerPath with
           |Ok installerPath -> 
                match copyFile (FileSystem.existingFilePathValue installerPath, destinationInstallerFilePath) with
                |Ok _ -> 
                    Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
                |Error ex -> Result.Error ex
           |Error ex -> 
                Result.Error ex
        else
            logger.Info("Installer supports extraction, extract installer...")
            let extractCommandLine = downloadedPackageInfo.Package.ExtractCommandLine.Replace("%PACKAGEPATH%",String.Format("\"{0}\"",packageFolderPath))
            let fileName = getFileNameFromCommandLine extractCommandLine
            let arguments = extractCommandLine.Replace(fileName,"")
            match (FileSystem.existingFilePath downloadedPackageInfo.InstallerPath) with
            |Ok fp -> 
                match DriverTool.ProcessOperations.startConsoleProcess (FileSystem.existingFilePathValueToPath fp, arguments,FileSystem.pathValue packageFolderPath,-1,null,null,false) with
                |Ok _ -> Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
                |Error ex -> Result.Error ex
            |Error ex -> Result.Error ex