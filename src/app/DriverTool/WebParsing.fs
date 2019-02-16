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
            let msg = sprintf "Failed to get web content for web page '%s' due to %s" uri ex.Message
            Result.Error (new System.Exception(msg,ex))
    
    open DriverTool.PathOperations
    open DriverTool.FileOperations

    let downloadWebContent url destinationFilePath refresh =
        match (fileExists destinationFilePath) && (not refresh) with
        |false -> 
            result{
                let! content = getContentFromWebPage url
                let! filePath = FileSystem.path (getTempFile "ds112090.html")
                let! writeResult = 
                    content |> (writeContentToFile filePath)
                return destinationFilePath
            }
        | true -> Result.Ok destinationFilePath   