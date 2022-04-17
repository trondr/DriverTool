namespace DriverTool.Library

module WebParsing =
    let logger = DriverTool.Library.Logging.getLoggerByName "WebParsing"    
    open DriverTool.Library.F
    open DriverTool.Library.FileSystem
    open DriverTool.Library.FileOperations

    let getContentFromWebPage (uri:string)  =  
        try
            logger.Info("Getting html content from: " + uri)
            let html = DriverTool.CSharpLib.WebParser.GetWebPageContentUnSafe(uri, logger)
            Result.Ok html
        with
        | ex -> 
            let msg = sprintf "Failed to get web content for web page '%s' due to %s" uri ex.Message
            Result.Error (new System.Exception(msg,ex))
    
    let downloadWebContent url destinationFilePath refresh =
        match (fileExists destinationFilePath) && (not refresh) with
        |false -> 
            result{
                let! content = getContentFromWebPage url                
                let! outputPath = 
                    content |> (writeContentToFile logger destinationFilePath)
                return outputPath
            }
        | true -> Result.Ok destinationFilePath   