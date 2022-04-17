namespace DriverTool.Library

open ManufacturerTypes

module PackageXml = 
    open System.Xml.Linq
    open DriverTool.Library.F0
    open DriverTool.Library.F
    open DriverTool.Library
    open DriverTool.Library.XmlHelper
    open DriverTool.Library.Web
    open DriverTool.Library.DriverPack
    
    open System
    let logger = DriverTool.Library.Logging.getLoggerByName "PackageXml"
        
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


    type PackageFileType = Installer|Readme|External
    
    type PackageFile =
        {
            Url:Uri option
            Name:string
            Checksum:string
            Size:Int64
            Type:PackageFileType
        }
        with
            static member Default = {Url=None;Name=String.Empty;Checksum=String.Empty;Size=0L;Type=PackageFileType.Readme}

    type PackageInfo = 
        {
            Name:string
            Title:string
            Version:string
            Installer:PackageFile
            ExtractCommandLine:string
            InstallCommandLine:string
            Category:string
            Readme:PackageFile
            ReleaseDate:string
            PackageXmlName:string
            ExternalFiles: PackageFile[] option
        }
        with
            static member Default = { Name = String.Empty; Title = String.Empty; Version=String.Empty; Installer=PackageFile.Default; ExtractCommandLine=String.Empty; InstallCommandLine=String.Empty; Category=String.Empty; Readme=PackageFile.Default; ReleaseDate=String.Empty; PackageXmlName=String.Empty; ExternalFiles=None }

    let packageInfoSortKey packageInfo =
        sprintf "%s-%s" packageInfo.Category packageInfo.ReleaseDate

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
        ReadmeFile:DriverTool.Library.Web.WebFile
        InstallerFile:DriverTool.Library.Web.WebFile        
        Released:DateTime;
        Os:string;
        OsBuild:string
    }

    type DownloadedSccmPackageInfo = { InstallerPath:string; ReadmePath:string; SccmPackage:SccmPackageInfo}

    type ExtractedSccmPackageInfo = { ExtractedDirectoryPath:string; DownloadedSccmPackage:DownloadedSccmPackageInfo;}
        
    ///Convert list of model codes to a wql query that can be used as condition in model specific SCCM task sequence step
    let toModelCodesWqlQuery name (manufacturer:Manufacturer) (modelCodes:string[]) =
        let modelNameSpace =
            match manufacturer with
            |Manufacturer.Dell _ -> "root\\WMI"
            |Manufacturer.HP _ -> "root\\WMI"
            |Manufacturer.Lenovo _ -> "root\\cimv2"

        let modelClassName =
            match manufacturer with
            |Manufacturer.Dell _ -> "MS_SystemInformation"
            |Manufacturer.HP _ -> "MS_SystemInformation"
            |Manufacturer.Lenovo _ -> "Win32_ComputerSystem"
        
        let modelPropertyName =
            match manufacturer with
            |Manufacturer.Dell _ -> "SystemSKU"
            |Manufacturer.HP _ -> "BaseBoardProduct"
            |Manufacturer.Lenovo _ -> "Model"               

        let whereCondition =
            (modelCodes 
            |> Array.map(fun mc -> sprintf "(%s like '%s%%')" modelPropertyName mc)
            |> String.concat " or ").Trim()        
        let query = sprintf "select %s from %s where %s" modelPropertyName modelClassName whereCondition
        {
            Name = name
            NameSpace = modelNameSpace
            Query=query
        }

    ///Convert manufacturer name to a wql query that can be used as condition for a manufacturer specific group in SCCM task sequence
    let toManufacturerWqlQuery manufacturer =        
        let nameSpace =
            match manufacturer with
            |Manufacturer.Dell _ -> "root\\WMI"
            |Manufacturer.HP _ -> "root\\WMI"
            |Manufacturer.Lenovo _ -> "root\\cimv2"

        let className =
            match manufacturer with
            |Manufacturer.Dell _ -> "MS_SystemInformation"
            |Manufacturer.HP _ -> "MS_SystemInformation"
            |Manufacturer.Lenovo _ -> "Win32_ComputerSystem"
        
        let propertyName =
            match manufacturer with
            |Manufacturer.Dell _ -> "BaseBoardManufacturer"
            |Manufacturer.HP _ -> "BaseBoardManufacturer"
            |Manufacturer.Lenovo _ -> "Manufacturer"     
        
        let manufacturerName = DriverTool.Library.ManufacturerTypes.manufacturerToName manufacturer        
        let query = sprintf "select %s from %s where (%s like '%%%s%%')" propertyName className propertyName manufacturerName 
        {
            Name = manufacturerName
            NameSpace = nameSpace
            Query=query
        }

    let getDestinationReadmePath destinationDirectory packageInfo =
        if(String.IsNullOrWhiteSpace(packageInfo.Readme.Name)) then
            String.Empty
        else
            System.IO.Path.Combine(FileSystem.pathValue destinationDirectory, packageInfo.Readme.Name)

    let getDestinationInstallerPath destinationDirectory packageInfo =
        if(String.IsNullOrWhiteSpace(packageInfo.Installer.Name)) then
            String.Empty
        else
            System.IO.Path.Combine(FileSystem.pathValue destinationDirectory, packageInfo.Installer.Name)

    let getDestinationPackageXmlPath destinationDirectory packageInfo =
        System.IO.Path.Combine(FileSystem.pathValue destinationDirectory, packageInfo.PackageXmlName)
    
    let toDownloadedPackageInfo destinationDirectory packageInfo =
        {
            InstallerPath = getDestinationInstallerPath destinationDirectory packageInfo;
            ReadmePath = getDestinationReadmePath destinationDirectory packageInfo;
            PackageXmlPath = getDestinationPackageXmlPath destinationDirectory packageInfo;
            Package = packageInfo;
        }

    let toDownloadUriUnsafe optionUri : Uri =
        match optionUri with
        |None -> raise (toException "Download Url not defined." None)
        |Some u ->  u

    let toReadmeDownloadInfo destinationDirectory (packageInfo:PackageInfo) =
        let readmeDownloadInfo = 
                result{                    
                    let! destinationReadmeFilePath = FileSystem.path (getDestinationReadmePath destinationDirectory packageInfo)
                    return {SourceUri = toDownloadUriUnsafe packageInfo.Readme.Url;SourceChecksum = packageInfo.Readme.Checksum; SourceFileSize = packageInfo.Readme.Size; DestinationFile = destinationReadmeFilePath;}
                }
        match readmeDownloadInfo with
        |Ok d -> Some d
        |Error ex -> 
            logger.Info("Failed to get download info for readme file. " + ex.Message)
            None
       
    let toInstallerDownloadInfo destinationDirectory (packageInfo:PackageInfo) =
        let installerDownloadInfo = 
                    result{                        
                        let! destinationInstallerFilePath = FileSystem.path (getDestinationInstallerPath destinationDirectory packageInfo)
                        return {SourceUri = toDownloadUriUnsafe packageInfo.Installer.Url;SourceChecksum = packageInfo.Installer.Checksum; SourceFileSize = packageInfo.Installer.Size; DestinationFile = destinationInstallerFilePath; }
                    }
        installerDownloadInfo

    let packageInfoToDownloadJobs destinationDirectory packageInfo =
        seq{
            let readmeDownloadInfo = toReadmeDownloadInfo destinationDirectory packageInfo                    
            match readmeDownloadInfo with
            |Some d -> yield d
            |None -> ()

            let installerDownloadInfo = toInstallerDownloadInfo destinationDirectory packageInfo
            match installerDownloadInfo with
            |Ok d -> yield d
            |Error ex -> logger.Error("Failed to get download info for installer file. " + ex.Message)        
        }

    let getDestinationFilePath destinationDirectory (packageFile:PackageFile) =
        if(String.IsNullOrWhiteSpace(packageFile.Name)) then
            Result.Error (new Exception(sprintf "Failed to get destination file path because package file name is blank. Package file: %A" packageFile))
        else
            FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue destinationDirectory, packageFile.Name))

    let optionToResult message option =
        match option with
        |Some v -> Result.Ok v
        |None -> Result.Error (new System.Exception(message))

    let resultToOption message result =
        match result with
        |Result.Ok v -> Some v
        |Result.Error (ex:Exception) ->
            logger.Warn(message + " " + ex.Message)
            None

    let packageFileToWebFileDownload (destinationDirectory:FileSystem.Path) (packageFile:PackageFile) =
        let webFileDownload =
            result{
                let! destinationFile = getDestinationFilePath destinationDirectory packageFile
                let webFileDestination = {DestinationFile=destinationFile}
                let! sourceUrl = optionToResult (sprintf "Failed to get source url for package file: %A" packageFile) packageFile.Url 
                let! webFileSource = (DriverTool.Library.Web.toWebFileSource sourceUrl.OriginalString packageFile.Checksum packageFile.Size) 
                let webFileDownload = {Source=webFileSource;Destination=webFileDestination}
                return webFileDownload            
            }
        resultToOption (sprintf "Failed to pepare download for package file: %A." packageFile) webFileDownload
            

    let packageInfoToWebFileDownloads destinationDirectory (packageInfo:PackageInfo) =
        seq{
            let readmeWebFileDownload = packageFileToWebFileDownload destinationDirectory packageInfo.Readme
            match readmeWebFileDownload with
            |Some v -> yield v
            |None -> ()

            let installerWebFileDownload =  packageFileToWebFileDownload destinationDirectory packageInfo.Installer
            match installerWebFileDownload with
            |Some v -> yield v
            |None -> ()
        }        

    /// <summary>
    /// Get files to download. As it is possible for two packages to share a readme file this function will return DownloadJobs with uniqe destination files.
    /// </summary>
    /// <param name="destinationDirectory"></param>
    /// <param name="packageInfos"></param>
    let packageInfosToDownloadJobs destinationDirectory packageInfos =        
        packageInfos
        |> Seq.map (fun p -> 
            packageInfoToDownloadJobs destinationDirectory p
            )
        |> Seq.concat
        //Make sure destination file is unique
        |> Seq.groupBy (fun p -> p.DestinationFile) 
        |> Seq.map (fun (_,v) -> v |>Seq.head)

    let toOptionalUri baseUrl fileName =
        match String.IsNullOrWhiteSpace(baseUrl) with
        |true -> None
        |false -> 
            match String.IsNullOrWhiteSpace(fileName) with
            |true -> 
                Some (new Uri(sprintf "%s" baseUrl))
            |false ->
                Some (new Uri(sprintf "%s/%s" baseUrl fileName))

    let toExternalFiles (packageXElement:XElement) (baseUrl:string) =
        let xExternal = packageXElement.Element(XName.Get("Files")).Element(XName.Get("External"))
        match xExternal with
        |null -> None
        | _ -> 
            let externalPackageFiles = 
                xExternal.Elements(XName.Get("File"))
                |> Seq.map (fun xf ->
                        let name = xf.Element(xn "Name").Value
                        let crc = xf.Element(xn "CRC").Value
                        let size = xf.Element(xn "Size").Value |> int64
                        {
                            Url = toOptionalUri baseUrl name
                            Name = name                            
                            Checksum = crc
                            Size = size
                            Type = External
                        }
                    )
                |> Seq.toArray
            Some externalPackageFiles

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
        let externalFiles = toExternalFiles packageXElement baseUrl
        {
            Name = name;
            Title = title;
            Version = version;
            Installer = 
                {
                    Url = toOptionalUri baseUrl installerName
                    Name = installerName
                    Checksum = installerCrc
                    Size = installerSize
                    Type = Installer
                }
            Readme = 
                {
                    Url = toOptionalUri baseUrl readmeName
                    Name = readmeName
                    Checksum = readmeCrc
                    Size = readmeSize
                    Type = Readme
                }            
            ExtractCommandLine = extractCommandLine;
            InstallCommandLine = installCommandLine;                
            Category = category;
            ReleaseDate = releaseDate;
            PackageXmlName = ((new System.IO.FileInfo(FileSystem.pathValue downloadedPackageInfo.FilePath)).Name)
            ExternalFiles = externalFiles
        }
    
    ///Keep only the two first words of the category.
    let truncateCategory (category:string) =
        let words = category.Split [|' '|] |>Array.filter(fun s -> not (s.Contains("and")))
        let truncatedWords = words.[0..1]
        truncatedWords |> String.concat " "

    ///Get folder name for the extracted update
    let getPackageFolderName category releaseDate =         
        let postfix = releaseDate
        let trucatedCategory = truncateCategory category
        let prefix = (trucatedCategory |? "Unknown_Category")
        let packageFolderName = 
            (sprintf "%s_%s" prefix postfix)
                .Replace("__", "_")
                .Replace("__", "_")
                .Replace(System.IO.Path.DirectorySeparatorChar.ToString(),"_")
                .Replace(System.IO.Path.AltDirectorySeparatorChar.ToString(),"_");                
        packageFolderName
    
    let downloadedPackageInfoToExtractedPackageInfo (packageFolderPath:FileSystem.Path,downloadedPackageInfo) =
        {
            ExtractedDirectoryPath = FileSystem.pathValue packageFolderPath;
            DownloadedPackage = downloadedPackageInfo;
        }

    let extractPackageXml (downloadedPackageInfo, packageFolderPath:FileSystem.Path)  =
        let destinationFilePath = FileSystem.pathUnSafe (System.IO.Path.Combine(FileSystem.pathValue packageFolderPath,downloadedPackageInfo.Package.PackageXmlName))
        match FileSystem.existingFilePathString downloadedPackageInfo.PackageXmlPath with
        |Ok filePath -> 
            match (FileOperations.copyFile true filePath destinationFilePath) with
            |Ok _ -> 
                Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let extractReadme (downloadedPackageInfo, packageFolderPath:FileSystem.Path)  =
        let destinationReadmeFilePath = FileSystem.pathUnSafe (System.IO.Path.Combine(FileSystem.pathValue packageFolderPath,downloadedPackageInfo.Package.Readme.Name))
        match FileSystem.existingFilePathString downloadedPackageInfo.ReadmePath with
        |Ok readmeFilePath -> 
            match (FileOperations.copyFile true readmeFilePath destinationReadmeFilePath) with
            |Ok _ -> 
                Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let getFileNameFromCommandLine (commandLine:string) = 
        let fileName = commandLine.Split(' ').[0];
        fileName;

    let extractInstaller (downloadedPackageInfo, packageFolderPath:FileSystem.Path) =
        let installCommandLineUseInstaller = downloadedPackageInfo.Package.InstallCommandLine.Contains(downloadedPackageInfo.Package.Installer.Name)
        if(String.IsNullOrWhiteSpace(downloadedPackageInfo.Package.ExtractCommandLine) || installCommandLineUseInstaller) then
           logger.Info("Installer does not support extraction, copy the installer directly to package folder...")
           let destinationInstallerFilePath = FileSystem.pathUnSafe (System.IO.Path.Combine(FileSystem.pathValue packageFolderPath,downloadedPackageInfo.Package.Installer.Name))
           match FileSystem.existingFilePathString downloadedPackageInfo.InstallerPath with
           |Ok installerPath -> 
                match (FileOperations.copyFile true installerPath destinationInstallerFilePath) with
                |Ok _ -> 
                    Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
                |Error ex -> Result.Error ex
           |Error ex -> 
                Result.Error ex
        else
            logger.Info("Installer supports extraction, extract installer...")
            let extractCommandLine = downloadedPackageInfo.Package.ExtractCommandLine.Replace("%PACKAGEPATH%",sprintf "\"%s\"" (FileSystem.pathValue packageFolderPath))
            let fileName = getFileNameFromCommandLine extractCommandLine
            let arguments = extractCommandLine.Replace(fileName,"")
            match (FileSystem.existingFilePathString downloadedPackageInfo.InstallerPath) with
            |Ok fp -> 
                match DriverTool.Library.ProcessOperations.startConsoleProcess (fp, arguments,FileSystem.pathValue packageFolderPath,-1,null,null,false) with
                |Ok _ -> Result.Ok (downloadedPackageInfoToExtractedPackageInfo (packageFolderPath,downloadedPackageInfo))
                |Error ex -> Result.Error ex
            |Error ex -> Result.Error ex

    let extractExternalFile cacheFolderPath packageFolderPath (packageFile:PackageFile) =
        result{
            let! sourceFile = PathOperations.combinePaths2 cacheFolderPath packageFile.Name
            let! targetFile = PathOperations.combinePaths2 packageFolderPath packageFile.Name
            let! result = FileOperations.copyFile true sourceFile targetFile
            return result
        }        

    let extractExternalFiles (downloadedPackageInfo:DownloadedPackageInfo) (packageFolderPath:FileSystem.Path) =
        result{
            let! installerPath = FileSystem.path downloadedPackageInfo.InstallerPath
            let cacheFolderPath = FileSystem.getDirectoryPath installerPath
            let! externalFiles = 
                match (downloadedPackageInfo.Package.ExternalFiles) with
                |Some externalFiles ->
                    externalFiles                    
                    |> Array.map (extractExternalFile cacheFolderPath packageFolderPath)
                    |> toAccumulatedResult
                |None -> Result.Ok Seq.empty
            return externalFiles
        }