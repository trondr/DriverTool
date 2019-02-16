namespace DriverTool

open System
open System.Net

module Web =
    open log4net

    type Web = class end
    let logger = Logging.getLoggerByName(typeof<Web>.Name)

    type DownloadInfo =
        {
            SourceUri:Uri;
            SourceChecksum:string;
            SourceFileSize:Int64;            
            DestinationFile:FileSystem.Path;            
        }

    let (|TextFile|_|) (input:string) = if input.ToLower().EndsWith(".txt") then Some(input) else None
    let (|XmlFile|_|) (input:string) = if input.ToLower().EndsWith(".xml") then Some(input) else None

    let ignoreVerificationErrors downloadInfo =
        match FileSystem.pathValue downloadInfo.DestinationFile with
        | TextFile _ -> true
        | XmlFile _ -> true
        | _ -> false

    let downloadFileBase (sourceUri:Uri, force, destinationFilePath:FileSystem.Path) =
        try
            use webClient = new WebClient()
            webClient.Proxy <- null;
            let webHeaderCollection = new WebHeaderCollection()
            webHeaderCollection.Add("User-Agent", "LenovoUtil/1.0") 
            webClient.Headers <- webHeaderCollection                          
            match (FileOperations.ensureFileDoesNotExist force destinationFilePath) with
            |Ok path -> 
                logger.InfoFormat("Downloading '{0}' -> {1}...", sourceUri.OriginalString, FileSystem.pathValue path)
                webClient.DownloadFile(sourceUri.OriginalString,FileSystem.pathValue path)
                Result.Ok path      
            |Error ex -> Result.Error (new Exception((sprintf "Destination file '%s' allready exists" (FileSystem.pathValue destinationFilePath)), ex))
        with
        | ex -> Result.Error (new Exception(sprintf "Failed to download '%s' due to '%s'" sourceUri.OriginalString ex.Message, ex))
    
    let downloadFile (sourceUri:Uri, force, destinationFilePath) =
        Logging.genericLoggerResult Logging.LogLevel.Debug downloadFileBase (sourceUri, force, destinationFilePath)

    let hasSameFileHash downloadInfo =
        (DriverTool.Checksum.hasSameFileHash (downloadInfo.DestinationFile, downloadInfo.SourceChecksum, downloadInfo.SourceFileSize))

    let downloadIsRequired downloadInfo =
        not (hasSameFileHash downloadInfo)        
    
    type HasSameFileHashFunc = (DownloadInfo) -> bool
    type IsTrustedFunc = FileSystem.Path -> bool

    let verifyDownloadBase (hasSameFileHashFunc: HasSameFileHashFunc, isTrustedFunc: IsTrustedFunc, logger: ILog , downloadInfo, ignoreVerificationErrors) = 
        match (hasSameFileHashFunc downloadInfo) with
        |true  -> Result.Ok downloadInfo
        |false ->
            let msg = sprintf "Destination file ('%s') hash does not match source file ('%s') hash. " (FileSystem.pathValue downloadInfo.DestinationFile) downloadInfo.SourceUri.OriginalString
            match ignoreVerificationErrors with
            |true ->
                logger.Warn(msg)
                Result.Ok downloadInfo
            |false->
                let isTrusted = isTrustedFunc downloadInfo.DestinationFile
                match isTrusted with
                |true ->                    
                    logger.Warn(msg + "However the file is trusted (the file is digitally signed) so it is assumed that there is a mistake in the published checksum data on the vendor web page.")
                    Result.Ok downloadInfo
                |false ->    
                    Result.Error (new Exception(msg + "Additionally the file is not trusted (not signed or signature has been invalidated.)"))


    let verifyDownload downloadInfo ignoreVerificationErrors =
        verifyDownloadBase (hasSameFileHash,Cryptography.isTrusted,Logging.getLoggerByName("verifyDownload"),downloadInfo,ignoreVerificationErrors)

    let downloadIfDifferent (downloadInfo, ignoreVerificationErrors) =        
        match (downloadIsRequired downloadInfo) with
        |true -> 
            match (downloadFile (downloadInfo.SourceUri, true, downloadInfo.DestinationFile)) with
            |Ok s -> 
                verifyDownload downloadInfo ignoreVerificationErrors
            |Result.Error ex -> Result.Error (new Exception(sprintf "Failed to download '%A' due to: %s " downloadInfo.SourceUri ex.Message, ex))
        |false -> 
            logger.Info(sprintf "Destination file '%s' allready exists." (FileSystem.pathValue downloadInfo.DestinationFile))
            Result.Ok downloadInfo

    type WebFile = {Url:string; Checksum:string; FileName:string;Size:Int64}

    let toUriUnsafe url =
        new Uri(url)
    
    let toUri url =
        tryCatchWithMessage toUriUnsafe url (sprintf "Failed to create uri '%s'." url)

    let downloadWebFile (destinationFolderPath:FileSystem.Path, webFile:WebFile) =
        result{
            let! destinationFilePath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue destinationFolderPath,webFile.FileName))
            let! sourceUri = toUri webFile.Url
            let downloadInfo = { SourceUri = sourceUri;SourceChecksum = webFile.Checksum;SourceFileSize = webFile.Size;DestinationFile = destinationFilePath}
            let! downloadedInfo = downloadIfDifferent (downloadInfo,false)            
            return downloadedInfo
        }

    let getFileNameFromUrl (url:string) =
        let uri = new Uri(url)        
        uri.Segments.[uri.Segments.Length-1]