namespace DriverTool

module DellUpdates =
    type DellUpdates = class end
    let logger = Logging.getLoggerByName(typeof<DellUpdates>.Name)

    let softwareCatalogCab = "http://downloads.dell.com/catalog/CatalogPC.cab"
    let driverPackageCatalogCab = "http://downloads.dell.com/catalog/DriverPackCatalog.cab"
    let downloadsBaseUrl = "http://downloads.dell.com"



    open FSharp.Data
    open System
    open DriverTool.Configuration

    let getLocalDellSoftwareCatalogCabFilePath =
        Path.create (System.IO.Path.Combine(getDownloadCacheDirectoryPath,"CatalogPC.cab"))

    let getLocalDellSoftwareCatalogXmlFilePath =
        Path.create (System.IO.Path.Combine(getDownloadCacheDirectoryPath,"CatalogPC.xml"))
    
    let expandExe =
        System.IO.Path.Combine(DriverTool.Environment.nativeSystemFolder,"expand.exe")

    let expandCabFile (cabFilePath:Path, destinationFolderPath:Path) =
        result{
            let! expandResult = ProcessOperations.startConsoleProcess (expandExe,String.Format("\"{0}\" -F:* -R \"{1}\"",cabFilePath.Value,destinationFolderPath.Value), destinationFolderPath.Value,-1,null,null,false)            
            return expandResult
        }

    let downloadSoftwareComponentsCatalog () =
        result {
            let! destinationCabFile = getLocalDellSoftwareCatalogCabFilePath
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist (true, destinationCabFile)
            let! downloadResult = Web.downloadFile (new Uri(softwareCatalogCab), true, nonExistingDestinationCabFile)
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)
            let! destinationFolderPath = Path.create getDownloadCacheDirectoryPath
            let! expandResult = expandCabFile (existingDestinationCabFile, destinationFolderPath)
            let! softwareCatalogXmlPath = getLocalDellSoftwareCatalogXmlFilePath
            let! existingSoftwareCatalogXmlPath = FileOperations.ensureFileExists softwareCatalogXmlPath            
            return existingSoftwareCatalogXmlPath
        }

    let optionToBoolean option = 
        match option with
        |None -> false
        |Some _ -> true

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
        |_ -> raise (new Exception(String.Format("Failed to convert os short name '{0}' to Dell oscode. Only WIN10X64 and WIN10X86 are supported os shortnames.",operatingSystemCode.Value)))
    
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

    let toPackageInfo (sc: XElement) =
        let path = getAttribute(sc,"path")
        let dellVersion = getAttribute(sc,"dellVersion")
        let vendorVersion = getAttribute (sc, "vendorVersion")

        let (directory, installerName) = pathToDirectoryAndFile path            
        {
            Name = toName (getElementValue (sc,"Name"),vendorVersion,dellVersion)
            Title = getElementValue (sc,"Name")
            Version = getAttribute (sc, "vendorVersion")
            BaseUrl = downloadsBaseUrl + "/" + directory;
            InstallerName = installerName
            InstallerCrc = getAttribute (sc,"hashMD5")
            InstallerSize = int64 (getAttribute (sc,"size"))
            ExtractCommandLine = ""
            InstallCommandLine = installerName + "/s"
            Category = getElementValue (sc,"Category")
            ReadmeName = "";
            ReadmeCrc = "";
            ReadmeSize=0L;
            ReleaseDate= (getAttribute (sc,"dateTime"))|>toDateString
            PackageXmlName="";
        }

    let getRemoteUpdates (modelCode:ModelCode, operatingSystemCode:OperatingSystemCode,overwrite:bool) =
        result{
            let! softwareCatalogXmlFile = downloadSoftwareComponentsCatalog()            
            let! dellOsCode = operatingSystemCodeToDellOsCode operatingSystemCode            
            let softwareComponentsXDocument = XDocument.Load(softwareCatalogXmlFile.Value)
            let manifestRoot = softwareComponentsXDocument.Root            
            let softwareComponents = manifestRoot.Descendants(XName.Get("SoftwareComponent"))
            let updates =
                softwareComponents
                |>Seq.filter(fun sc -> isSupportedForModel (sc,modelCode))                
                |>Seq.filter(fun sc -> isSupportedForOs (sc,dellOsCode))
                |>Seq.map(fun sc -> toPackageInfo sc)
                |>getLatestPackageInfoVersion
                |>Seq.toArray
            System.Console.WriteLine("Updates: " + updates.Length.ToString())            
            return updates
        }

    let getLocalUpdates (modelCode:ModelCode, operatingSystemCode:OperatingSystemCode,overwrite:bool) : Result<PackageInfo[],Exception> =
        Result.Error (new Exception("Not implemented"))

    let getLocalDriverPackageCatalogCabFilePath =
        Path.create (System.IO.Path.Combine(getDownloadCacheDirectoryPath,"DriverPackCatalog.cab"))

    let getLocalDriverPackageXmlFilePath =
        Path.create (System.IO.Path.Combine(getDownloadCacheDirectoryPath,"DriverPackCatalog.xml"))

    let downloadDriverPackageCatalog () =
        result {
            let! destinationCabFile = getLocalDriverPackageCatalogCabFilePath
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist (true, destinationCabFile)
            let! downloadResult = Web.downloadFile (new Uri(driverPackageCatalogCab), true, nonExistingDestinationCabFile)
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)
            let! destinationFolderPath = Path.create getDownloadCacheDirectoryPath
            let! expandResult = expandCabFile (existingDestinationCabFile, destinationFolderPath)
            let! driverPackageCatalogXmlPath = getLocalDriverPackageXmlFilePath
            let! existingDriverPackageCatalogXmlPath = FileOperations.ensureFileExists driverPackageCatalogXmlPath            
            return existingDriverPackageCatalogXmlPath
        }
    
    let toSccmPackageInfo (dp:XElement) (operatingSystemCode:OperatingSystemCode) : SccmPackageInfo =
        let path = getAttribute(dp,"path")        
        let (directory, installerName) = pathToDirectoryAndFile path
        {
            ReadmeUrl="";
            ReadmeChecksum="";
            ReadmeFileName="";
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
        |_ -> raise (new Exception(String.Format("Failed to convert os short name '{0}' to Dell oscode. Only WIN10X64 and WIN10X86 are supported os shortnames.",operatingSystemCode.Value)))

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
            let xDocument = XDocument.Load(driverPackageCatalogXmlPath.Value)            
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

    let extractSccmPackage (downloadedSccmPackage:DownloadedSccmPackageInfo, destinationPath:Path) =
        logger.Info("Extract Sccm Driver Package CAB...")
        match(result{
            let! installerPath = Path.create downloadedSccmPackage.InstallerPath
            let! cabFilepath = FileOperations.ensureFileExtension (installerPath, ".cab")
            let! existingCabFilePath = FileOperations.ensureFileExists cabFilepath            
            let! expandResult = expandCabFile (existingCabFilePath,destinationPath)
            return destinationPath
        }) with
        |Ok _ -> Result.Ok destinationPath
        |Error ex -> Result.Error (new Exception("Failed to extract Sccm package. " + ex.Message, ex))        