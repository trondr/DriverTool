namespace DriverTool

module DownloadActor =
            
    open DriverTool.Library.Messages
    open DriverTool.Library.Logging
    open DriverTool.Library.F
    open DriverTool.Library.F0
    open DriverTool.Actors
    open Akka.FSharp
    let logger = getLoggerByName "DownloadActor"

    let downloadSccmPackage (sccmPackageDownloadContext:SccmPackageInfoDownloadContext) =
        match(result{
            let downloadSccmPackage = DriverTool.Updates.downloadSccmPackageFunc sccmPackageDownloadContext.Manufacturer
            let! downloadedSccmPackage = downloadSccmPackage (sccmPackageDownloadContext.CacheFolderPath, sccmPackageDownloadContext.SccmPackage)
            return downloadedSccmPackage
        })with
        |Result.Error ex ->            
            CreateDriverPackageMessage.Error ex
        |Ok downloadedSccmPackage -> 
            DownloadedSccmPackage downloadedSccmPackage

    let downloadSccmPackageAsync context =
        System.Threading.Tasks.Task.Run(fun () -> downloadSccmPackage context)
        |>Async.AwaitTask

    let downloadActor (mailbox:Actor<_>) =
        
        let rec loop () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender, mailbox.Context.Self)
                match message with
                |DownloadPackage package -> 
                    logger.Info(sprintf "Downloading package %A." package)
                    throwNotImplementedException logger message
                |DownloadSccmPackage sccmPackageDownloadContext -> 
                    logger.Info(sprintf "Downloading sccm package %A." sccmPackageDownloadContext)
                    (downloadSccmPackageAsync sccmPackageDownloadContext)
                    |>pipeToWithSender self sender
                |DownloadedSccmPackage downloadedSccmPackage ->
                    logger.Info(sprintf "Sccm package has been downloaded: %A. Notify requester." downloadedSccmPackage)
                    sender <! (DownloadedSccmPackage downloadedSccmPackage)
                |CreateDriverPackageMessage.Error ex ->
                    logger.Warn(sprintf "Download failed due to: %s" (getAccumulatedExceptionMessages ex))
                    logger.Warn("Ignoring download failure and continue processing.")
                | _ ->
                    logger.Warn(sprintf "Message not handled by DownloadActor: %A" message)
                    return! loop()
                return! loop ()
            }
        loop()

    