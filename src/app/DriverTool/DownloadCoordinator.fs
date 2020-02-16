namespace DriverTool

module DownloadCoordinatorActor =
    
    open Akka.FSharp
    open Akka.Routing
    open DriverTool.Library.Messages
    open DriverTool.DownloadActor    
    open DriverTool.Library.F0
    open DriverTool.Library.Logging
    open DriverTool.Library.PackageXml
    open DriverTool.Library
    open DriverTool.Library.Web
    open DriverTool.Library.WebDownload
    let logger = getLoggerByName "DownloadCoordinatorActor"

    //The download coordinator context is used to keep track of which download jobs have been started.
    type DownloadCoordinatorContext = {Downloads:Map<FileSystem.Path,WebFileDownload>}

    //Check if download job is not allready beeing downloaded.
    let notAllreadyDownloading downloadCoordinatorContext webFileDownload  =
        not (downloadCoordinatorContext.Downloads.ContainsKey(webFileDownload.Destination.DestinationFile))

    /// Add download job to download coordinator context if not allready added.
    let updateDownloadCoordinatorContext downloadCoordinatorContext webFileDownload =
        if(not (downloadCoordinatorContext.Downloads.ContainsKey(webFileDownload.Destination.DestinationFile))) then
            {downloadCoordinatorContext with Downloads=downloadCoordinatorContext.Downloads.Add(webFileDownload.Destination.DestinationFile, webFileDownload)}
        else
            downloadCoordinatorContext

    /// Get unique downloads jobs (installer and reamde) from package. Readme file can be shared by more than one package so
    /// make sure to check if a file is not allready beeing downloaded according to the downloads coordinator context. This is necessary
    /// to avoid any file system sharing vialations when two threads attempts to download to the same destination file.
    let packageToUniqueDownloadJob downloadCoordinatorContext destinationFolderPath package =
        let uniqueDownloadJobs =                 
            (packageInfoToWebFileDownloads destinationFolderPath package)            
            |>Seq.filter(fun wfd -> notAllreadyDownloading downloadCoordinatorContext wfd)            
            |>Seq.toArray
        uniqueDownloadJobs

    let downloadCoordinatorActor (mailbox:Actor<_>) =
        
        let downloadActor = spawnOpt mailbox.Context "DownloadActor" downloadActor [ SpawnOption.Router(SmallestMailboxPool(10)) ]
        let downloadCoordinatorContext = {Downloads=Map.empty}

        let rec loop downloadCoordinatorContext = 
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender, mailbox.Context.Self)
                match message with
                |DownloadPackage (package,packagingContext) -> 
                    logger.Info(sprintf "Downloading package %A." package)                    
                    (packageToUniqueDownloadJob downloadCoordinatorContext packagingContext.CacheFolderPath package)                    
                    |> Array.map(fun dl -> 
                        logger.Info(sprintf "Request download of job %A for package %A." dl package)
                        downloadActor <! CreateDriverPackageMessage.StartDownload dl
                        ) |> ignore                                        
                |StartDownload webFileDownload ->
                    logger.Info(sprintf "Request download of job %A." webFileDownload)                                        
                    downloadActor <! CreateDriverPackageMessage.StartDownload webFileDownload
                    let updatedDownloadCoordinatorContext = updateDownloadCoordinatorContext downloadCoordinatorContext webFileDownload
                    return! loop updatedDownloadCoordinatorContext 
                |DownloadFinished webFileDownload ->
                    logger.Info(sprintf "Download of job %A is finished." webFileDownload)

                    
                |DownloadSccmPackage sccmPackageDownloadContext ->
                    logger.Info(sprintf "Request download of sccm package %A." sccmPackageDownloadContext)
                    downloadActor <! CreateDriverPackageMessage.DownloadSccmPackage sccmPackageDownloadContext                
                |CreateDriverPackageMessage.Error ex ->
                    logger.Warn(sprintf "Download failed due to: %s" (getAccumulatedExceptionMessages ex))
                    logger.Warn("Ignoring download failure and continue processing.")
                | _ ->
                    logger.Warn(sprintf "Message not handled by DownloadActorCoordinator: %A" message)
                    return! loop downloadCoordinatorContext
                return! loop downloadCoordinatorContext
            }
        loop downloadCoordinatorContext

