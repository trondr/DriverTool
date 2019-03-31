namespace DriverTool

module DellUpdates =
    type DellUpdates = class end
    let loggerd = Logging.getLoggerByName(typeof<DellUpdates>.Name)

    open F
    open FSharp.Data
    open System
    open DriverTool.Configuration
    open DellSettings
    open DriverTool.DellUpdates2

    let getLocalDellSoftwareCatalogCabFilePath =
        FileSystem.path (System.IO.Path.Combine(downloadCacheDirectoryPath,"CatalogPC.cab"))

    let getLocalDellSoftwareCatalogXmlFilePath =
        FileSystem.path (System.IO.Path.Combine(downloadCacheDirectoryPath,"CatalogPC.xml"))
    

    let downloadSoftwareComponentsCatalog () =
        result {
            let! destinationCabFile = getLocalDellSoftwareCatalogCabFilePath
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist true destinationCabFile
            let! downloadResult = Web.downloadFile (new Uri(softwareCatalogCab), true, nonExistingDestinationCabFile)
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)
            let! destinationFolderPath = FileSystem.path downloadCacheDirectoryPath
            let! expandResult = DriverTool.DellCatalog.expandCabFile (existingDestinationCabFile, destinationFolderPath)
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
    open DriverTool.UpdatesContext

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

    let toPackageInfo (sc: XElement,logDirectory) =
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
            InstallCommandLine = (sprintf "\"%s\" /s /l=\"%s\\DUP_%s.log\"" installerName (FileSystem.pathValue logDirectory) installerName)
            Category = getElementValue (sc,"Category")
            ReadmeName = "";
            ReadmeCrc = "";
            ReadmeSize=0L;
            ReleaseDate= (getAttribute (sc,"dateTime"))|>toDateString
            PackageXmlName="";
        }

    let getRemoteUpdates (context:UpdatesRetrievalContext) =
        result{
            let! softwareCatalogXmlFile = downloadSoftwareComponentsCatalog()            
            let! dellOsCode = operatingSystemCodeToDellOsCode context.OperatingSystem            
            let softwareComponentsXDocument = XDocument.Load(FileSystem.pathValue softwareCatalogXmlFile)
            let manifestRoot = softwareComponentsXDocument.Root            
            let softwareComponents = manifestRoot.Descendants(XName.Get("SoftwareComponent"))
            let updates =
                softwareComponents
                |>Seq.filter(fun sc -> isSupportedForModel (sc,context.Model))                
                |>Seq.filter(fun sc -> isSupportedForOs (sc,dellOsCode))
                |>Seq.map(fun sc -> toPackageInfo (sc,context.LogDirectory))
                |>getLatestPackageInfoVersion
                |>Seq.toArray
            loggerd.Info(sprintf "Updates: %i" updates.Length)
            return updates
        }

    let getLocalUpdates (context:UpdatesRetrievalContext) =
        result
            {
                let! remoteUpdates = getRemoteUpdates context
                let! localUpdates = DellCommandUpdate.getLocalUpdates (context.Model, context.OperatingSystem, remoteUpdates)
                return localUpdates
            }
    
    open DriverTool.Web

    open DriverTool.Web

    


        