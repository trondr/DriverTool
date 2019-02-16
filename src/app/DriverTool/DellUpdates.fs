namespace DriverTool

module DellUpdates =
    type DellUpdates = class end
    let loggerd = Logging.getLoggerByName(typeof<DellUpdates>.Name)

    let softwareCatalogCab = "http://downloads.dell.com/catalog/CatalogPC.cab"
    let driverPackageCatalogCab = "http://downloads.dell.com/catalog/DriverPackCatalog.cab"

    open F
    open FSharp.Data
    open System
    open DriverTool.Configuration
    open DellSettings

    let getLocalDellSoftwareCatalogCabFilePath =
        FileSystem.path (System.IO.Path.Combine(getDownloadCacheDirectoryPath,"CatalogPC.cab"))

    let getLocalDellSoftwareCatalogXmlFilePath =
        FileSystem.path (System.IO.Path.Combine(getDownloadCacheDirectoryPath,"CatalogPC.xml"))
    
    let expandExe =
        System.IO.Path.Combine(DriverTool.Environment.nativeSystemFolder,"expand.exe")

    let expandCabFile (cabFilePath:FileSystem.Path, destinationFolderPath:FileSystem.Path) =
        result{
            let! expandExePath = FileSystem.path expandExe
            let arguments = sprintf "\"%s\" -F:* -R \"%s\"" (FileSystem.pathValue cabFilePath) (FileSystem.pathValue destinationFolderPath)
            let! expandResult = ProcessOperations.startConsoleProcess (expandExePath, arguments, FileSystem.pathValue destinationFolderPath,-1,null,null,false)            
            return expandResult
        }

    let downloadSoftwareComponentsCatalog () =
        result {
            let! destinationCabFile = getLocalDellSoftwareCatalogCabFilePath
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist true destinationCabFile
            let! downloadResult = Web.downloadFile (new Uri(softwareCatalogCab), true, nonExistingDestinationCabFile)
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)
            let! destinationFolderPath = FileSystem.path getDownloadCacheDirectoryPath
            let! expandResult = expandCabFile (existingDestinationCabFile, destinationFolderPath)
            let! softwareCatalogXmlPath = getLocalDellSoftwareCatalogXmlFilePath
            let! existingSoftwareCatalogXmlPath = FileOperations.ensureFileExists softwareCatalogXmlPath            
            return existingSoftwareCatalogXmlPath
        }

    open DriverTool.PackageXml
    open System.Linq

    let pathToDirectoryAndFile (path:string) =        
        match path with
        |Regex @"^(.+?)([^/]+)$" [directory;file] -> 
            (directory.Trim('/'),file)
        |_ -> raise (new Exception("Failed to get directory and file path from path: "+ path ))        

    let operatingSystemCodeToDellOsCodeUnsafe (operatingSystemCode:OperatingSystemCode) =
        match operatingSystemCode.Value with
        |"WIN10X64" -> "W10P4" //Microsoft Windows 10 Pro X64
        |"WIN10X86" -> "W10P2" //Microsoft Windows 10 Pro X64
        |_ -> raise (new Exception(sprintf "Failed to convert os short name '%s' to Dell oscode. Only WIN10X64 and WIN10X86 are supported os shortnames." operatingSystemCode.Value))
    
    let operatingSystemCodeToDellOsCode (operatingSystemCode:OperatingSystemCode) =
        tryCatch operatingSystemCodeToDellOsCodeUnsafe operatingSystemCode

    open System.Xml.Linq    

    let isSupportedForModel (softwareComponent:XElement, modelCode:ModelCode) =
        softwareComponent
            .Descendants(XName.Get("SupportedSystems"))
            .Descendants(XName.Get("Brand"))
        |>Seq.tryFind(fun brand-> 
                        brand.Descendants(XName.Get("Model"))
                        |>Seq.tryFind(fun model-> 
                                        let systemId = model.Attribute(XName.Get("systemID")).Value                                        
                                        systemId = modelCode.Value
                                    )
                        |>optionToBoolean
                    )
        |>optionToBoolean
    
    let isSupportedForOs (softwareComponent:XElement, dellOsCode) =        
        softwareComponent
            .Descendants(XName.Get("SupportedOperatingSystems"))
            .Descendants(XName.Get("OperatingSystem"))
        |>Seq.tryFind(fun os -> 
                        let osCode = os.Attribute(XName.Get("osCode")).Value
                        osCode = dellOsCode
                        )
        |>optionToBoolean

    let getAttribute (xElement:XElement, attributeName:string) =        
        xElement.Attribute(XName.Get(attributeName)).Value
    
    let getElementValue (xElement:XElement, elementName:string) =
        xElement
            .Element(XName.Get(elementName))
            .Element(XName.Get("Display"))
            .Value

    let toDateString (dateTimeString:string) =
        DateTime.Parse(dateTimeString).ToString("yyyy-MM-dd")

    let toName (name:string, vendorVersion:string, dellVersion:string) =
        name
            .Replace(","+vendorVersion,"")
            .Replace(","+dellVersion,"")
       
    let toVersion (versionString) = 
        (new System.Version(versionString))

    let getLatestPackageInfoVersion (packageInfos:seq<PackageInfo>) =
        packageInfos
        |>Seq.groupBy(fun p -> p.Name)
        |>Seq.map(fun (_,ps) -> 
                       ps
                       |>Seq.maxBy(fun p -> 
                                    toVersion(p.Version)                                    
                                )
                  )

    let toPackageInfo (sc: XElement,logDirectory:string) =
        let path = getAttribute(sc,"path")
        let dellVersion = getAttribute(sc,"dellVersion")
        let vendorVersion = getAttribute (sc, "vendorVersion")
        let name = toName (getElementValue (sc,"Name"),vendorVersion,dellVersion)        
        let (directory, installerName) = pathToDirectoryAndFile path            
        {
            Name = name
            Title = name
            Version = getAttribute (sc, "vendorVersion")
            BaseUrl = downloadsBaseUrl + "/" + directory;
            InstallerName = installerName
            InstallerCrc = getAttribute (sc,"hashMD5")
            InstallerSize = int64 (getAttribute (sc,"size"))
            ExtractCommandLine = ""
            InstallCommandLine = (sprintf "\"%s\" /s /l=\"%s\\DUP_%s.log\"" installerName logDirectory installerName)
            Category = getElementValue (sc,"Category")
            ReadmeName = "";
            ReadmeCrc = "";
            ReadmeSize=0L;
            ReleaseDate= (getAttribute (sc,"dateTime"))|>toDateString
            PackageXmlName="";
        }

    let getRemoteUpdates (modelCode:ModelCode, operatingSystemCode:OperatingSystemCode,overwrite:bool,logDirectory:string) =
        result{
            let! softwareCatalogXmlFile = downloadSoftwareComponentsCatalog()            
            let! dellOsCode = operatingSystemCodeToDellOsCode operatingSystemCode            
            let softwareComponentsXDocument = XDocument.Load(FileSystem.pathValue softwareCatalogXmlFile)
            let manifestRoot = softwareComponentsXDocument.Root            
            let softwareComponents = manifestRoot.Descendants(XName.Get("SoftwareComponent"))
            let updates =
                softwareComponents
                |>Seq.filter(fun sc -> isSupportedForModel (sc,modelCode))                
                |>Seq.filter(fun sc -> isSupportedForOs (sc,dellOsCode))
                |>Seq.map(fun sc -> toPackageInfo (sc,logDirectory))
                |>getLatestPackageInfoVersion
                |>Seq.toArray
            loggerd.Info(sprintf "Updates: %i" updates.Length)
            return updates
        }

    let getLocalUpdates (modelCode:ModelCode, operatingSystemCode:OperatingSystemCode,overwrite:bool,logDirectory:string) : Result<PackageInfo[],Exception> =
        result
            {
                let! remoteUpdates = getRemoteUpdates (modelCode, operatingSystemCode,overwrite,logDirectory)
                let! localUpdates = DellCommandUpdate.getLocalUpdates (modelCode,operatingSystemCode,remoteUpdates)
                return localUpdates
            }
        
    let getLocalDriverPackageCatalogCabFilePath =
        FileSystem.path (System.IO.Path.Combine(getDownloadCacheDirectoryPath,"DriverPackCatalog.cab"))

    let getLocalDriverPackageXmlFilePath =
        FileSystem.path (System.IO.Path.Combine(getDownloadCacheDirectoryPath,"DriverPackCatalog.xml"))

    let downloadDriverPackageCatalog () =
        result {
            let! destinationCabFile = getLocalDriverPackageCatalogCabFilePath
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist true destinationCabFile
            let! downloadResult = Web.downloadFile (new Uri(driverPackageCatalogCab), true, nonExistingDestinationCabFile)
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)
            let! destinationFolderPath = FileSystem.path getDownloadCacheDirectoryPath
            let! expandResult = expandCabFile (existingDestinationCabFile, destinationFolderPath)
            let! driverPackageCatalogXmlPath = getLocalDriverPackageXmlFilePath
            let! existingDriverPackageCatalogXmlPath = FileOperations.ensureFileExists driverPackageCatalogXmlPath            
            return existingDriverPackageCatalogXmlPath
        }
    
    open DriverTool.Web

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

    open DriverTool.Web

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
        loggerd.Info("Extract Sccm Driver Package CAB...")
        match(result{
            let! installerPath = FileSystem.path downloadedSccmPackage.InstallerPath
            let! cabFilepath = FileOperations.ensureFileExtension ".cab" installerPath
            let! existingCabFilePath = FileOperations.ensureFileExists cabFilepath            
            let! expandResult = expandCabFile (existingCabFilePath,destinationPath)
            return destinationPath
        }) with
        |Ok _ -> Result.Ok destinationPath
        |Error ex -> Result.Error (new Exception("Failed to extract Sccm package. " + ex.Message, ex))        
    
    let extractUpdate (rootDirectory:FileSystem.Path, (prefix,downloadedPackageInfo:DownloadedPackageInfo)) =
        result{
            let packageFolderName = getPackageFolderName downloadedPackageInfo.Package
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