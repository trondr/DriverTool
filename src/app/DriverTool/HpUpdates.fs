namespace DriverTool

module HpUpdates =
    open System
    open PackageXml
    open DriverTool.Web
    open DriverTool.HpCatalog
    open System.Xml.Linq
    open DriverTool.SdpCatalog
        
    let toDateString (dateTime:DateTime) =
        dateTime.ToString("yyyy-MM-dd")

    let sdpNs =
        XNamespace.Get("http://schemas.microsoft.com/wsus/2005/04/CorporatePublishing/SoftwareDistributionPackage.xsd")
    let cmdNs =
        XNamespace.Get("http://schemas.microsoft.com/wsus/2005/04/CorporatePublishing/Installers/CommandLineInstallation.xsd")
    
    let getAttribute (xElement:XElement) (attributeName:string) =        
        match xElement with
        | null -> None
        |_ -> 
            let attribute = xElement.Attribute(XName.Get(attributeName))            
            match attribute with
            |null -> None
            |_ -> Some attribute.Value
    
    let toInt (value:string option) =
        match value with
        |None -> 1
        |Some v -> Convert.ToInt32(v)
    
    let toLong (value:string option) defaultValue =
        match value with
        |None -> defaultValue
        |Some v -> Convert.ToInt64(v)

    let toDateTime (value:string option) defaultValue =
        match value with
        |None -> defaultValue
        |Some v -> DateTime.Parse(v)

    let toUri (value:string option) defaultValue =
        match value with
        |None -> defaultValue()
        |Some v -> new Uri(v)
    
    let toBoolean (value:string option) =
        match value with
        |None -> false
        |Some v -> 
            match v with
            |"true" -> true
            |"false" -> false
            |_ -> failwith (sprintf "Invalid boolean '%s'. Valid values: [true|false]" v)

    let toInstallationResult (value:string option) =
        match value with
        |None -> InstallationResult.Failed
        |Some v -> 
            match v with
            |"Failed" -> Failed
            |"Succeeded" -> Succeeded
            |"Cancelled" -> Cancelled
            |_ -> failwith (sprintf "Invalid installation result '%s'. Valid installation result: [Failed|Succeeded|Cancelled]" v)

    let toString (value:string option) (defaultString:string) =
        match value with
        |None -> defaultString
        |Some v -> v

    let toReturnCodes (installerData:XElement) =
        let returnCodes = installerData.Elements(XName.Get("ReturnCode",cmdNs.NamespaceName))
        let commandLineReturnCodes =
            returnCodes
            |>Seq.map(fun rx -> 
                    {
                        Code=toInt (getAttribute rx "Code");
                        Result=toInstallationResult (getAttribute rx "Result");
                        Reboot=toBoolean (getAttribute rx "Reboot");
                    }    
                )
            |> Seq.toArray
        commandLineReturnCodes
 
    let toCommandLineInstallerData (installerItem:XElement) =
        let commandLineInstallerDataXElement = installerItem.Element(XName.Get("CommandLineInstallerData",cmdNs.NamespaceName))
        let program = getAttribute commandLineInstallerDataXElement "Program"
        let arguments = getAttribute commandLineInstallerDataXElement "Arguments"
        let defaultResult = getAttribute commandLineInstallerDataXElement "DefaultResult"
        let rebootByDefault = getAttribute commandLineInstallerDataXElement "RebootByDefault"
        let returnCodes = toReturnCodes commandLineInstallerDataXElement
        {
            Program = toString program "";
            Arguments = toString arguments "";
            DefaultResult=toInstallationResult defaultResult;
            RebootByDefault=toBoolean rebootByDefault;
            ReturnCodes=returnCodes;
        } 
        
    type OriginFile = {
            Digest:string;
            FileName:string;
            Size:Int64;
            Modified:DateTime;
            OriginUri:Uri
        }   

    let toOriginFile (installerItem:XElement)  = 
        let originFileXElement = installerItem.Element(XName.Get("OriginFile",sdpNs.NamespaceName))
        let digest = getAttribute originFileXElement "Digest"
        let fileName = getAttribute originFileXElement "FileName"
        let size = getAttribute originFileXElement "Size"
        let modified = getAttribute originFileXElement "Modified"
        let originUri = getAttribute originFileXElement "OriginUri"
        {
            Digest=toString digest "";
            FileName=toString fileName "";
            Size=toLong size 0L;
            Modified=toDateTime modified DateTime.MinValue
            OriginUri=toUri originUri (fun _ -> failwith "OriginUri attribute not found.")
        }

    let getBaseUrl (uri:Uri) =
        let fileName = uri.Segments.[uri.Segments.Length-1]
        uri.OriginalString.Replace(fileName,"").TrimEnd([|'/'|])

    let toPackageInfo (sdpXmlFile: string, logDirectory:string) : PackageInfo[] =
        let sdp = sdpeval.Sdp.LoadSdp(sdpXmlFile)
        let xDocument = XDocument.Load(sdpXmlFile)
        let packageInfos = 
            xDocument.Descendants(XName.Get("InstallableItem",sdpNs.NamespaceName))
            |>Seq.map(fun ii -> 
                           
                           let commandLineInstallerData = toCommandLineInstallerData ii
                           let originFile = toOriginFile ii
                           {
                                Name = sdp.PackageId.ToString();
                                Title = sdp.Title;
                                Version = "";
                                BaseUrl = getBaseUrl originFile.OriginUri;
                                InstallerName = commandLineInstallerData.Program
                                InstallerCrc = (Checksum.base64StringToFileHash originFile.Digest)|>Checksum.fileHashToString
                                InstallerSize = originFile.Size
                                ExtractCommandLine = ""
                                InstallCommandLine = (sprintf "\"%s\" %s" commandLineInstallerData.Program commandLineInstallerData.Arguments) 
                                Category = sdp.ProductNames.[0]
                                ReadmeName = "";
                                ReadmeCrc = "";
                                ReadmeSize=0L;
                                ReleaseDate= sdp.CreationDate|>toDateString
                                PackageXmlName="";
                           }
                )
            |>Seq.toArray
        packageInfos

    let validateModelAndOs (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) =
        result{
            let! currentModelCode = ModelCode.create "" true
            let!supportedModelCode = 
                if(modelCode.Value <> currentModelCode.Value) then
                    Result.Error (new Exception(sprintf "DriverTool can only create HP driver package for the current model '%s' as filtering is done with live queries on current system." currentModelCode.Value))
                else
                    Result.Ok currentModelCode
            let! currentOperatingSystem = OperatingSystemCode.create "" true            
            let!supporteOperatingSystemCode = 
                if(operatingSystemCode.Value <> currentOperatingSystem.Value) then
                    Result.Error (new Exception(sprintf "DriverTool can only create HP driver package for the running operating system '%s' as filtering is done with live queries on current system." currentOperatingSystem.Value))
                else
                    Result.Ok currentOperatingSystem
            return (supportedModelCode, supporteOperatingSystemCode)
        }

    let getLocalUpdates (modelCode: ModelCode, operatingSystemCode: OperatingSystemCode, overwrite,logDirectory:string) =
        result{
            let! supported = validateModelAndOs modelCode operatingSystemCode
            let! hpCatalogForSmsLatest = HpCatalog.downloadSmsSdpCatalog()
            let! hpCatalogForSmsLatestV2 = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue hpCatalogForSmsLatest,"V2"))
            let! existingHpCatalogForSmsLatestV2 = DirectoryOperations.ensureDirectoryExists false hpCatalogForSmsLatestV2
            let! sdpFiles = DirectoryOperations.findFiles false "*.sdp" existingHpCatalogForSmsLatestV2
            let relevantSdpFiles = 
                sdpFiles
                |>Seq.toArray
                |>Array.filter(fun sf-> 
                        let sdp = sdpeval.Sdp.LoadSdp (FileSystem.pathValue sf)
                        sdp.InstallableItems
                        |>Seq.tryFind(fun ii -> 
                                let isInstallable = sdpeval.Sdp.EvaluateApplicabilityXml(ii.IsInstallableApplicabilityRule)
                                let isInstalled =  sdpeval.Sdp.EvaluateApplicabilityXml(ii.IsInstalledApplicabilityRule)
                                (isInstallable && isInstalled)
                            )
                        |>optionToBoolean
                    )
            let packageInfos =
                relevantSdpFiles
                |>Array.map(fun sf -> toPackageInfo ((FileSystem.pathValue sf),logDirectory))
                |>Array.concat
            return packageInfos
        }        

    let getRemoteUpdates (modelCode: ModelCode, operatingSystemCode: OperatingSystemCode, overwrite,logDirectory:string) =
        result{
            let! supported = validateModelAndOs modelCode operatingSystemCode
            let! hpCatalogForSmsLatest = HpCatalog.downloadSmsSdpCatalog()
            let! hpCatalogForSmsLatestV2 = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue hpCatalogForSmsLatest,"V2"))
            let! existingHpCatalogForSmsLatestV2 = DirectoryOperations.ensureDirectoryExists false hpCatalogForSmsLatestV2
            let! sdpFiles = (DirectoryOperations.findFiles false "*.sdp" existingHpCatalogForSmsLatestV2)
            let relevantSdpFiles = 
                sdpFiles
                |>Seq.toArray
                |>Array.filter(fun sf-> 
                        let sdp = sdpeval.Sdp.LoadSdp (FileSystem.pathValue sf)
                        sdp.InstallableItems
                        |>Seq.tryFind(fun ii -> 
                                let isInstallable = sdpeval.Sdp.EvaluateApplicabilityXml(ii.IsInstallableApplicabilityRule)                                
                                isInstallable
                            )
                        |>optionToBoolean
                    )
            let packageInfos =
                relevantSdpFiles
                |>Array.map(fun sf -> toPackageInfo ((FileSystem.pathValue sf),logDirectory))
                |>Array.concat
            return packageInfos
        }

    let getSccmDriverPackageInfo (modelCode: ModelCode, operatingSystemCode:OperatingSystemCode) =
        result{
            let! driverPackCatalogXmlFilePath = downloadDriverPackCatalog()
            let! sccmPackageInfo = getSccmDriverPackageInfoBase (driverPackCatalogXmlFilePath, modelCode, operatingSystemCode)
            return sccmPackageInfo
        }

    let downloadSccmPackage (cacheDirectory, sccmPackage:SccmPackageInfo) =
        result{                        
            let! installerdestinationFilePath = FileSystem.path (System.IO.Path.Combine(cacheDirectory,sccmPackage.InstallerFileName))
            let! installerUri = DriverTool.Web.toUri sccmPackage.InstallerUrl
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
        logger.Info("Extract Sccm Driver Package ...")
        match(result{
            let! installerPath = FileSystem.path downloadedSccmPackage.InstallerPath
            let! exeFilepath = FileOperations.ensureFileExtension ".exe" installerPath
            let! existingExeFilePath = FileOperations.ensureFileExists exeFilepath  
            let arguments = sprintf "-PDF -F \"%s\" -S -E" (FileSystem.pathValue destinationPath)
            let! exitCode = ProcessOperations.startConsoleProcess (installerPath,arguments,FileSystem.pathValue destinationPath,-1,null,null,false)
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
            let result = 
                [|extractInstallerResult|]
                |> toAccumulatedResult
            let res = 
                match result with 
                | Ok r -> extractInstallerResult
                | Error ex -> Result.Error ex
            return! res
        }
        //raise (new NotImplementedException("HpUpdates.extractUpdate"))