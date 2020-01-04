namespace DriverTool

module Actors =
            
    open DriverTool.Library.Messages
    open DriverTool.Library.Logging
    open DriverTool.Library.F
    open DriverTool.Library.F0
    open Akka.FSharp
    let logger = getLoggerByName "DownloadActor"

    // make a pipe-friendly version of Akka.NET PipeTo for handling async computations
    let pipeToWithSender recipient sender asyncComp = pipeTo asyncComp recipient sender
    
    let downloadSccmPackage (sccmPackageDownloadContext:SccmPackageInfoDownloadContext) =
        match(result{
            let downloadSccmPackage = DriverTool.Updates.downloadSccmPackageFunc sccmPackageDownloadContext.Manufacturer
            let! downloadedSccmPackage = downloadSccmPackage (sccmPackageDownloadContext.CacheFolderPath, sccmPackageDownloadContext.SccmPackage)
            return downloadedSccmPackage
        })with
        |Result.Error ex ->            
            DownloadMessage.Error ex
        |Ok downloadedSccmPackage -> 
            DownloadMessage.DownloadedSccmPackage downloadedSccmPackage

    let downloadSccmPackageAsync context =
        System.Threading.Tasks.Task.Run(fun () -> downloadSccmPackage context)
        |>Async.AwaitTask

    let downloadActor (mailbox:Actor<_>) =
        
        let rec loop () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender, mailbox.Context.Self)
                match message with
                |DownloadMessage.DownloadPackage package -> 
                    logger.Info(sprintf "TO BE IMPLEMENTED: Downloading package %A." package)
                    sender <! "TO BE IMPLEMENTED: Downloaded package"
                |DownloadMessage.DownloadSccmPackage sccmPackageDownloadContext -> 
                    logger.Info(sprintf "Downloading sccm package %A." sccmPackageDownloadContext)
                    (downloadSccmPackageAsync sccmPackageDownloadContext)
                    |>pipeToWithSender self sender
                |DownloadMessage.DownloadedSccmPackage downloadedSccmPackage ->
                    logger.Info(sprintf "Sccm package has been downloaded: %A. Notify requester." downloadedSccmPackage)
                    sender <! (CreateDriverPackageMessage.DownloadedSccmPackage downloadedSccmPackage)
                |DownloadMessage.Error ex ->
                    logger.Warn(sprintf "Download failed due to: %s" (getAccumulatedExceptionMessages ex))
                return! loop ()
            }
        loop()

    let packagingActor (mailbox:Actor<_>) =
        
        let rec loop () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (actorSystem, sender, self) = mailbox.Context.System,mailbox.Context.Sender, mailbox.Context.Self               
                match message with
                |PackagingMessage.DownloadedPackage downloadedPackage -> 
                    logger.Info(sprintf "TO BE IMPLEMENTED: Extracting package %A." downloadedPackage)
                    sender <! "TO BE IMPLEMENTED: Extracted package"
                |PackagingMessage.DownloadedSccmPackage downloadedSccmPackage -> 
                    logger.Info(sprintf "TO BE IMPLEMENTED: Extracting sccm package %A." downloadedSccmPackage)
                    sender <! "TO BE IMPLEMENTED: Extracted sccm package"
                return! loop ()
            }
        loop()