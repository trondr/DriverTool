namespace DriverTool

open FSharp.Data

module LenovoCatalog =
    let logger = Logging.getLoggerByName("LenovoCatalog")
    open FSharp.Data
    open System

    type CatalogXmlProvider = XmlProvider<"https://download.lenovo.com/cdrt/td/catalog.xml">

    let getCacheDirectory =
        DriverTool.Configuration.getDownloadCacheDirectoryPath

    let getLocalLenvoCatalogXmlFilePath =
        Path.create (System.IO.Path.Combine(getCacheDirectory,"LenovoCatalog.xml"))

    let downloadCatalog =
        result {
            let! destinationFile = getLocalLenvoCatalogXmlFilePath
            let! downloadResult = Web.downloadFile (new Uri("https://download.lenovo.com/cdrt/td/catalog.xml"), true, destinationFile)
            return downloadResult        
        }
    
    type Product = {Model:Option<string>;Os:string;OsBuild:Option<string>;Name:string;SccmDriverPackUrl:Option<string>}

    let getSccmPackageInfos =
        result{
            let! catalogXmlPath = downloadCatalog
            let productsXml = CatalogXmlProvider.Load(catalogXmlPath.Value)
            return productsXml.Products
            |>  Seq.map (fun p-> 
                        let driverPack = (p.DriverPacks|> Seq.tryFind (fun dp-> dp.Id = "sccm"))
                        {
                            Model=p.Model.String;
                            Os=p.Os;
                            OsBuild=p.Build.String;
                            Name=p.Name;
                            SccmDriverPackUrl= 
                                match driverPack with
                                |Some v -> Some v.Value
                                |None -> None
                        }
                    )

        }
    
    type SccmPackageDownloadInfo = {
        ReadmeUrl: string;
        ReadmeChecksum: string;
        InstallerUrl:string;
        InstallerChecksum:string
    }

    open F    
               
    let getLenovoSccmPackageDownloadInfo (uri:string) =
        let content = DriverTool.WebParsing.getContentFromWebPage uri
        match content with
        |Ok v -> 
            let (exeUrl, exeChecksum) = 
                match v with
                |Regex @"((https[s]?):\/\/[^\s]+\.exe).+?<p>SHA-256:(.+?)</p>" [file;na;sha256] -> (file,sha256)            
                |_ -> ("","")
            let (txtUrl,txtChecksum) =
                match v with
                |Regex @"((https[s]?):\/\/[^\s]+\.txt).+?<p>SHA-256:(.+?)</p>" [file;na;sha256] -> (file,sha256)
                |_ -> ("","")
            let sccmPackage = {ReadmeUrl = txtUrl; ReadmeChecksum = txtChecksum; InstallerUrl= exeUrl; InstallerChecksum=exeChecksum}
            Result.Ok sccmPackage
        |Error ex -> Result.Error ex
    
    let osShortNameToLenovoOs osShortName =
        match osShortName with
        | "WIN10X86" -> "win10"
        | "WIN10X64" -> "win10"
        | "WIN7X86" -> "win732"
        | "WIN7X64" -> "win764"
        | "WIN81X86" -> "win81"
        | "WIN81X64" -> "win81"
        | _ -> raise (new System.Exception("Unsupported OS: " + osShortName))
    
    let findSccmPackageInfoByNameAndOsAndBuild name os build products =
        let sccmPackageInfos = 
            products
            |> Seq.filter (fun p -> p.Name = name && p.Os = os && (p.OsBuild.Value = build))
            |> Seq.toArray
        match sccmPackageInfos.Length > 0 with
        | true -> 
            sccmPackageInfos |> Seq.head
        | false -> 
            products
            |> Seq.filter (fun p -> p.Name = name && p.Os = os && (p.OsBuild.Value = "*"))
            |> Seq.head
    
    
    type ModelInfo = { Name:string; Os:string ; OsBuild: string}
    
    let getOsBuildBase osVersion =
        match osVersion with
        | "10.0.14393" -> "1607"
        | "10.0.15063" -> "1703"
        | "10.0.16299" -> "1709"
        | "10.0.17134" -> "1803"
        | "10.0.17763" -> "1809"
        | "10.0.18290" -> "1903"
        | _ -> 
            logger.WarnFormat("Unsupported OS Build for Windows version: {0}. Returning OsBuild=\"*\".", osVersion)
            "*"
        
    open DriverTool.Util.FSharp
    
    let getOsBuild = 
        let osVersion = 
            match (WmiHelper.getWmiProperty "Win32_OperatingSystem" "Version") with
            |Ok osv -> osv
            |Error ex -> raise (new System.Exception("Failed to get OS Build for current system due to: " + ex.Message))        
        getOsBuildBase osVersion
    
    let getModelName = 
        match (WmiHelper.getWmiProperty "Win32_ComputerSystemProduct" "Version") with
        |Ok n -> n
        |Error ex -> raise (new System.Exception("Failed to model name for current system due to: " + ex.Message))        

    let getModelInfo =
        let name = getModelName
        let os = osShortNameToLenovoOs (OperatingSystem.getOsShortName)
        let osBuild = getOsBuild
        {
            Name = name;
            Os = os;
            OsBuild = osBuild
        }
