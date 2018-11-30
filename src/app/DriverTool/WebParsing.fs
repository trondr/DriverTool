namespace DriverTool

module WebParsing =
    let logger = 
        Logging.getLoggerByName("WebParsing")
    open System    

    let getContentFromWebPage (uri:string)  =  
        try
            logger.Info("Getting html content from: " + uri)
            let html = DriverTool.CSharpLib.WebParser.GetWebPageContentUnSafe(uri, logger)
            Result.Ok html
        with
        | ex -> 
            let msg = String.Format("Failed to get web content for web page '{0}' due to {1}", uri, ex.Message)
            Result.Error (new System.Exception(msg,ex))
    
            