namespace DriverTool

module ExtractCoordinatorActor =
    open Akka.Actor
    open Akka.FSharp
    open Akka.FSharp.Actors
    open Akka.Routing
    open DriverTool.Library.Messages
    open DriverTool.Library.Logging
    open DriverTool.ExtractActor
    let logger = getLoggerByName "ExtractCoordinatorActor"

    let extractCoordinatorActor (ownerActor:IActorRef) (mailbox:Actor<_>) =

        let extractActor = spawnOpt mailbox.Context "ExtractActor" extractActor [ SpawnOption.Router(SmallestMailboxPool(10)) ]
        let extractCoordinatorContext = 10
    
        let rec loop extractCoordinatorContext =
            actor {
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender,mailbox.Context.Self)
                match message with
                |ExtractPackage (packagingContext,downloadedPackage) ->
                    let updatedPackagingContext = {packagingContext with ExtractFolderPrefix = extractCoordinatorContext}
                    let updatedExtractCoordinatorContext = extractCoordinatorContext + 10
                    extractActor<! ExtractPackage (updatedPackagingContext, downloadedPackage)                    
                    return! loop updatedExtractCoordinatorContext
                |PackageExtracted extractedPackage -> 
                    logger.Info(sprintf "Package extracted: %A." extractedPackage)
                    ownerActor <! PackageExtracted extractedPackage
                    return! loop extractCoordinatorContext
                | _ ->
                    logger.Warn(sprintf "Message not handled by DownloadActorCoordinator: %A" message)
                    return! loop extractCoordinatorContext
            }
        loop extractCoordinatorContext
