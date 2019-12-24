namespace DriverTool

open System
open System.Net
open DriverTool.Library.F0
open DriverTool.Library.F
open DriverTool.Library

module Web =
    open Common.Logging
    open DriverTool.Library.Logging
    type Web = class end
    let logger = getLoggerByName(typeof<Web>.Name)

    type WebFile = {Url:string; Checksum:string; FileName:string;Size:Int64}

    type DownloadInfo =
        {
            SourceUri:Uri;
            SourceChecksum:string;
            SourceFileSize:Int64;            
            DestinationFile:FileSystem.Path;            
        }

    let toDownloadInfo sourceUri sourceChecksum sourceFileSize destinationFilePath =
        {
            SourceUri = sourceUri
            SourceChecksum = sourceChecksum
            SourceFileSize = sourceFileSize
            DestinationFile = destinationFilePath        
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
                logger.Info(msg (sprintf "WebProxy='%s', BypassOnlocal=%b, ByPassList=%A" url byPassOnLocal byPassList))
                webProxy
    
    let downloadFileBase (sourceUri:Uri, force, destinationFilePath:FileSystem.Path) =
        try
            use webClient = new WebClient()
            let webProxy = getWebProxy DriverTool.Library.Configuration.getWebProxyUrl DriverTool.Library.Configuration.getWebProxyByPassOnLocal DriverTool.Library.Configuration.getWebProxyByPassList
            webClient.Proxy <- webProxy
            use disposable = 
                webClient.DownloadProgressChanged.Subscribe (fun progress -> printProgress sourceUri.OriginalString progress)
            let webHeaderCollection = new WebHeaderCollection()
            webHeaderCollection.Add("User-Agent", "DriverTool/1.0") 
            webClient.Headers <- webHeaderCollection                          
            match (FileOperations.ensureFileDoesNotExist force destinationFilePath) with
            |Ok path -> 
                logger.Info(msg (sprintf "Downloading '%s' -> '%s'..." sourceUri.OriginalString (FileSystem.pathValue path)))
                let downloadTask = webClient.DownloadFileTaskAsync(sourceUri.OriginalString,FileSystem.pathValue path)                
                Async.AwaitTask downloadTask |> Async.RunSynchronously                
                Result.Ok path      
            |Result.Error ex -> Result.Error (new Exception((sprintf "Destination file '%s' allready exists" (FileSystem.pathValue destinationFilePath)), ex))
        with
        | ex -> Result.Error (new Exception(sprintf "Failed to download '%s' due to '%s'" sourceUri.OriginalString (getAccumulatedExceptionMessages ex), ex))
    
    let downloadFile (sourceUri:Uri, force, destinationFilePath) =
        genericLoggerResult LogLevel.Debug downloadFileBase (sourceUri, force, destinationFilePath)

    let hasSameFileHash downloadInfo =
        (DriverTool.Library.Checksum.hasSameFileHash (downloadInfo.DestinationFile, downloadInfo.SourceChecksum, downloadInfo.SourceFileSize))

    let useCachedVersionBase (logger:Common.Logging.ILog) (fileExists:string->bool) (downloadInfo:DownloadInfo) = 
        let useCachedVersionFilePath =((FileSystem.pathValue downloadInfo.DestinationFile) + ".usecachedversion")
        let useCachedVersionFileExists = (fileExists useCachedVersionFilePath) 
        let destinationFileExists = (fileExists (FileSystem.pathValue downloadInfo.DestinationFile))
        let useCachedVersion = useCachedVersionFileExists && destinationFileExists          
        match useCachedVersion with
        |true -> 
            logger.Warn(sprintf "Using cached version of '%A'. Skipping download." downloadInfo.DestinationFile)
            true
        |false -> 
            false   

    let useCachedVersion (logger:Common.Logging.ILog) (downloadInfo:DownloadInfo) =        
        useCachedVersionBase logger System.IO.File.Exists downloadInfo

    let downloadIsRequired logger downloadInfo =
        ((String.IsNullOrEmpty(downloadInfo.SourceChecksum))||(not (hasSameFileHash downloadInfo))) && (not (useCachedVersion logger downloadInfo))
    
    type HasSameFileHashFunc = (DownloadInfo) -> bool
    type IsTrustedFunc = FileSystem.Path -> bool

    let verifyDownloadBase (hasSameFileHashFunc: HasSameFileHashFunc, isTrustedFunc: IsTrustedFunc, logger: ILog , downloadInfo, ignoreVerificationErrors) = 
        match (hasSameFileHashFunc downloadInfo) with
        |true  -> 
            logger.Info(msg (sprintf  "Destination file ('%A') hash match source file ('%A') hash." (downloadInfo.DestinationFile) (downloadInfo.SourceUri) ))
            Result.Ok downloadInfo
        |false ->
            let msg1 = (sprintf "Destination file ('%s') hash does not match source file ('%s') hash. " (FileSystem.pathValue downloadInfo.DestinationFile) downloadInfo.SourceUri.OriginalString)
            match ignoreVerificationErrors with
            |true ->
                logger.Warn(msg1)
                Result.Ok downloadInfo
            |false->
                let isTrusted = isTrustedFunc downloadInfo.DestinationFile
                match isTrusted with
                |true ->                    
                    logger.Warn(msg1 + "However the file is trusted (the file is digitally signed) so it is assumed that there is a mistake in the published checksum data on the vendor web page.")
                    Result.Ok downloadInfo
                |false ->    
                    Result.Error (new Exception(msg1 + "Additionally the file is not trusted (not signed or signature has been invalidated.)"))


    let verifyDownload logger downloadInfo ignoreVerificationErrors =
        verifyDownloadBase (hasSameFileHash,Cryptography.isTrusted,logger,downloadInfo,ignoreVerificationErrors)

    let downloadIfDifferent (logger, downloadInfo, ignoreVerificationErrors) =        
        match (downloadIsRequired logger downloadInfo) with
        |true -> 
            match (downloadFile (downloadInfo.SourceUri, true, downloadInfo.DestinationFile)) with
            |Ok s -> 
                verifyDownload logger downloadInfo ignoreVerificationErrors
            |Result.Error ex -> Result.Error (new Exception(sprintf "Failed to download '%A' due to: %s " downloadInfo.SourceUri ex.Message, ex))
        |false -> 
            logger.Info(msg (sprintf "Destination file '%s' allready exists." (FileSystem.pathValue downloadInfo.DestinationFile)))
            Result.Ok downloadInfo

    let toUriUnsafe url =
        new Uri(url)
    
    let toUri url =
        tryCatchWithMessage toUriUnsafe url (sprintf "Failed to create uri '%s'." url)

    let downloadWebFile (logger,destinationFolderPath:FileSystem.Path) (webFile:WebFile) =
        result{
            let! destinationFilePath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue destinationFolderPath,webFile.FileName))
            let! sourceUri = toUri webFile.Url
            let downloadInfo = { SourceUri = sourceUri;SourceChecksum = webFile.Checksum;SourceFileSize = webFile.Size;DestinationFile = destinationFilePath}
            let! downloadedInfo = downloadIfDifferent (logger,downloadInfo,false)            
            return downloadedInfo
        }

    let getFileNameFromUrl (url:string) =
        let uri = new Uri(url)        
        uri.Segments.[uri.Segments.Length-1]

    let getFolderNameFromUrl (url:string) =
        let fileName = getFileNameFromUrl url
        url.Replace(fileName,"").Trim(System.IO.Path.AltDirectorySeparatorChar)