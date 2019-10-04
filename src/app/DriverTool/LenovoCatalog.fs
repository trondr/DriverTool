namespace DriverTool

module LenovoCatalog =
    open FSharp.Data    
    open System
    open DriverTool 
    open DriverTool.PackageXml
    open LenovoCatalogXml    
    open DriverTool.Web
    open DriverTool.OperatingSystem

    let logger = Logging.getLoggerByName("LenovoCatalog")

    let getCacheDirectory =
        DriverTool.Configuration.downloadCacheDirectoryPath

    let getLocalLenvoCatalogXmlFilePath cacheFolderPath =
        FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue cacheFolderPath ,"LenovoCatalog.xml"))

    let downloadCatalog cacheFolderPath =
        result {
            let! destinationFile = getLocalLenvoCatalogXmlFilePath cacheFolderPath
            let! downloadResult = Web.downloadFile (new Uri("https://download.lenovo.com/cdrt/td/catalog.xml"), true, destinationFile)
            return downloadResult
        }
    
    type Product = {Model:Option<string>;Os:string;OsBuild:Option<string>;Name:string;SccmDriverPackUrl:Option<string>;ModelCodes:array<string>}

    let getSccmPackageInfosFromLenovoCatalogProducts (lenovoCatalogProducts:seq<LenovoCatalogProduct>) =
        let products =
            lenovoCatalogProducts
            |>Seq.map(fun p -> 
                    
                        let driverPack = (p.DriverPacks|> Seq.tryFind (fun dp-> dp.Id = "sccm"))                                
                        {
                            Model=Some p.Model;
                            Os=p.Os;
                            OsBuild=Some p.Build;
                            Name=p.Name;
                            SccmDriverPackUrl= 
                                (match driverPack with
                                |Some v -> Some v.Url
                                |None -> None
                                );
                            ModelCodes = (p.Queries.ModelTypes 
                                            |> Seq.map (fun m-> 
                                                        match m with
                                                        |ModelType modelType -> modelType
                                                ) 
                                            |> Seq.toArray
                                         )
                        }                            
                )
            |>Seq.toArray
        products

    let getSccmPackageInfos cacheFolderPath =
        result
            {
                let! catalogXmlPath = downloadCatalog cacheFolderPath
                let! lenovoCatalogProducts = DriverTool.LenovoCatalogXml.loadLenovoCatalog catalogXmlPath
                let products =
                    getSccmPackageInfosFromLenovoCatalogProducts lenovoCatalogProducts                    
                return products
            }

    let getReleaseDateFromUrlBase (url:string)  =
        let fileName = (getFileNameFromUrl url)
        match fileName with
        |Regex @"(\d{4})(\d{2})(\d{2})\..+?$" [year;month;day] -> (new DateTime(year |> int, month|>int, day|>int))        
        |Regex @"(\d{4})(\d{2})\..+?$" [year;month] -> (new DateTime(year |> int, month|>int, 1))
        |_ -> 
            (new DateTime(1970,01,01))

    let getReleaseDateFromUrl (url:string) =
        match (tryCatchWithMessage getReleaseDateFromUrlBase url (sprintf "Failed to get release date from url '%s'." url)) with
        |Ok r -> r
        |Error ex -> raise ex
    
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

    let urlToDownloadType (url:string) =
        match url with
        | Regex @".txt$" [] ->  DownloadType.Readme
        | Regex @".exe$" [] ->  DownloadType.Installer
        | Regex @".msi$" [] ->  DownloadType.Installer
        | _ -> DownloadType.Unknown

    let osNameToOsShortName osName =
        match osName with
        | "Windows 10 (64-bit)" -> "WIN10X64"
        | "Windows 10 (32-bit)" -> "WIN10X86"
        | "Windows 8.1 (64-bit)" -> "WIN81X64"
        | "Windows 8.1 (32-bit)" -> "WIN81X86"
        | "Windows 7 (64-bit)" -> "WIN7X64"
        | "Windows 7 (32-bit)" -> "WIN7X86"
        | _ -> (raise (new Exception("Unknown OS name: " + osName) ))

    let getDownloadLinkInfo (ulNode:HtmlNode) =
        let liElements = ulNode.Elements() |> Seq.toArray
        let subLiElements = liElements.[0].Elements()        
        let name = subLiElements.[0].InnerText()        
        let osName = getOs liElements.[1]
        let url = getDownloadLink ulNode
        let checksum = getCheckSum ulNode
        let osBuild = getOsBuildFromName2 name
        let downloadLinkInfo =
            {
                DriverName = name
                Type = urlToDownloadType url
                Url = url
                Checksum = checksum
                FileName= getFileNameFromUrl url
                Os = osNameToOsShortName osName
                OsBuild = osBuild
            }
        downloadLinkInfo
    
    let osShortNameToLenovoOs osShortName =
        match osShortName with
        | "WIN10X86" -> "win10"
        | "WIN10X64" -> "win10"
        | "WIN7X86" -> "win732"
        | "WIN7X64" -> "win764"
        | "WIN81X86" -> "win81"
        | "WIN81X64" -> "win81"
        | "win10" -> "win10"
        | "win81" -> "win81"
        | "win732" -> "win732"
        | "win764" -> "win764"
        | _ -> raise (new System.Exception("Unsupported OS: " + osShortName))
    
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


    let getDownloadsTabHtmlNode (htmlDocument:HtmlDocument) = 
        let downloadsTab =
            htmlDocument.Descendants["div"]
                //Find the downloadsTab
                |>Seq.filter (fun d -> 
                                        match (d.TryGetAttribute("id")) with
                                        |Some id -> (id.Value() = "downloadsTab")
                                        |None -> false
                             )
                |>Seq.tryHead
        match downloadsTab with
        |None -> Result.Error (toException (sprintf "Download tabs ('downloadTabs') not found in html document.") None)
        |Some dt -> Result.Ok dt
        
    let loadHtmlDocument content =
        try
            use htmlContentStream = stringToStream content
            let htmlDocument = HtmlDocument.Load(htmlContentStream)
            Result.Ok htmlDocument
        with
        |ex -> Result.Error (toException (sprintf "Failed to load html document content.") (Some ex))

    let getUnorderdLists (downloadsTabHtmlNode:HtmlNode) =
        let list =
            downloadsTabHtmlNode.Descendants["ul"]
            //Find all unordered list containing items of type "EXE" or "TXT README" 
            |>Seq.filter (fun ul-> 
                                (ul.Elements() |> Seq.exists (fun li -> li.InnerText().Contains(".exe") || li.InnerText().Contains(".txt") || li.InnerText().Contains("TXT README") || li.InnerText().Contains("README")))
                            )
        if (Seq.isEmpty list) then 
            Result.Error (toException (sprintf "No download links were found beneath the downloads tab in the html document.") None)
        else
            Result.Ok list

    let getDownloadLinksFromWebPageContent content =
        result{
            let! htmlDocument = loadHtmlDocument content
            let! downloadsTabHtmlNode = getDownloadsTabHtmlNode htmlDocument
            let! unorderedLists = getUnorderdLists downloadsTabHtmlNode
            let sccmPackageInfos = getSccmPackageInfoFromUlNodes unorderedLists
            return sccmPackageInfos
        }
    
    type ModelInfo = { Name:string; Os:string ; OsBuild: string}
    
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

    let findSccmPackageInfoByModelCode4AndOsAndBuild (logger:Common.Logging.ILog) modelCode4 os osBuild products =
        logger.Info(sprintf "Finding sccm package info for model '%s', os '%s', osbuild '%s'..." modelCode4 os osBuild )
        let matchedProducts = 
            products
            |> Seq.filter (fun p -> 
                                let foundModelCodes = 
                                    p.ModelCodes
                                    |>Array.filter (fun m-> 
                                            (m = modelCode4)
                                        )
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
                        
                  
       