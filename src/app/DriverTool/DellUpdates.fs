namespace DriverTool

module DellUpdates =
    
    let softwareCatalogCab = "http://downloads.dell.com/catalog/CatalogPC.cab"
    let driverPackageCatalogCab = "http://downloads.dell.com/catalog/DriverPackCatalog.cab"
    let downloadsBaseUrl = "http://downloads.dell.com"



    open FSharp.Data
    open System

    let getCacheDirectory =
        DriverTool.Configuration.getDownloadCacheDirectoryPath

    let getLocalDellSoftwareCatalogCabFilePath =
        Path.create (System.IO.Path.Combine(getCacheDirectory,"CatalogPC.cab"))

    let getLocalDellSoftwareCatalogXmlFilePath =
        Path.create (System.IO.Path.Combine(getCacheDirectory,"CatalogPC.xml"))
    
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
            let! destinationFolderPath = Path.create getCacheDirectory
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
            System.Console.WriteLine(softwareComponentsXDocument.Root.Name.LocalName)
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