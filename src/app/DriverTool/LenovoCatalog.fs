namespace DriverTool

module LenovoCatalog =
    
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

    let getSccmPackagesInfo =
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