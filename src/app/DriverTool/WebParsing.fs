namespace DriverTool

module WebParsing =
    let logger = 
        Logging.getLoggerByName("WebParsing")
    open System    

    let getContentFromWebPage (uri:string)  =  
        try
            let html = DriverTool.CSharpLib.WebParser.GetWebPageContentUnSafe(uri, logger)
            Result.Ok html
        with
        | ex -> 
            let msg = String.Format("Failed to get web content for web page '{0}' due to {1}",uri, ex.Message)
            Result.Error (new System.Exception(msg,ex))
    
    type SccmPackageDownloadInfo = {
        ReadmeUrl: string;
        ReadmeChecksum: string;
        InstallerUrl:string;
        InstallerChecksum:string
    }

    open F    
           
    let getLenovoSccmPackageDownloadInfo (uri:string) =
        let content = getContentFromWebPage uri
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