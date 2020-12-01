namespace DriverTool.Library

open System
open System.Net
open DriverTool.Library.F0
open DriverTool.Library.F

module WebDownload=
    open Common.Logging
    open DriverTool.Library.Logging
    
    type WebDownload = class end
    let logger = getLoggerByName(typeof<WebDownload>.Name)

    type WebFileSource = {Url:Uri; Checksum:string option; Size:Int64 option; FileName:string}
    type WebFileDestination = {DestinationFile:FileSystem.Path}
    type WebFileDownload = {Source:WebFileSource;Destination:WebFileDestination}

    type WebFile2 =
        |SourceWebFile of WebFileSource
        |DownloadWebFile of WebFileSource*WebFileDestination
        |DownloadedWebFile of WebFileDestination
    

    let toUriUnsafe url =
        new Uri(url)
    
    let toUri url =
        tryCatchWithMessage toUriUnsafe url (sprintf "Failed to create uri '%s'." url)

    let toOptionalString value =
        match value with
        |null -> None
        |value when String.IsNullOrWhiteSpace(value) -> None
        |_ -> Some value
    
    let toOptionalSize size =
        match size with
        |size when size > 0L -> Some size
        |_ -> None

    let getFileNameFromUri (uri:Uri) =
        System.IO.Path.GetFileName(uri.LocalPath)

    let toWebFileSource url checksum size =
        result{
            let! uri = toUri url
            let optionalChecksum = toOptionalString checksum
            let optionalSize = toOptionalSize size
            let fileName = getFileNameFromUri uri
            let webFileSource =
                {
                    Url = uri
                    Checksum = optionalChecksum
                    Size = optionalSize
                    FileName = fileName
                }
            return webFileSource
        }

    let toWebFileDownload url checksum size destinationFile =
        result{
            let! webFileSource = toWebFileSource url checksum size
            return {Source=webFileSource;Destination={DestinationFile=destinationFile}}
        }

    let (|TextFile|_|) (input:string) = if input.ToLower().EndsWith(".txt") then Some(input) else None
    let (|XmlFile|_|) (input:string) = if input.ToLower().EndsWith(".xml") then Some(input) else None

    let ignoreVerificationErrors webFile =
        match FileSystem.pathValue webFile.DestinationFile with
        | TextFile _ -> true
        | XmlFile _ -> true
        | _ -> false

    let getWebProxy (logger:Common.Logging.ILog) (webProxyUrl:string) (byPassOnLocal:bool) (byPassList:string[]) = 
        match webProxyUrl with
        | "" -> null
        | null -> null
        | url -> 
                let webProxy = new WebProxy(url, byPassOnLocal, byPassList)
                logger.Info(msg (sprintf "WebProxy='%s', BypassOnlocal=%b, ByPassList=%A" url byPassOnLocal byPassList))
                webProxy

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

    let downloadFile' (webProxy, force, destinationFilePath, sourceUri:Uri) =
        try
            use webClient = new WebClient()            
            webClient.Proxy <- webProxy
            use _disposable = webClient.DownloadProgressChanged.Subscribe (fun progress -> printProgress sourceUri.OriginalString progress)
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

    let downloadFile force (sourceUri,destinationFilePath)  =        
        let webProxy = getWebProxy logger DriverTool.Library.Configuration.getWebProxyUrl DriverTool.Library.Configuration.getWebProxyByPassOnLocal DriverTool.Library.Configuration.getWebProxyByPassList
        genericLoggerResult LogLevel.Debug downloadFile' (webProxy, force, destinationFilePath, sourceUri)

    let useCachedVersion' (logger:Common.Logging.ILog) (fileExists:string->bool) (destinationFile:FileSystem.Path) =
        let useCachedVersionFilePath =((FileSystem.pathValue destinationFile) + ".usecachedversion")
        let useCachedVersionFileExists = (fileExists useCachedVersionFilePath) 
        let destinationFileExists = (fileExists (FileSystem.pathValue destinationFile))
        let useCachedVersion = useCachedVersionFileExists && destinationFileExists          
        match useCachedVersion with
        |true -> 
            logger.Warn(sprintf "Using cached version of '%A'. Skipping download." destinationFile)
            true
        |false -> 
            false       

    let useCachedVersion (destinationFile:FileSystem.Path) =        
        useCachedVersion' logger System.IO.File.Exists destinationFile
    
    let downloadIsRequired' (logger:Common.Logging.ILog) (hasSameFileHashF:(FileSystem.Path*string option*Int64 option->bool)) (useCachedVersionF:FileSystem.Path->bool) sourceChecksum sourceFileSize destinationFile =
        let doNotUseCachedVersion = not (useCachedVersionF destinationFile)
        let doesNotHaveSameFileHash = not (hasSameFileHashF (destinationFile, sourceChecksum, sourceFileSize))        
        doesNotHaveSameFileHash && doNotUseCachedVersion

    let downloadIsRequired sourceChecksum sourceFileSize destinationFile =
        downloadIsRequired' logger DriverTool.Library.Checksum.hasSameFileHash2 useCachedVersion sourceChecksum sourceFileSize destinationFile

    type HasSameFileHashFunc = FileSystem.Path*string option*Int64 option->bool
    type IsTrustedFunc = FileSystem.Path -> bool

    let verifyDownload' (logger:Common.Logging.ILog) (hasSameFileHashFunc: HasSameFileHashFunc) (isTrustedFunc: IsTrustedFunc) ignoreVerificationErrors (webFileSource, webFileDestination)  = 
        match (hasSameFileHashFunc (webFileDestination.DestinationFile,webFileSource.Checksum,webFileSource.Size)) with
        |true  -> 
            logger.Info(msg (sprintf  "Destination file ('%A') hash match source file ('%A') hash." webFileDestination webFileSource.Url ))
            Result.Ok (WebFile2.DownloadedWebFile webFileDestination)
        |false ->
            let msg1 = (sprintf "Destination file ('%s') hash does not match source file ('%s') hash. " (FileSystem.pathValue webFileDestination.DestinationFile) webFileSource.Url.OriginalString)
            match ignoreVerificationErrors with
            |true ->
                logger.Warn(msg1)
                Result.Ok (WebFile2.DownloadedWebFile webFileDestination)
            |false->
                let isTrusted = isTrustedFunc webFileDestination.DestinationFile
                match isTrusted with
                |true ->                    
                    logger.Warn(msg1 + "However the file is trusted (the file is digitally signed) so it is assumed that there is a mistake in the published checksum data on the vendor web page.")
                    Result.Ok (WebFile2.DownloadedWebFile webFileDestination)
                |false ->    
                    Result.Error (new Exception(msg1 + "Additionally the file is not trusted (not signed or signature has been invalidated.)"))

    let verifyDownload ignoreVerificationErrors download =
        verifyDownload' logger (DriverTool.Library.Checksum.hasSameFileHash2) (Cryptography.isTrusted) ignoreVerificationErrors download
    
    type DownloadFileFunc = (bool->Uri*FileSystem.Path -> Result<FileSystem.Path,Exception>)
    type VerifyDownloadFunc = (bool->(WebFileSource* WebFileDestination)->Result<WebFile2,Exception>)

    let downloadIfDifferent' (logger:ILog) downloadIsRequired' (downloadFile':DownloadFileFunc) (verifyDownload':VerifyDownloadFunc) ignoreVerificationErrors webFileDownload =        
        let webFileSource = webFileDownload.Source
        let webFileDestination = webFileDownload.Destination
        let isDownloadRequired = downloadIsRequired' webFileSource.Checksum webFileSource.Size webFileDestination.DestinationFile
        match isDownloadRequired with
        |true -> 
            let downloadResult = downloadFile' true (webFileSource.Url,webFileDestination.DestinationFile)
            match downloadResult with
            |Result.Ok s -> 
                verifyDownload' ignoreVerificationErrors (webFileSource, webFileDestination) 
            |Result.Error ex -> Result.Error (new Exception((sprintf "Failed to download '%A' due to: %s " webFileSource.Url ex.Message), ex))
        |false -> 
            logger.Info(msg (sprintf "Destination file '%s' allready exists." (FileSystem.pathValue webFileDestination.DestinationFile)))
            Result.Ok (WebFile2.DownloadedWebFile webFileDestination)

    let downloadIfDifferent ignoreVerificationErrors webFileDownload =
        downloadIfDifferent' logger downloadIsRequired downloadFile verifyDownload ignoreVerificationErrors webFileDownload