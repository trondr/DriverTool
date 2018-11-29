namespace DriverTool

module WebParsing =
    let logger = 
        Logging.getLoggerByName("CreateDriverPackage")
    open System    

    let getContentFromWebPage (uri:string)  =  
        try
            let html = DriverTool.CSharpLib.WebParser.GetWebPageContentUnSafe(uri, logger)
            Result.Ok html
        with
        | ex -> 
            let msg = String.Format("Failed to get web content for web page '{0}' due to {1}",uri, ex.Message)
            Result.Error (new System.Exception(msg,ex))
    
    open F
    
    let getLenovoSccmPackageDownloadUrl (uri:string) =
        let content = getContentFromWebPage uri
        match content with
        |Ok v -> 
            match v with
            |Regex @"((https[s]?):\/\/[^\s]+\.exe).+?<p>SHA-256:(.+?)</p>" [file;na;sha256] -> Result.Ok (file,sha256)
            |_ -> Result.Error (new Exception("Failed to get download url"))
        |Error ex -> Result.Error ex
        //let exePattern = @"((https[s]?):\/\/[^\s]+\.exe).+?<p>SHA-256:(.+?)</p>"
        //let tcxPattern = @"((https[s]?):\/\/[^\s]+\.exe).+?<p>SHA-256:(.+?)</p>"

        //Result.Ok ""