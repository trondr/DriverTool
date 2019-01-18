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
                let readmeDownloadInfo = 
                    result{
                        let sourceReadmeUrl = String.Format("{0}/{1}", packageInfo.BaseUrl, packageInfo.ReadmeName)
                        let! sourceReadmeUri = toUri (sourceReadmeUrl)
                        let! destinationReadmeFilePath = Path.create (getDestinationReadmePath destinationDirectory packageInfo)
                        return {SourceUri = sourceReadmeUri;SourceChecksum = packageInfo.ReadmeCrc; SourceFileSize = packageInfo.ReadmeSize; DestinationFile = destinationReadmeFilePath;}
                    }
                match readmeDownloadInfo with
                |Ok d -> yield d
                |Error ex -> logger.Error("Failed to get download info for readme file. " + ex.Message)

                let installerDownloadInfo = 
                    result{
                        let sourceInstallerUrl = String.Format("{0}/{1}", packageInfo.BaseUrl, packageInfo.InstallerName)
                        let! sourceInstallerUri = toUri (sourceInstallerUrl)
                        let! destinationInstallerFilePath = Path.create (getDestinationInstallerPath destinationDirectory packageInfo)
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
    
    let toTitlePostFix (title:string) (version:string) (releaseDate:string) = 
        nullOrWhiteSpaceGuard title "title"
        let parts = title.Split('-');
        let titlePostfix = 
            match parts.Length with
            | 0 -> String.Empty
            | _ -> parts.[parts.Length - 1]
        toValidDirectoryName (String.Format("{0}_{1}_{2}",titlePostfix,version,releaseDate))
    
    open System.Linq
    open System.Text.RegularExpressions
    
    let toTitlePrefix (title:string) (category:string) (postFixLength: int) = 
        nullOrWhiteSpaceGuard title "title"
        nullGuard category "category"
        let parts = title.Split('-');
        let partsString =
            (parts.[0]).AsEnumerable().Take(57 - postFixLength - category.Length).ToArray()
        let titlePrefix = 
            category + "_" + new String(partsString);
        toValidDirectoryName titlePrefix    

    let removeVowels (text:string) =
        System.Text.RegularExpressions.Regex.Replace(text,"[aeiouy]","",RegexOptions.IgnoreCase)
    
    let reducePackageTitle  (title:string) =
        if((String.length title) > 60) then
            removeVowels title
        else
            title

    let getPackageFolderName (packageInfo:PackageInfo) =                
        let validDirectoryName = 
            toValidDirectoryName (reducePackageTitle packageInfo.Title)
        let postfix = 
            toTitlePostFix validDirectoryName packageInfo.Version packageInfo.ReleaseDate
        let prefix = 
            toTitlePrefix validDirectoryName (packageInfo.Category |? String.Empty) postfix.Length
        let packageFolderName = 
            String.Format("{0}_{1}",prefix,postfix).Replace("__", "_").Replace("__", "_");
        packageFolderName
    
    let downloadedPackageInfoToExtractedPackageInfo (packageFolderPath:Path,downloadedPackageInfo) =
        {
            ExtractedDirectoryPath = packageFolderPath.Value;
            DownloadedPackage = downloadedPackageInfo;
        }

    let copyFile (sourceFilePath, destinationFilePath) =
        try
            System.IO.File.Copy(sourceFilePath, destinationFilePath, true)
            Result.Ok destinationFilePath
        with
        | ex -> Result.Error (new Exception(String.Format("Failed to copy file '{0}'->'{1}'.", sourceFilePath, destinationFilePath), ex))
    
    open DriverTool.ExistingPath

    let extractPackageXml (downloadedPackageInfo, packageFolderPath:Path)  =
        let destinationFilePath = System.IO.Path.Combine(packageFolderPath.Value,downloadedPackageInfo.Package.PackageXmlName)
        match ExistingFilePath.create downloadedPackageInfo.PackageXmlPath with
        |Ok filePath -> 
            match (copyFile (filePath.Value, destinationFilePath)) with
            |Ok _ -> 
                Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let extractReadme (downloadedPackageInfo, packageFolderPath:Path)  =
        let destinationReadmeFilePath = System.IO.Path.Combine(packageFolderPath.Value,downloadedPackageInfo.Package.ReadmeName)
        match ExistingFilePath.create downloadedPackageInfo.ReadmePath with
        |Ok readmeFilePath -> 
            match (copyFile (readmeFilePath.Value, destinationReadmeFilePath)) with
            |Ok _ -> 
                Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let getFileNameFromCommandLine (commandLine:string) = 
        let fileName = commandLine.Split(' ').[0];
        fileName;

    let extractInstaller (downloadedPackageInfo, packageFolderPath:Path) =
        if(String.IsNullOrWhiteSpace(downloadedPackageInfo.Package.ExtractCommandLine)) then
           logger.Info("Installer does not support extraction, copy the installer directly to package folder...")
           let destinationInstallerFilePath = System.IO.Path.Combine(packageFolderPath.Value,downloadedPackageInfo.Package.InstallerName)
           match ExistingFilePath.create downloadedPackageInfo.InstallerPath with
           |Ok installerPath -> 
                match copyFile (installerPath.Value, destinationInstallerFilePath) with
                |Ok _ -> 
                    Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
                |Error ex -> Result.Error ex
           |Error ex -> 
                Result.Error ex
        else
            logger.Info("Installer supports extraction, extract installer...")
            let extractCommandLine = downloadedPackageInfo.Package.ExtractCommandLine.Replace("%PACKAGEPATH%",String.Format("\"{0}\"",packageFolderPath.Value))
            let fileName = getFileNameFromCommandLine extractCommandLine
            let arguments = extractCommandLine.Replace(fileName,"")
            match (ExistingFilePath.create downloadedPackageInfo.InstallerPath) with
            |Ok fp -> 
                match DriverTool.ProcessOperations.startConsoleProcess (fp.Value, arguments,packageFolderPath.Value,-1,null,null,false) with
                |Ok _ -> Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
                |Error ex -> Result.Error ex
            |Error ex -> Result.Error ex