namespace DriverTool

open System
open System.Net

module Web =
    type Web = class end
    let logger = Logging.Logger<Web>

    type DownloadInfo =
        {
            SourceUri:Uri;
            SourceChecksum:string;
            SourceFileSize:Int64;            
            DestinationFile:Path;            
        }

    let downloadFilePlain (sourceUri:Uri, force, destinationFilePath:Path) =
        try
            use webClient = new WebClient()
            let webHeaderCollection = new WebHeaderCollection()
            webHeaderCollection.Add("User-Agent", "LenovoUtil/1.0") 
            webClient.Headers <- webHeaderCollection                          
            match (FileOperations.ensureFileDoesNotExist (force, destinationFilePath)) with
            |Ok path -> 
                logger.InfoFormat("Downloading '{0}' -> {1}...", sourceUri.OriginalString, path.Value)
                webClient.DownloadFile(sourceUri.OriginalString,path.Value)
                Result.Ok path      
            |Error ex -> Result.Error (new Exception(String.Format("Destination file '{0}' allready exists", destinationFilePath.Value), ex))            
        with
        | ex -> Result.Error (new Exception( String.Format("Failed to download {0} due to {e.Message}",sourceUri.OriginalString, ex.Message),ex))
    
    let downloadFile (sourceUri:Uri, force, destinationFilePath) =
        Logging.debugLoggerResult downloadFilePlain (sourceUri, force, destinationFilePath)

    let hasSameFileHash downloadInfo =
        (DriverTool.Checksum.hasSameFileHash (downloadInfo.DestinationFile.Value, downloadInfo.SourceChecksum, downloadInfo.SourceFileSize))

    let downloadIsRequired downloadInfo =
        not (hasSameFileHash downloadInfo)        
    
    let verifyDownload downloadInfo ignoreVerificationErrors =
        match (hasSameFileHash downloadInfo) with
        |true  -> Result.Ok downloadInfo
        |false -> 
            let msg = String.Format("Destination file ('{0}') hash does not match source file ('{1}') hash.",downloadInfo.DestinationFile,downloadInfo.SourceUri.OriginalString)
            match ignoreVerificationErrors with
            |true ->
                Logging.getLoggerByName("verifyDownload").Warn(msg)
                Result.Ok downloadInfo
            |false->Result.Error (new Exception(msg))

    let downloadIfDifferent (downloadInfo, ignoreVerificationErrors) =        
        match (downloadIsRequired downloadInfo) with
        |true -> 
            match (downloadFile (downloadInfo.SourceUri, true, downloadInfo.DestinationFile)) with
            |Ok s -> 
                verifyDownload downloadInfo ignoreVerificationErrors
            |Error ex -> Result.Error (new Exception("Download could not be verified. " + ex.Message))
        |false -> 
            logger.Info(String.Format("Destination file '{0}' allready exists.", downloadInfo.DestinationFile))
            Result.Ok downloadInfo

        
