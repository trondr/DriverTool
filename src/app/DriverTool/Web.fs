namespace DriverTool

open System
open System.Net

module Web =
    
    let downloadFilePlain (sourceUri:Uri, destinationFile, force) =
        try
            use webClient = new WebClient()
            let webHeaderCollection = new WebHeaderCollection()
            webHeaderCollection.Add("User-Agent", "LenovoUtil/1.0") 
            webClient.Headers <- webHeaderCollection
            
            let destinationPath = Path.createWithContinuation (fun p -> FileOperations.ensureFileDoesNotExist force p) (fun ex -> Result.Error ex) destinationFile  
            
            match destinationPath with
            |Ok path -> 
                Console.WriteLine("Downloading '{0}' -> {1}...", sourceUri.OriginalString, path.Value)
                webClient.DownloadFile(sourceUri.OriginalString,path.Value)
                Result.Ok destinationFile      
            |Error ex -> Result.Error (new Exception(String.Format("Destination file '{0}' allready exists", destinationFile), ex))            
        with
        | ex -> Result.Error (new Exception( String.Format("Failed to download {0} due to {e.Message}",sourceUri.OriginalString, ex.Message),ex))
    
    let downloadFile (sourceUri:Uri, destinationFile, force) =
        Logging.debugLoggerResult downloadFilePlain (sourceUri, destinationFile, force)



