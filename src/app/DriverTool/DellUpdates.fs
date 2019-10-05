namespace DriverTool

module DellUpdates=
    
    open System
    open System.Xml.Linq
    open DriverTool.PackageXml
    open DriverTool.DellCatalog
    open DriverTool.DellSettings
    open DriverTool.Web    
    open DriverTool.UpdatesContext
    open DriverTool.SdpCatalog    
    open FSharp.Collections.ParallelSeq

    type DellUpdates = class end
    let logger = Logging.getLoggerByName(typeof<DellUpdates>.Name)

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
                                Installer =
                                    {
                                        Url = toOptionalUri originFile.OriginUri ""
                                        Name = DriverTool.SdpUpdates.toInstallerName ii.InstallerData
                                        Checksum = (Checksum.base64StringToFileHash originFile.Digest)|>Checksum.fileHashToString
                                        Size = originFile.Size
                                        Type = Installer
                                    }                                
                                ExtractCommandLine = ""
                                InstallCommandLine = DriverTool.SdpUpdates.toInstallerCommandLine ii.InstallerData
                                Category = sdp.ProductName
                                Readme =
                                    {
                                        Url = toOptionalUri sdp.MoreInfoUrl ""
                                        Name = Web.getFileNameFromUrl sdp.MoreInfoUrl
                                        Checksum = ""
                                        Size = 0L
                                        Type = Readme
                                    }                                
                                ReleaseDate= (getSdpReleaseDate sdp)|>toDateString
                                PackageXmlName=sdp.PackageId + ".sdp";
                           }
                    )
                |>Seq.toArray
            pacakgeInfos

    let getLocalUpdates (context:UpdatesRetrievalContext) =
        result{
            let! supported = DriverTool.SdpUpdates.validateModelAndOs context.Model context.OperatingSystem
            let! sdpFiles = downloadSdpFiles()
            let! sdps = DriverTool.SdpUpdates.loadSdps sdpFiles
            let mutable count = 0
            let packageInfos = 
                sdps
                |>Seq.map(fun p -> 
                    count <- count + 1 
                    printf "%i\r" count
                    p
                    )
                |>PSeq.filter DriverTool.SdpUpdates.localUpdatesFilter
                |>PSeq.toArray
                |>(DriverTool.SdpUpdates.sdpsToPacakgeInfos context toPackageInfos)
            let! copyResult =  DriverTool.SdpUpdates.copySdpFilesToDownloadCache packageInfos sdpFiles            
            return packageInfos
        }

    let printCount count totalCount p =        
        printf "%i of %i\r" count totalCount
        p
        
    let getRemoteUpdates (context:UpdatesRetrievalContext) =
        result{
            let! supported = DriverTool.SdpUpdates.validateModelAndOs context.Model context.OperatingSystem
            let! sdpFiles = downloadSdpFiles()
            let! sdps = DriverTool.SdpUpdates.loadSdps sdpFiles            
            let mutable count = 0
            let packageInfos = 
                sdps                
                |>Seq.map(fun p -> 
                    count <- count + 1 
                    printf "%i\r" count
                    p
                    )
                |>PSeq.filter DriverTool.SdpUpdates.remoteUpdatesFilter
                |>PSeq.toArray
                |>(DriverTool.SdpUpdates.sdpsToPacakgeInfos context toPackageInfos)
            let! copyResult =  DriverTool.SdpUpdates.copySdpFilesToDownloadCache packageInfos sdpFiles
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

    let getSccmDriverPackageInfo (modelCode:ModelCode, operatingSystemCode:OperatingSystemCode, cacheFolderPath:FileSystem.Path)  : Result<SccmPackageInfo,Exception> =
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
        result{
            let! installerPath = FileSystem.path downloadedSccmPackage.InstallerPath
            let! cabFilepath = FileOperations.ensureFileExtension ".cab" installerPath
            let! existingCabFilePath = FileOperations.ensureFileExists cabFilepath            
            let! expandResult = DriverTool.DellCatalog.expandCabFile (existingCabFilePath,destinationPath)

            let! copiedReadmeFilePath = 
                FileOperations.copyFileIfExists downloadedSccmPackage.ReadmePath destinationPath

            return (destinationPath,copiedReadmeFilePath)
        }

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

    let getCategoryFromInstallerName installerName defaultCategory =
        match installerName with
        |Regex "Firmware" [] -> "Firmware"
        |Regex "Audio.+Driver" [] -> "Audio-Driver"
        |Regex "Chipset.+Driver" [] -> "Chipset"
        |Regex "Chipset.+Software" [] -> "Chipset"
        |Regex "Controller.+Driver" [] -> "Chipset"
        |Regex "Thermal.+Framework" [] -> "Chipset"        
        |Regex "Card.+Reader.+Driver" [] -> "Card-Reader"        
        |Regex "Docks.+Driver" [] -> "Docks"
        |Regex "Input.+Driver" [] -> "Input"
        |Regex "Touchpad.+Driver" [] -> "Input"        
        |Regex "HID.+Event.+Filter.+Driver" [] -> "Input"
        |Regex "Communications.+Driver" [] -> "Network"
        |Regex "GPS.+driver" [] -> "Network"
        |Regex "Ethernet.+Driver" [] -> "Network"
        |Regex "Ethernet.+Controller" [] -> "Network"
        |Regex "WiFi.+Driver" [] -> "Network"
        |Regex "WiGig.+Driver" [] -> "Network"
        |Regex "Network.+Driver" [] -> "Network"
        |Regex "Bluetooth.+Driver" [] -> "Network"
        |Regex "Serial-ATA.+Driver" [] -> "Storage"
        |Regex "Storage.+Driver" [] -> "Storage"
        |Regex "Video.+Driver" [] -> "Video"
        |Regex "Graphics.+Driver" [] -> "Video"
        |Regex "Application" [] -> "Software"
        |Regex "Service" [] -> "Software"        
        |_ -> defaultCategory
    
    /// <summary>
    /// Dell. Decuct category from the installer name and update the package info category. The sdp only have "Drivers and Applications" as category.
    /// </summary>
    /// <param name="downloadedUpdates"></param>
    let updateDownloadedPackageInfo downloadedUpdates =
        result
            {
                let! updatedUpdates = 
                    downloadedUpdates
                    |>Array.map(fun d ->
                                result
                                    {                                        
                                        let category = getCategoryFromInstallerName d.Package.Installer.Name d.Package.Category
                                        let up = {d.Package with Category = category}
                                        let ud = {d with Package = up}
                                        return ud
                                    }
                            )
                    |>toAccumulatedResult
                return (updatedUpdates |> Seq.toArray |> Array.sortBy (fun dp -> packageInfoSortKey dp.Package))
            }