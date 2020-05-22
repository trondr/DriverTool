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
        let extractActor = spawnOpt mailbox.Context "ExtractActor" (extractActor mailbox.Context.Self) [ SpawnOption.Router(SmallestMailboxPool(System.Environment.ProcessorCount)) ]
        let extractCoordinatorContext = {Index=10; Packages=[]}
        
        if(logger.IsDebugEnabled) then mailbox.Context.System.Scheduler.ScheduleTellRepeatedly(System.TimeSpan.FromSeconds(5.0),System.TimeSpan.FromSeconds(5.0),mailbox.Context.Self,(CreateDriverPackageMessage.Info "ExtractCoordinatorActor is alive"),mailbox.Context.Self)
    
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
                |PackageExtracted (extractedPackage,downloadedPackage) -> 
                    System.Console.WriteLine(sprintf "Package extracted (console): %s." downloadedPackage.Package.Installer.Name)
                    logger.Info( (sprintf "Package extracted: %s." downloadedPackage.Package.Installer.Name))
                    let updatedExtractCoordinatorContext = 
                        removePackageFromExtractCoordinatorContext extractCoordinatorContext downloadedPackage.Package
                    ownerActor <! PackageExtracted (extractedPackage,downloadedPackage)
                    return! loop updatedExtractCoordinatorContext
                |ExtractSccmPackage (packagingContext,dowloadedSccmPackage) ->
                    logger.Info( (sprintf "Extracting sccm package: %s." dowloadedSccmPackage.SccmPackage.InstallerFile.FileName))
                    extractActor<! ExtractSccmPackage (packagingContext, dowloadedSccmPackage)
                    return! loop extractCoordinatorContext
                |SccmPackageExtracted (extractedSccmPackageInfo,downloadedSccmPackageInfo) ->
                    match extractedSccmPackageInfo with
                    |Some esp ->
                        logger.Info( (sprintf "Sccm package extracted: %s." downloadedSccmPackageInfo.SccmPackage.InstallerFile.FileName))
                        ownerActor <! SccmPackageExtracted (extractedSccmPackageInfo,downloadedSccmPackageInfo)
                    |None -> 
                        logger.Warn(sprintf "Sccm package '%s' was not extracted." downloadedSccmPackageInfo.SccmPackage.InstallerFile.FileName)
                |CreateDriverPackageMessage.Info msg ->
                    logger.Info(msg)
                | _ ->
                    logger.Warn((sprintf "Message not handled by ExtractCoordinatorActor: %A" message))
                return! loop extractCoordinatorContext
            }
        loop extractCoordinatorContext
