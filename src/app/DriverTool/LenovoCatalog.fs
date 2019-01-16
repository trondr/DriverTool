namespace DriverTool

open FSharp.Data
open DriverTool.PackageXml

module LenovoCatalog =
    let logger = Logging.getLoggerByName("LenovoCatalog")
    
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
    
    type Product = {Model:Option<string>;Os:string;OsBuild:Option<string>;Name:string;SccmDriverPackUrl:Option<string>;ModelCodes:array<string>}

    let getSccmPackageInfos =
        result{
            let! catalogXmlPath = downloadCatalog
            let productsXml = CatalogXmlProvider.Load(catalogXmlPath.Value)
            let products =  
                productsXml.Products
                |>  Seq.map (fun p-> 
                            let driverPack = (p.DriverPacks|> Seq.tryFind (fun dp-> dp.Id = "sccm"))
                            {
                                Model=p.Model.String;
                                Os=p.Os;
                                OsBuild=p.Build.String;
                                Name=p.Name;
                                SccmDriverPackUrl= 
                                    (match driverPack with
                                    |Some v -> Some v.Value
                                    |None -> None);
                                ModelCodes = p.Queries.Types |> Seq.map (fun m-> m.String.Value) |> Seq.toArray
                            }
                        )
                |> Seq.toArray
            return products
        }
    
    open F    
    open System
    open Web          
    

    let getReleaseDateFromUrlBase (url:string)  =
        let fileName = (getFileNameFromUrl url)
        match fileName with
        |Regex @"(\d{4})(\d{2})(\d{2})\..+?$" [year;month;day] -> (new DateTime(year |> int, month|>int, day|>int))        
        |Regex @"(\d{4})(\d{2})\..+?$" [year;month] -> (new DateTime(year |> int, month|>int, 1))
        |_ -> 
            (new DateTime(1970,01,01))

    let getReleaseDateFromUrl (url:string) =
        match (tryCatchWithMessage getReleaseDateFromUrlBase url (String.Format("Failed to get release date from url '{0}'.",url))) with
        |Ok r -> r
        |Error ex -> raise ex
    
    open FSharp.Data

    let stringToStream (text:string) =
        let stream = new System.IO.MemoryStream()
        let sw = new System.IO.StreamWriter(stream)
        sw.Write(text)
        sw.Flush()
        stream.Position <- 0L
        stream
    
    type DownloadType = |Unknown = 0|Readme = 1|Installer = 2

    type DownloadLinkInfo = {
        DriverName: string
        Type: DownloadType
        Url:string
        Checksum: string
        FileName:string
        Os: string
        OsBuild: string
    }

    let driverTypeToDownloadType (driverType:string) =
        match driverType.Trim() with
        |"TXT README" -> DownloadType.Readme
        |"EXE" -> DownloadType.Installer
        |_ -> DownloadType.Unknown

    let getDownloadLink (liNode:HtmlNode) =
        let link = 
            liNode.Descendants["a"]
            |>Seq.filter (fun a -> 
                            match (a.TryGetAttribute("href")) with
                            |Some link -> 
                                link.Value().ToLower().EndsWith(".txt") || link.Value().EndsWith(".exe")
                            |None -> false                        
                         )
            |>Seq.map (fun a -> 
                            match (a.TryGetAttribute("href")) with
                            |Some link -> (link.Value())
                            |None -> ""                        
                        )
            |>Seq.toArray
            |>Seq.head
        link
    
    let getCheckSum (liNode:HtmlNode) =
        let checkSum =
            liNode.Descendants["p"]
            |>Seq.filter (fun p -> 
                            p.InnerText().StartsWith("SHA-256:")                            
                        )
            |>Seq.map (fun p -> p.InnerText().Replace("SHA-256:","").Trim())
            |>Seq.head
        checkSum

    let getOs (liNode:HtmlNode) =
        let os =
            liNode.Descendants["span"]
            |> Seq.map (fun s -> s.InnerText().Trim())
            |> Seq.head
        os

    let osNameToOsShortName osName =
        match osName with
        | "Windows 10 (64-bit)" -> "WIN10X64"
        | "Windows 10 (32-bit)" -> "WIN10X86"
        | "Windows 8.1 (64-bit)" -> "WIN81X64"
        | "Windows 8.1 (32-bit)" -> "WIN81X86"
        | "Windows 7 (64-bit)" -> "WIN7X64"
        | "Windows 7 (32-bit)" -> "WIN7X86"
        | _ -> (raise (new Exception("Unknown OS name: " + osName) ))

    open DriverTool.OperatingSystem

    let getDownloadLinkInfo (ulNode:HtmlNode) =
        let liElements = ulNode.Elements() |> Seq.toArray
        let subLiElements = liElements.[0].Elements()        
        let name = subLiElements.[0].InnerText()
        let dtype = subLiElements.[1].InnerText()
        let osName = getOs liElements.[1]
        let url = getDownloadLink ulNode
        let checksum = getCheckSum ulNode
        let osBuild = getOsBuildFromName name
        {
            DriverName = name
            Type = driverTypeToDownloadType dtype
            Url = url
            Checksum = checksum
            FileName= getFileNameFromUrl url
            Os = osNameToOsShortName osName
            OsBuild = osBuild
        }
    
    let osShortNameToLenovoOs osShortName =
        match osShortName with
        | "WIN10X86" -> "win10"
        | "WIN10X64" -> "win10"
        | "WIN7X86" -> "win732"
        | "WIN7X64" -> "win764"
        | "WIN81X86" -> "win81"
        | "WIN81X64" -> "win81"
        | _ -> raise (new System.Exception("Unsupported OS: " + osShortName))
    
    open DriverTool.Web

    let getSccmPackageInfoFromUlNodes ulNodes =
        let infos = 
            ulNodes
            |> Seq.map (fun ul -> getDownloadLinkInfo ul)
            |> Seq.groupBy (fun d -> d.Os, d.OsBuild)
            |> Seq.map (fun ((os,osBuild),links) -> 
                            let readme = links|> Seq.filter (fun d -> d.Type = DownloadType.Readme) |> Seq.head
                            let installer = links|> Seq.filter (fun d -> d.Type = DownloadType.Installer) |> Seq.head
                            let sccmPackage = 
                                {
                                    ReadmeFile =                                         
                                        {
                                        Url = readme.Url;
                                        Checksum = readme.Checksum;
                                        FileName = readme.FileName;
                                        Size=0L;
                                        }
                                    InstallerUrl= installer.Url;
                                    InstallerChecksum=installer.Checksum;
                                    InstallerFileName = installer.FileName;
                                    Released=(getReleaseDateFromUrl installer.Url);
                                    Os= (osShortNameToLenovoOs installer.Os);
                                    OsBuild=installer.OsBuild
                                }
                            sccmPackage
                        )
        infos

    let getDownloadLinksFromWebPageContent (content:string)  = 
        use htmlContentStream = stringToStream content
        let htmlDocument = HtmlDocument.Load(htmlContentStream)
        let downloadLinks =
                htmlDocument.Descendants["div"]
                //Find the downloadsTab
                |>Seq.filter (fun d -> 
                                        match (d.TryGetAttribute("id")) with
                                        |Some id -> (id.Value() = "downloadsTab")
                                        |None -> false
                             )
                //Get all unordered lists under the downloadsTab
                |> Seq.map (fun c -> 
                                c.Descendants["ul"]
                                //Find all unordered list containing items of type "EXE" or "TXT README" 
                                |>Seq.filter (fun ul-> 
                                                    (ul.Elements() |> Seq.exists (fun li -> li.InnerText().Contains("EXE") || li.InnerText().Contains("TXT README")))
                                                )
                                |> getSccmPackageInfoFromUlNodes                                                      
                           )        
                |> Seq.concat
        downloadLinks
    
    type ModelInfo = { Name:string; Os:string ; OsBuild: string}
    
    open DriverTool
    
    let getModelName = 
        match (WmiHelper.getWmiPropertyDefault "Win32_ComputerSystemProduct" "Version") with
        |Ok n -> n
        |Error ex -> raise (new System.Exception("Failed to model name for current system due to: " + ex.Message))        

    let getModelInfo =
        let name = getModelName
        let os = osShortNameToLenovoOs (OperatingSystem.getOsShortName)
        let osBuild = getOsBuildForCurrentSystem
        {
            Name = name;
            Os = os;
            OsBuild = osBuild
        }

    let getHighestOsBuildProduct (products:seq<Product>) =
        let maxOsbUildProduct =
            products
            |> Seq.maxBy (fun p -> p.OsBuild)
        maxOsbUildProduct

    let findSccmPackageInfoByModelCode4AndOsAndBuild modelCode4 os osBuild products =
        let matchedProducts = 
            products
            |> Seq.filter (fun p -> 
                                let foundModelCodes = 
                                    p.ModelCodes
                                    |>Array.filter (fun m-> (m = modelCode4))
                                foundModelCodes.Length > 0
                           )
            |> Seq.filter (fun p -> (p.Os = os))
            |> Seq.toArray
        match matchedProducts.Length > 0 with
        |true -> 
            let matchedOs = matchedProducts |> Seq.filter (fun p -> (p.OsBuild.Value = osBuild)) |> Seq.toArray
            match (matchedOs.Length > 0) with
            | true -> Some matchedProducts.[0]
            | false ->    
                match osBuild with
                | "*" -> 
                    let matchedOsBuild = matchedProducts |> Seq.filter (fun p -> (p.Os = os) && (p.OsBuild.Value = osBuild))|>Seq.toArray
                    match matchedOsBuild.Length > 0 with
                    |true -> Some matchedOsBuild.[0]
                    |false -> Some (getHighestOsBuildProduct matchedProducts)
                |_ -> Some (getHighestOsBuildProduct matchedProducts)
        |false -> None
                        
                  
       