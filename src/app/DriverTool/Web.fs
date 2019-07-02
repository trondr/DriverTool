namespace DriverTool

open System
open System.Net

module Web =
    open log4net

    type Web = class end
    let logger = Logging.getLoggerByName(typeof<Web>.Name)

    type WebFile = {Url:string; Checksum:string; FileName:string;Size:Int64}

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
    
    open FSharp.Control.Reactive


    /// <summary>
    /// progressActor makes sure progress info is output to the console nicely and orderly from multiple threads.
    /// </summary>
    let progressActor = 
        MailboxProcessor.Start(fun inbox -> 
            let rec messageLoop() = async{
                let! msg = inbox.Receive()
                printf "%s" msg
                return! messageLoop()
                }        
            messageLoop()
            )

    let progressMessage percentage count totalCount msg =
        sprintf "%3i%% (%10i of %10i): %-47s\r" percentage count totalCount msg
        
    let printProgress sourceUri (progress:DownloadProgressChangedEventArgs) = 
        progressActor.Post (progressMessage progress.ProgressPercentage progress.BytesReceived progress.TotalBytesToReceive sourceUri)

    let getWebProxy (webProxyUrl:string) (byPassOnLocal:bool) (byPassList:string[]) = 
        match webProxyUrl with
        | "" -> null
        | null -> null
        | url -> 
                let webProxy = new WebProxy(url, byPassOnLocal, byPassList)
                logger.Info(sprintf "WebProxy='%s', BypassOnlocal=%b, ByPassList=%A" url byPassOnLocal byPassList)
                webProxy
    
    let downloadFileBase (sourceUri:Uri, force, destinationFilePath:FileSystem.Path) =
        try
            use webClient = new WebClient()
            let webProxy = getWebProxy Configuration.getWebProxyUrl Configuration.getWebProxyByPassOnLocal Configuration.getWebProxyByPassList
            webClient.Proxy <- webProxy
            use disposable = 
                webClient.DownloadProgressChanged.Subscribe (fun progress -> printProgress sourceUri.OriginalString progress)
            let webHeaderCollection = new WebHeaderCollection()
            webHeaderCollection.Add("User-Agent", "DriverTool/1.0") 
            webClient.Headers <- webHeaderCollection                          
            match (FileOperations.ensureFileDoesNotExist force destinationFilePath) with
            |Ok path -> 
                logger.Info(sprintf "Downloading '%s' -> '%s'..." sourceUri.OriginalString (FileSystem.pathValue path))
                let downloadTask = webClient.DownloadFileTaskAsync(sourceUri.OriginalString,FileSystem.pathValue path)                
                Async.AwaitTask downloadTask |> Async.RunSynchronously                
                Result.Ok path      
            |Error ex -> Result.Error (new Exception((sprintf "Destination file '%s' allready exists" (FileSystem.pathValue destinationFilePath)), ex))
        with
        | ex -> Result.Error (new Exception(sprintf "Failed to download '%s' due to '%s'" sourceUri.OriginalString (getAccumulatedExceptionMessages ex), ex))
    
    let downloadFile (sourceUri:Uri, force, destinationFilePath) =
        Logging.genericLoggerResult Logging.LogLevel.Debug downloadFileBase (sourceUri, force, destinationFilePath)

    let hasSameFileHash downloadInfo =
        (DriverTool.Checksum.hasSameFileHash (downloadInfo.DestinationFile, downloadInfo.SourceChecksum, downloadInfo.SourceFileSize))

    let downloadIsRequired downloadInfo =
        (String.IsNullOrEmpty(downloadInfo.SourceChecksum))||(not (hasSameFileHash downloadInfo))
    
    type HasSameFileHashFunc = (DownloadInfo) -> bool
    type IsTrustedFunc = FileSystem.Path -> bool

    let verifyDownloadBase (hasSameFileHashFunc: HasSameFileHashFunc, isTrustedFunc: IsTrustedFunc, logger: ILog , downloadInfo, ignoreVerificationErrors) = 
        match (hasSameFileHashFunc downloadInfo) with
        |true  -> 
            logger.Info(sprintf "Destination file ('%s') hash match source file ('%s') hash." )
            Result.Ok downloadInfo
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

    let toUriUnsafe url =
        new Uri(url)
    
    let toUri url =
        tryCatchWithMessage toUriUnsafe url (sprintf "Failed to create uri '%s'." url)

    let downloadWebFile (destinationFolderPath:FileSystem.Path) (webFile:WebFile) =
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

    let getFolderNameFromUrl (url:string) =
        let fileName = getFileNameFromUrl url
        url.Replace(fileName,"").Trim(System.IO.Path.AltDirectorySeparatorChar)