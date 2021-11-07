namespace DriverTool.Library

open System
open System.Net
open DriverTool.Library.F0
open DriverTool.Library.F

module Web =
    open Common.Logging
    open DriverTool.Library.Logging
    type Web = class end
    let logger = getLoggerByName(typeof<Web>.Name)

    type DownloadStatus = NotDownloaded|Downloaded|DownloadedAndVerified

    type WebFile = {Url:string; Checksum:string; Size:Int64; FileName:string}
        
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
    
    let getWebProxy (webProxyUrl:string) (byPassOnLocal:bool) (byPassList:string[]) = 
        match webProxyUrl with
        | "" -> null
        | null -> null
        | url -> 
                let webProxy = new WebProxy(url, byPassOnLocal, byPassList)
                logger.Info(sprintf "WebProxy='%s', BypassOnlocal=%b, ByPassList=%A" url byPassOnLocal byPassList)
                webProxy
    
    let downloadFile reportProgress (sourceUri:Uri) force (destinationFilePath:FileSystem.Path) =
        try
            use webClient = new WebClient()
            let webProxy = getWebProxy DriverTool.Library.Configuration.getWebProxyUrl DriverTool.Library.Configuration.getWebProxyByPassOnLocal DriverTool.Library.Configuration.getWebProxyByPassList
            webClient.Proxy <- webProxy
            use subscription = webClient.DownloadProgressChanged.Subscribe (fun progress -> 
                    let percentage = (float progress.BytesReceived) / (float progress.TotalBytesToReceive) * 100.0
                    //reportProgress true (Some percentage) (sprintf "Downloading '%s'..." sourceUri.OriginalString)
                    reportProgress (sprintf "Downloading '%s'..." sourceUri.OriginalString) String.Empty String.Empty (Some percentage) true None
                )
            let webHeaderCollection = new WebHeaderCollection()
            webHeaderCollection.Add("User-Agent", "DriverTool/1.0")
            webClient.Headers <- webHeaderCollection                          
            match (FileOperations.ensureFileDoesNotExist force destinationFilePath) with
            |Ok path ->                 
                reportProgress (sprintf "Downloading '%s' -> '%s'..." sourceUri.OriginalString (FileSystem.pathValue path)) String.Empty String.Empty (Some 0.0) true None
                let downloadTask = webClient.DownloadFileTaskAsync(sourceUri.OriginalString,FileSystem.pathValue path)                
                Async.AwaitTask downloadTask |> Async.RunSynchronously            
                reportProgress sourceUri.OriginalString String.Empty String.Empty (Some 100.0) false None
                Result.Ok path      
            |Result.Error ex -> toErrorResult ex (Some(sprintf "Destination file '%s' allready exists" (FileSystem.pathValue destinationFilePath)))
        with
        | ex -> toErrorResult ex (Some(sprintf "Failed to download '%s' due to '%s'" sourceUri.OriginalString (getAccumulatedExceptionMessages ex)))

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
        ((String.IsNullOrEmpty(downloadInfo.SourceChecksum) && (not (Cryptography.isTrusted downloadInfo.DestinationFile) ))||(not (hasSameFileHash downloadInfo))) && (not (useCachedVersion logger downloadInfo))
    
    type HasSameFileHashFunc = (DownloadInfo) -> bool
    type IsTrustedFunc = FileSystem.Path -> bool

    let verifyDownloadBase (hasSameFileHashFunc: HasSameFileHashFunc) (isTrustedFunc: IsTrustedFunc) (logger: ILog) downloadInfo ignoreVerificationErrors = 
        match (hasSameFileHashFunc downloadInfo) with
        |true  -> 
            logger.Info(sprintf  "Destination file ('%A') hash match source file ('%A') hash." (downloadInfo.DestinationFile) (downloadInfo.SourceUri) )
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
        verifyDownloadBase hasSameFileHash Cryptography.isTrusted logger downloadInfo ignoreVerificationErrors

    let downloadIfDifferent logger reportProgress downloadInfo ignoreVerificationErrors =        
        match (downloadIsRequired logger downloadInfo) with
        |true -> 
            match (downloadFile reportProgress downloadInfo.SourceUri true downloadInfo.DestinationFile) with
            |Ok s -> 
                verifyDownload logger downloadInfo ignoreVerificationErrors
            |Result.Error ex -> Result.Error (new Exception(sprintf "Failed to download '%A' due to: %s " downloadInfo.SourceUri ex.Message, ex))
        |false -> 
            logger.Info(sprintf "Destination file '%s' allready exists." (FileSystem.pathValue downloadInfo.DestinationFile))
            Result.Ok downloadInfo

    let toUriUnsafe url =
        new Uri(url)
    
    let toUri url =
        tryCatch (Some (sprintf "Failed to create uri '%s'." url)) toUriUnsafe url 

    let downloadWebFile logger reportProgress (destinationFolderPath:FileSystem.Path) (webFile:WebFile) =
        result{
            let! destinationFilePath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue destinationFolderPath,webFile.FileName))
            let! sourceUri = toUri webFile.Url
            let downloadInfo = { SourceUri = sourceUri;SourceChecksum = webFile.Checksum;SourceFileSize = webFile.Size;DestinationFile = destinationFilePath}
            let! downloadedInfo = downloadIfDifferent logger reportProgress downloadInfo false
            return downloadedInfo
        }

    let getFileNameFromUrl (url:string) =
        let uri = new Uri(url)        
        uri.Segments.[uri.Segments.Length-1]

    let getFolderNameFromUrl (url:string) =
        let fileName = getFileNameFromUrl url
        url.Replace(fileName,"").Trim(System.IO.Path.AltDirectorySeparatorChar)

    type WebFileSource = {Url:Uri; Checksum:string option; Size:Int64 option; FileName:string}
    type WebFileDestination = {DestinationFile:FileSystem.Path}
    type WebFileDownload = {Source:WebFileSource;Destination:WebFileDestination}

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
