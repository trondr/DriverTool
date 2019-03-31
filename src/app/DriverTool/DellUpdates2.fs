namespace DriverTool

module DellUpdates2=
    
    open System
    open System.Xml.Linq
    open DriverTool.PackageXml
    open DriverTool.DellCatalog
    open DriverTool.DellSettings
    open DriverTool.Web    
    open DriverTool.UpdatesContext
    open DriverTool.SdpCatalog
    open DriverTool.SdpUpdates

    type DellUpdates2 = class end
    let logger = Logging.getLoggerByName(typeof<DellUpdates2>.Name)

    let downloadSdpFiles () =
        result
            {
                let! dellCatalogForSms = DellCatalog.downloadSmsSdpCatalog()
                let! dellCatalogForSmsV2 = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue dellCatalogForSms,"V2"))
                let! existingDellCatalogForSmsV2 = DirectoryOperations.ensureDirectoryExists false dellCatalogForSmsV2
                let! sdpFiles = DirectoryOperations.findFiles false "*.sdp" existingDellCatalogForSmsV2
                return sdpFiles
            }

    let toPackageInfos sdp  =
            let pacakgeInfos =
                sdp.InstallableItems
                |>Seq.map(fun ii ->
                           let originFile = ii.OriginFile
                           {
                                Name = sdp.PackageId;
                                Title = sdp.Title;
                                Version = "";
                                BaseUrl = Web.getFolderNameFromUrl originFile.OriginUri
                                InstallerName = toInstallerName ii.InstallerData
                                InstallerCrc = (Checksum.base64StringToFileHash originFile.Digest)|>Checksum.fileHashToString
                                InstallerSize = originFile.Size
                                ExtractCommandLine = ""
                                InstallCommandLine = toInstallerCommandLine ii.InstallerData
                                Category = sdp.ProductName
                                ReadmeName = Web.getFileNameFromUrl sdp.MoreInfoUrl;
                                ReadmeCrc = "";
                                ReadmeSize=0L;
                                ReleaseDate= (getSdpReleaseDate sdp)|>toDateString
                                PackageXmlName=sdp.PackageId + ".sdp";
                           }
                    )
                |>Seq.toArray
            pacakgeInfos

    let getLocalUpdates (context:UpdatesRetrievalContext) =
        result{
            let! supported = validateModelAndOs context.Model context.OperatingSystem
            let! sdpFiles = downloadSdpFiles()
            let! sdps = loadSdps sdpFiles
            let packageInfos = 
                sdps                
                |>Seq.filter localUpdatesFilter
                |>Seq.toArray
                |>(sdpsToPacakgeInfos context toPackageInfos)
            let! copyResult =  copySdpFilesToDownloadCache packageInfos sdpFiles            
            return packageInfos
        }
        
    let getRemoteUpdates (context:UpdatesRetrievalContext) =
        result{
            let! supported = validateModelAndOs context.Model context.OperatingSystem
            let! sdpFiles = downloadSdpFiles()
            let! sdps = loadSdps sdpFiles
            let packageInfos = 
                sdps                
                |>Seq.filter remoteUpdatesFilter
                |>Seq.toArray
                |>(sdpsToPacakgeInfos context toPackageInfos)
            
            let! copyResult =  copySdpFilesToDownloadCache packageInfos sdpFiles
            return packageInfos
        }

    let osCodeToDellOsCodeAndArchitectureUnsafe (operatingSystemCode:OperatingSystemCode) =
        match operatingSystemCode.Value with
        |"WIN10X64" -> ("Windows10","x64") //Microsoft Windows 10 Pro X64
        |"WIN10X86" -> ("Windows10","x86") //Microsoft Windows 10 Pro X64
        |_ -> raise (new Exception(sprintf "Failed to convert os short name '%s' to Dell oscode. Only WIN10X64 and WIN10X86 are supported os shortnames." operatingSystemCode.Value))

    let osCodeToDellOsCodeAndArchitecture (operatingSystemCode:OperatingSystemCode) =
        tryCatch osCodeToDellOsCodeAndArchitectureUnsafe operatingSystemCode

    let driverPackageXNamespace =
        XNamespace.Get("openmanage/cm/dm")

    let isSupportedForModel2 (softwareComponent:XElement, modelCode:ModelCode) =
        softwareComponent
            .Descendants(XName.Get("SupportedSystems",driverPackageXNamespace.NamespaceName))
            .Descendants(XName.Get("Brand",driverPackageXNamespace.NamespaceName))
        |>Seq.tryFind(fun brand-> 
                        brand.Descendants(XName.Get("Model",driverPackageXNamespace.NamespaceName))
                        |>Seq.tryFind(fun model-> 
                                        let systemId = model.Attribute(XName.Get("systemID")).Value                                        
                                        systemId = modelCode.Value
                                    )
                        |>optionToBoolean
                    )
        |>optionToBoolean
    
    let getAttribute (xElement:XElement, attributeName:string) =        
        xElement.Attribute(XName.Get(attributeName)).Value

    let isSupportedForOsAndArchitecture (dp:XElement,dellOsCode,dellOsArchitecture) =
        dp
            .Descendants(XName.Get("SupportedOperatingSystems",driverPackageXNamespace.NamespaceName))
            .Descendants(XName.Get("OperatingSystem",driverPackageXNamespace.NamespaceName))
        |>Seq.tryFind(fun os -> 
                        let osCode = (getAttribute (os, "osCode"))
                        let osArch = (getAttribute (os,"osArch"))
                        osCode = dellOsCode && osArch = dellOsArchitecture
                        )
        |>optionToBoolean
    
    let pathToDirectoryAndFile (path:string) =        
        match path with
        |Regex @"^(.+?)([^/]+)$" [directory;file] -> 
            (directory.Trim('/'),file)
        |_ -> raise (new Exception("Failed to get directory and file path from path: "+ path ))        

    let toSccmPackageInfo (dp:XElement) (operatingSystemCode:OperatingSystemCode) : SccmPackageInfo =
        let path = getAttribute(dp,"path")        
        let (directory, installerName) = pathToDirectoryAndFile path
        {
            ReadmeFile =
                {
                    Url="";
                    Checksum="";
                    FileName="";
                    Size=0L;
                }
            InstallerUrl=downloadsBaseUrl + "/" + path;
            InstallerChecksum=getAttribute (dp,"hashMD5");
            InstallerFileName=installerName;
            Released=(getAttribute (dp,"dateTime"))|>DateTime.Parse;
            Os=operatingSystemCode.Value;
            OsBuild="";
        }

    let getSccmDriverPackageInfo (modelCode:ModelCode, operatingSystemCode:OperatingSystemCode)  : Result<SccmPackageInfo,Exception> =
        result{
            let! driverPackageCatalogXmlPath = downloadDriverPackageCatalog ()
            let xDocument = XDocument.Load(FileSystem.pathValue driverPackageCatalogXmlPath)            
            let! (dellOsCode, dellOsArchitecture) = osCodeToDellOsCodeAndArchitecture operatingSystemCode            
            let driverPackage = 
                xDocument.Descendants(XName.Get("DriverPackage",driverPackageXNamespace.NamespaceName))
                |>Seq.filter(fun dp -> isSupportedForModel2(dp, modelCode))                
                |>Seq.filter(fun dp -> isSupportedForOsAndArchitecture (dp,dellOsCode,dellOsArchitecture))
                |>Seq.toArray
                |>Seq.map(fun dp -> toSccmPackageInfo dp operatingSystemCode)                
                |>Seq.tryFind(fun _ -> true)
            let! sccmPackageInfo =
                match driverPackage with
                |Some dpi -> Result.Ok dpi
                |None -> Result.Error (new Exception(sprintf "Failed to find Dell sccm driver package for model '%s' and operating system '%s' " modelCode.Value operatingSystemCode.Value))
            return sccmPackageInfo
        }
    
    let downloadSccmPackage (cacheDirectory, sccmPackage:SccmPackageInfo) =
        result{                        
            let! installerdestinationFilePath = FileSystem.path (System.IO.Path.Combine(cacheDirectory,sccmPackage.InstallerFileName))
            let! installerUri = toUri sccmPackage.InstallerUrl
            let installerDownloadInfo = { SourceUri = installerUri;SourceChecksum = sccmPackage.InstallerChecksum;SourceFileSize = 0L;DestinationFile = installerdestinationFilePath}
            let! installerInfo = Web.downloadIfDifferent (installerDownloadInfo,false)
            let installerPath = FileSystem.pathValue installerInfo.DestinationFile

            return {
                InstallerPath = installerPath
                ReadmePath = String.Empty
                SccmPackage = sccmPackage;
            }
        }       
        
    let extractSccmPackage (downloadedSccmPackage:DownloadedSccmPackageInfo, destinationPath:FileSystem.Path) =
        logger.Info("Extract Sccm Driver Package CAB...")
        match(result{
            let! installerPath = FileSystem.path downloadedSccmPackage.InstallerPath
            let! cabFilepath = FileOperations.ensureFileExtension ".cab" installerPath
            let! existingCabFilePath = FileOperations.ensureFileExists cabFilepath            
            let! expandResult = DriverTool.DellCatalog.expandCabFile (existingCabFilePath,destinationPath)
            return destinationPath
        }) with
        |Ok _ -> Result.Ok destinationPath
        |Error ex -> Result.Error (new Exception("Failed to extract Sccm package. " + ex.Message, ex))        

    let extractUpdate (rootDirectory:FileSystem.Path, (prefix,downloadedPackageInfo:DownloadedPackageInfo)) =
        result{
            let packageFolderName = getPackageFolderName downloadedPackageInfo.Package.Category downloadedPackageInfo.Package.ReleaseDate
            let! packageFolderPath = DriverTool.PathOperations.combine2Paths (FileSystem.pathValue rootDirectory, prefix + "_" + packageFolderName)
            let! existingPackageFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmpty (packageFolderPath, true)            
            let extractInstallerResult = extractInstaller (downloadedPackageInfo, existingPackageFolderPath)
            let extractReadmeResult = extractReadme (downloadedPackageInfo, existingPackageFolderPath)
            let extractPacakgeXmlResult = extractPackageXml (downloadedPackageInfo,existingPackageFolderPath)
            let! result = 
                [|extractInstallerResult;extractReadmeResult;extractPacakgeXmlResult|]
                |> toAccumulatedResult
            return result |>Seq.toArray |> Seq.head
        }

    let updateDownloadedPackageInfo downloadedUpdates =
        result
            {
                return downloadedUpdates        
            }