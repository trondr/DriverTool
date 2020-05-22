namespace DriverTool

module ExtractCoordinatorActor =
    open Akka.Actor
    open Akka.FSharp
    open Akka.Routing
    open DriverTool.Library.Messages
    open DriverTool.Library.PackageXml    
    open DriverTool.ExtractActor
    open DriverTool.Library.Logging
    let logger = getLoggerByName "ExtractCoordinatorActor"

    type ExtractCoordinatorContext =
        {
            Index : int;
            Packages: PackageInfo list
        }

    let updateExtractCoordinatorContext (extractCoordinatorContext:ExtractCoordinatorContext) (package:PackageInfo) index =
        let packageExists = List.exists (fun p -> p = package) extractCoordinatorContext.Packages
        if( not packageExists) then
            {extractCoordinatorContext with Packages=(extractCoordinatorContext.Packages @ [package]);Index=index}
        else
            extractCoordinatorContext

    let removePackageFromExtractCoordinatorContext (extractCoordinatorContext:ExtractCoordinatorContext) (package:PackageInfo) =
        let packageExists = List.exists (fun p -> p = package) extractCoordinatorContext.Packages
        if(packageExists) then            
            let updatedExtractCoordinatorContext = {extractCoordinatorContext with Packages=List.filter (fun p-> p <> package) extractCoordinatorContext.Packages}
            if(logger.IsDebugEnabled) then logger.Debug(sprintf "Extract coordinator context: %A" updatedExtractCoordinatorContext )
            updatedExtractCoordinatorContext
        else
            extractCoordinatorContext

    let extractCoordinatorActor (ownerActor:IActorRef) (mailbox:Actor<_>) =        
        let extractActor = spawnOpt mailbox.Context "ExtractActor" extractActor [ SpawnOption.Router(SmallestMailboxPool(10)) ]
        let extractCoordinatorContext = {Index=10; Packages=[]}
    
        let rec loop (extractCoordinatorContext:ExtractCoordinatorContext) =
            actor {
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender,mailbox.Context.Self)
                match message with
                |ExtractPackage (packagingContext,downloadedPackage) ->
                    logger.Info( (sprintf "Extracting package: %s." downloadedPackage.Package.Installer.Name))
                    let updatedPackagingContext = {packagingContext with ExtractFolderPrefix = extractCoordinatorContext.Index}
                    let updatedExtractCoordinatorContext = 
                        updateExtractCoordinatorContext extractCoordinatorContext downloadedPackage.Package (extractCoordinatorContext.Index + 10)
                    extractActor<! ExtractPackage (updatedPackagingContext, downloadedPackage)                    
                    return! loop updatedExtractCoordinatorContext
                |PackageExtracted extractedPackage -> 
                    logger.Info( (sprintf "Package extracted: %s." extractedPackage.DownloadedPackage.Package.Installer.Name))
                    let updatedExtractCoordinatorContext = 
                        removePackageFromExtractCoordinatorContext extractCoordinatorContext extractedPackage.DownloadedPackage.Package
                    ownerActor <! PackageExtracted extractedPackage
                    return! loop updatedExtractCoordinatorContext
                | _ ->
                    logger.Warn((sprintf "Message not handled by ExtractCoordinatorActor: %A" message))
                    return! loop extractCoordinatorContext
            }
        loop extractCoordinatorContext
