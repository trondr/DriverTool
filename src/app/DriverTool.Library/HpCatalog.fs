namespace DriverTool

module HpCatalog =
    let soruceDriverPackCatalogCab = "https://ftp.hp.com/pub/caps-softpaq/cmit/HPClientDriverPackCatalog.cab"
    let platformListCab = "https://ftp.hp.com/pub/caps-softpaq/cmit/imagepal/ref/platformList.cab"
    let softPackSource = "https://ftp.hp.com/pub/softpaq/"
    let smsSdpCatalog = "https://ftp.hp.com/pub/softlib/software/sms_catalog/HpCatalogForSms.latest.cab"

    open System
    open DriverTool.Library.Cab
    open DriverTool.Library.F
    open DriverTool.Library
    open DriverTool.Library.Logging

    let expandCabFile (cabFilePath:FileSystem.Path, destinationFolderPath:FileSystem.Path, destinationFilePath:FileSystem.Path) =
        result{
            let! expandExePath = FileSystem.path expandExe
            let arguments = sprintf "\"%s\" -F:* \"%s\"" (FileSystem.pathValue cabFilePath) (FileSystem.pathValue destinationFilePath)
            let workingDirectory =  FileSystem.pathValue destinationFolderPath
            let! expandExitCode = ProcessOperations.startConsoleProcess (expandExePath, arguments, workingDirectory,-1,null,null,false)
            let! expandResult = expandExeExitCodeToResult cabFilePath expandExitCode
            return expandResult
        }

    let downloadDriverPackCatalog cachedFolderPath =
        result{            
            let! destinationCabFile = PathOperations.combine2Paths (FileSystem.pathValue cachedFolderPath,"HPClientDriverPackCatalog.cab")
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist true destinationCabFile
            let! downloadResult = Web.downloadFile reportProgressStdOut (new Uri(soruceDriverPackCatalogCab)) true nonExistingDestinationCabFile
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)
            let! destinationFilePath = PathOperations.combine2Paths (FileSystem.pathValue cachedFolderPath,"HPClientDriverPackCatalog.xml")
            let! expandResult = expandCabFile (existingDestinationCabFile, cachedFolderPath,destinationFilePath)
            let! existingDriverPackageCatalogXmlPath = FileOperations.ensureFileExists destinationFilePath            
            return existingDriverPackageCatalogXmlPath
        }
    
    let downloadSmsSdpCatalog cachedFolderPath =
        result{            
            let! hpCatalogDestinationFolderPath = PathOperations.combinePaths2 cachedFolderPath "HpCatalogForSms.latest"
            let! nonExistingHpCatalogDestinationFolderPath = DirectoryOperations.deleteDirectory true hpCatalogDestinationFolderPath
            let! existingHpCatalogDestinationFolderPath = DirectoryOperations.ensureDirectoryExistsAndIsEmpty (nonExistingHpCatalogDestinationFolderPath,true) 
            let! destinationCabFile = PathOperations.combinePaths2 cachedFolderPath "HpCatalogForSms.latest.cab"
            let! nonExistingDestinationCabFile = FileOperations.ensureFileDoesNotExist true destinationCabFile
            let! downloadResult = Web.downloadFile reportProgressStdOut (new Uri(smsSdpCatalog)) true nonExistingDestinationCabFile
            let! existingDestinationCabFile = FileOperations.ensureFileExists (destinationCabFile)            
            let! expandResult = DriverTool.SdpUpdates.expandSmsSdpCabFile (existingDestinationCabFile, existingHpCatalogDestinationFolderPath)                        
            return existingHpCatalogDestinationFolderPath
        }

    open System.Xml.Linq

    /// <summary>
    /// <ProductOSDriverPack>
    /// <ProductType>Workstations</ProductType>
    /// <SystemId>0AEC</SystemId>
    /// <SystemName>HP Z800 Workstation</SystemName>
    /// <OSName>Windows 7 64-bit</OSName>
    /// <SoftPaqId>sp58949</SoftPaqId>
    /// </ProductOSDriverPack>
    /// </summary>
    type ProductOSDriverPack = {ProductType:string;SystemId:string;SystemName:string;OSName:string;SoftPaqId:string}

    /// <summary>
    /// <SoftPaq>
    /// <Id>sp58949</Id>
    /// <Name>HP Z400, Z600, Z800 Windows 7 x64 Driver Pack</Name>
    /// <Version>A 1.01 3</Version>
    /// <Category>Manageability - Driver Pack</Category>
    /// <DateReleased>2012-10-08</DateReleased>
    /// <Url>https://ftp.hp.com/pub/softpaq/sp58501-59000/sp58949.exe</Url>
    /// <Size>526744088</Size>
    /// <MD5>58b62f84d0c5977e7a5969b72d621def</MD5>
    /// <CvaFileUrl>https://ftp.hp.com/pub/softpaq/sp58501-59000/sp58949.cva</CvaFileUrl>
    /// <ReleaseNotesUrl>https://ftp.hp.com/pub/softpaq/sp58501-59000/sp58949.html</ReleaseNotesUrl>
    /// <CvaTitle>HP Z400, Z600, Z800 Windows 7 x64 Driver Pack</CvaTitle>
    /// </SoftPaq>
    /// </summary>
    type SoftPaq ={Id:string;Name:string;Version:string;Category:string;DateReleased:string;Url:string;Size:Int64;MD5:string;CvaFileUrl:string;ReleaseNotesUrl:string;CvaTitle:string}

    let getElementValue (softPaqNode:XElement, elementName:string) =
        softPaqNode
            .Element(XName.Get(elementName))
            .Value

    let toSoftPaq (softPaqNode:XElement) =
        {
            Id=getElementValue (softPaqNode,"Id");
            Name=getElementValue (softPaqNode,"Name");
            Version=getElementValue (softPaqNode,"Version");
            Category=getElementValue (softPaqNode,"Category");
            DateReleased=getElementValue (softPaqNode,"DateReleased");
            Url=getElementValue (softPaqNode,"Url");
            Size=Int64.Parse( getElementValue (softPaqNode,"Size"));
            MD5=getElementValue (softPaqNode,"MD5");
            CvaFileUrl=getElementValue (softPaqNode,"CvaFileUrl");
            ReleaseNotesUrl=getElementValue (softPaqNode,"ReleaseNotesUrl");
            CvaTitle=getElementValue (softPaqNode,"CvaTitle");
        }

    let getSoftPaqs (driverPackCatalogXmlFilePath:FileSystem.Path) =
        result
            {
                let! existingDriverPackageCatalogXmlFilePath = FileOperations.ensureFileExists(driverPackCatalogXmlFilePath)
                let xDocument = XDocument.Load(FileSystem.pathValue existingDriverPackageCatalogXmlFilePath)
                let softPaqs =
                    xDocument.Descendants(XName.Get("SoftPaq"))
                    |>Seq.map (fun sn -> toSoftPaq sn)
                    |>Seq.toArray
                return softPaqs
            }

    let toProductOSDriverPack (productOSDriverPackNode:XElement) =
        {
            ProductType=getElementValue (productOSDriverPackNode,"ProductType");
            SystemId=getElementValue (productOSDriverPackNode,"SystemId");
            SystemName=getElementValue (productOSDriverPackNode,"SystemName");
            OSName=getElementValue (productOSDriverPackNode,"OSName");
            SoftPaqId=getElementValue (productOSDriverPackNode,"SoftPaqId");
        }
    
    let getProductOSDriverPacks (driverPackCatalogXmlFilePath:FileSystem.Path) =
        result
            {
                let! existingDriverPackageCatalogXmlFilePath = FileOperations.ensureFileExists(driverPackCatalogXmlFilePath)
                let xDocument = XDocument.Load(FileSystem.pathValue existingDriverPackageCatalogXmlFilePath)
                let productOSDriverPacks =
                    xDocument.Descendants(XName.Get("ProductOSDriverPack"))
                    |>Seq.map (fun pn -> toProductOSDriverPack pn)
                    |>Seq.toArray
                return productOSDriverPacks
            }
    
    open System.Text.RegularExpressions
    open PackageXml

    let isSupportedOs (osName:string) =
        osName.StartsWith("Windows 10 64-bit") || osName.StartsWith("Windows 10 32-bit")

    let operatingSystemCodeToHpOsName (operatingSystemCode:OperatingSystemCode) =
        match operatingSystemCode.Value with
        |"WIN10X64" -> "Windows 10 64-bit"
        |"WIN10X86" -> "Windows 10 32-bit"
        |_ -> raise (new Exception(sprintf "Failed to convert os short name '%s' to Hp os name. Only WIN10X64 and WIN10X86 are supported os shortnames." operatingSystemCode.Value))
    
    let hpOsNameToOsCodeAndOsBuild (hpOsName:string) =
        let osCode = 
            match hpOsName with
            |hpoOsN when hpOsName.StartsWith("Windows 10 64-bit") -> "WIN10X64"
            |hpoOsN when hpOsName.StartsWith("Windows 10 32-bit") -> "WIN10X86"
            |_ -> raise (new Exception(sprintf "Failed to convert HP os name '%s' to Hp os name to supported osshort names. Only WIN10X64 and WIN10X86 are supported os shortnames." hpOsName))
        let osBuild = OperatingSystem.getOsBuildFromName hpOsName
        (osCode,osBuild)

    let systemIdToModels (systemId:string) =
        systemId.Replace(" ","").Trim().Split([|','|])

    let isSupportedForModel (systemId:string, modelCode:ModelCode)=
        let models = systemIdToModels (systemId.ToLowerInvariant())
        let modelCodeLowerCase = modelCode.Value.ToLowerInvariant()
        let found = models |> Array.tryFind(fun m -> modelCodeLowerCase = m)
        match found with
        | None -> false
        |Some _ -> true
        
    let isSupportedForOperatingSystem(hpOsName,operatingSystemCode:OperatingSystemCode,osBuild:string) =        
        let (hpOsCode,hpOsBuild) = hpOsNameToOsCodeAndOsBuild hpOsName
        (hpOsCode = operatingSystemCode.Value) && (hpOsBuild = osBuild)        

    let toSccmPackageInfo (softPaq:SoftPaq) (hpOsName:string) : SccmPackageInfo =
        
        let (osCode,osBuild)= hpOsNameToOsCodeAndOsBuild hpOsName
        {
            ReadmeFile =
                {
                    Url="";
                    Checksum="";
                    FileName="";
                    Size=0L;
                }
            InstallerFile =
                {
                    Url=softPaq.Url;
                    Checksum=softPaq.MD5;
                    FileName=Web.getFileNameFromUrl softPaq.Url;
                    Size=softPaq.Size;
                }
            Released=softPaq.DateReleased|>DateTime.Parse;
            Os=osCode;
            OsBuild=osBuild;
        }

    let getSccmDriverPackageInfoBase (driverPackCatalogXmlFilePath:FileSystem.Path, modelCode: ModelCode, operatingSystemCode:OperatingSystemCode, osBuild:string) =
        result{
            let! existingDriverPackageCatalogXmlFilePath = FileOperations.ensureFileExists(driverPackCatalogXmlFilePath)
            let! softPaqs = getSoftPaqs existingDriverPackageCatalogXmlFilePath
            let! productOSDriverPacks = getProductOSDriverPacks existingDriverPackageCatalogXmlFilePath            
            let sccmDriverPackage =
                productOSDriverPacks
                |>Array.filter(fun osdp -> isSupportedForModel (osdp.SystemId, modelCode) )
                |>Array.tryFind(fun osdp -> isSupportedForOperatingSystem (osdp.OSName,operatingSystemCode,osBuild))                
            let! sccmPackageInfo =
                match sccmDriverPackage with
                |Some dp -> 
                    let sccmpi =
                        softPaqs
                        |>Array.tryFind(fun sp -> sp.Id = dp.SoftPaqId)
                    match sccmpi with
                    |Some i -> Result.Ok (toSccmPackageInfo i dp.OSName)
                    |None -> Result.Error (new Exception(sprintf "Failed to find HP sccm driver package. Found os driver product but failed to find softpaq for model '%s' and operating system '%s' and os build '%s'" modelCode.Value operatingSystemCode.Value osBuild))
                |None -> Result.Error (new Exception(sprintf "Failed to find HP sccm driver package for model '%s' and operating system '%s' and os build '%s' " modelCode.Value operatingSystemCode.Value osBuild))
            return sccmPackageInfo
        }

    let loadCatalog (catalogPath:FileSystem.Path) : Result<SoftPaq[]*ProductOSDriverPack[],Exception> =
        result{
            let! existingDriverPackageCatalogXmlFilePath = FileOperations.ensureFileExists catalogPath
            let! softPaqs = getSoftPaqs existingDriverPackageCatalogXmlFilePath
            let! productOSDriverPacks = getProductOSDriverPacks existingDriverPackageCatalogXmlFilePath            
            return (softPaqs,productOSDriverPacks)
        }
    
    
    