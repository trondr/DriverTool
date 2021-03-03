namespace DriverTool

module LenovoCatalog =
    open FSharp.Data    
    open System
    open DriverTool 
    open DriverTool.Library.PackageXml
    open LenovoCatalogXml    
    open DriverTool.Library.Web
    open DriverTool.Library.OperatingSystem
    open DriverTool.Library.F0
    open DriverTool.Library.F
    open DriverTool.Library

    let logger = DriverTool.Library.Logging.getLoggerByName "LenovoCatalog"

    let getLocalLenvoCatalogXmlFilePath cacheFolderPath =
        FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue cacheFolderPath ,"LenovoCatalog.xml"))

    let downloadCatalog cacheFolderPath =
        result {
            let! destinationFile = getLocalLenvoCatalogXmlFilePath cacheFolderPath
            let! downloadResult = Web.downloadFile (new Uri("https://download.lenovo.com/cdrt/td/catalog.xml"), true, destinationFile)
            return downloadResult
        }    
    
    type Product = {Model:Option<string>;Os:string;OsBuild:Option<string>;Name:string;SccmDriverPackUrl:Option<string>;ModelCodes:array<string>}

    let getAllLenovoModels (lenovoCatalogProducts:seq<LenovoCatalogProduct>) =
        let models = 
            lenovoCatalogProducts
            |>Seq.map(fun p-> p.Queries.ModelTypes)
            |>Seq.concat
            |>Seq.map(fun mt -> 
                match mt with 
                |ModelType modelType -> ModelCode.createUnsafe modelType false
                )
            |>Seq.toArray
        models

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
            | true -> Some matchedOs.[0]
            | false ->    
                match osBuild with
                | "*" -> 
                    let matchedOsBuild = matchedProducts |> Seq.filter (fun p -> (p.Os = os) && (p.OsBuild.Value = osBuild))|>Seq.toArray
                    match matchedOsBuild.Length > 0 with
                    |true -> Some matchedOsBuild.[0]
                    |false -> Some (getHighestOsBuildProduct matchedProducts)
                |_ -> Some (getHighestOsBuildProduct matchedProducts)
        |false -> None
                        
                  
       