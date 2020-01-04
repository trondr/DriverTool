namespace DriverTool

module PackagingActor =
    
    open DriverTool.Library.Messages
    open DriverTool.Library.Logging
    open DriverTool.Library.F
    open DriverTool.Library.F0
    open DriverTool.Library.Retry
    open DriverTool.PackageTemplate
    open DriverTool.Actors
    open Akka.FSharp
    let logger = getLoggerByName "PackagingActor"
    
    let packagingActor parent (mailbox:Actor<_>) =
        
        let throwNotInitializedException actorMessage =            
            throwExceptionWithLogging logger (sprintf "Packaging actor has not been initialized. Cannot process message %A" actorMessage)

        let throwAllreadyInitializedException actorMessage =            
            throwExceptionWithLogging logger (sprintf "Packaging actor has allready been initialized. Cannot process message %A" actorMessage)

        let initializePackaging (packagingContext:PackagingContext) =
            match(result{
                logger.Info(msg (sprintf "Extracting package template to '%A'" packagingContext.PackageFolderPath))
                let! extractedPackagePaths = extractPackageTemplate packagingContext.PackageFolderPath
                logger.Info(msg (sprintf "Package template was extracted successfully from embedded resource. Number of files extracted: %i" (Seq.length extractedPackagePaths)))
                return extractedPackagePaths
            })with
            |Result.Error ex -> CreateDriverPackageMessage.Error ex
            |Result.Ok ex -> CreateDriverPackageMessage.Info (sprintf "Successfully extracted package template to '%A'" packagingContext)

        let movePackaging (source:PackagingContext) (destination:PackagingContext) =
            let mutable attempt = 0
            let move =
                retry{
                   attempt <- attempt+1
                   logger.Info(sprintf "Attempting to move %A -> %A. Attempt: %i of 10" source.PackageFolderPath destination.PackageFolderPath attempt)
                   return DriverTool.Library.DirectoryOperations.moveDirectoryUnsafe source.PackageFolderPath destination.PackageFolderPath                    
                }
            let result = (move,RetryPolicies.Retry(10, System.TimeSpan.FromSeconds(10.0))) ||> run
            match(result) with
            |RetryResult.RetryFailure ex -> CreateDriverPackageMessage.Error ex
            |RetryResult.RetrySuccess _ -> CreateDriverPackageMessage.PackagingMoved (source,destination)

        let rec initialize () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender, mailbox.Context.Self)
                match message with
                |InitializePackaging context ->
                    self <! (initializePackaging context)
                    return! create context                
                | _ ->                    
                    throwNotInitializedException message
                return! initialize ()
            }
        and create (packagingContext:PackagingContext) =
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = mailbox.Context.Sender, mailbox.Context.Self
                match message with
                |InitializePackaging dpcc ->
                    throwAllreadyInitializedException message
                |MovePackaging (source,destination) ->
                    logger.Info(sprintf "Moving packaging folder: %A -> %A" source.PackageFolderPath destination.PackageFolderPath)
                    self <! (movePackaging source destination)
                |PackagingMoved (sourceContext,destinationContext) ->
                    logger.Info(sprintf "Successfully moved packaging folder: %A -> %A" sourceContext.PackageFolderPath destinationContext.PackageFolderPath)
                    return! create destinationContext
                |ExtractPackage downloadedPackage -> 
                    logger.Info(sprintf "Extracting package: %A." downloadedPackage)
                    throwNotImplementedException logger message
                |ExtractSccmPackage downloadedSccmPackage -> 
                    logger.Info(sprintf "Extracting sccm package: %A." downloadedSccmPackage)
                    throwNotImplementedException logger message
                |CreateDriverPackageMessage.Error ex ->
                    parent <! CreateDriverPackageMessage.Error ex
                | _ ->
                    logger.Warn(sprintf "Message not handled by PackagingActor: %A" message)
                    return! create packagingContext
                return! create packagingContext
            }

        initialize ()

