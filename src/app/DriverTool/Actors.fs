namespace DriverTool

module Actors =
        
    open Akka.FSharp
    open DriverTool.Library.Messages
    open DriverTool.Library.Logging
    let logger = getLoggerByName "DownloadActor"

    let downloadActor (mailbox:Actor<_>) =
        
        let rec loop () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (actorSystem, sender, self) = mailbox.Context.System,mailbox.Context.Sender, mailbox.Context.Self
                let downloadActorMessage = toDownloadMessage message
                match downloadActorMessage with
                |DownloadPackage package -> 
                    logger.Info(sprintf "TO BE IMPLEMENTED: Downloading package %A." package)
                    sender <! "TO BE IMPLEMENTED: Downloaded package"
                |DownloadSccmPackage sccmPackage -> 
                    logger.Info(sprintf "TO BE IMPLEMENTED: Downloading sccm package %A." sccmPackage)
                    sender <! "TO BE IMPLEMENTED: Downloaded sccm package"
                return! loop ()
            }
        loop()

    let packagingActor (mailbox:Actor<_>) =
        
        let rec loop () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (actorSystem, sender, self) = mailbox.Context.System,mailbox.Context.Sender, mailbox.Context.Self
                let downloadActorMessage = toDownloadedMessage message
                match downloadActorMessage with
                |DownloadedPackage downloadedPackage -> 
                    logger.Info(sprintf "TO BE IMPLEMENTED: Extracting package %A." downloadedPackage)
                    sender <! "TO BE IMPLEMENTED: Extracted package"
                |DownloadedSccmPackage downloadedSccmPackage -> 
                    logger.Info(sprintf "TO BE IMPLEMENTED: Extracting sccm package %A." downloadedSccmPackage)
                    sender <! "TO BE IMPLEMENTED: Extracted sccm package"
                return! loop ()
            }
        loop()