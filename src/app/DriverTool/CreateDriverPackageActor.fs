namespace DriverTool

module CreateDriverPackageActor =
    
    open System    
    open DriverTool.Library.F0
    open DriverTool.Library.F
    open DriverTool.Library.HostMessages
    open DriverTool.Library.Messages    
    open DriverTool.Library
    open DriverTool.Library.Logging
    open DriverTool.Library.PackageXml
    open DriverTool.Library.UpdatesContext
    open DriverTool.Actors
    open DriverTool.DownloadActor
    open DriverTool.ExtractActor
    open DriverTool.PackageTemplate
    open Akka.FSharp    
    open Akka.Actor
    open Akka.Routing
    let logger = getLoggerByName "CreateDriverPackageActor"
    
    let getUniqueUpdatesByInstallerName packageInfos = 
        let uniqueUpdates = 
            packageInfos
            |> Array.groupBy (fun p -> p.Installer.Name)
            |> Array.map (fun (k,v) -> v |>Array.head)
        uniqueUpdates

    let retrieveUpdates (urc:UpdatesRetrievalContext) =
        match(result{
            logger.Info("Getting update infos...")
            let getUpdates = DriverTool.Updates.getUpdatesFunc (logger, urc.Manufacturer,urc.BaseOnLocallyInstalledUpdates)             
            let! packageInfos = getUpdates urc.CacheFolderPath urc
            let uniquePackageInfos = packageInfos |> Array.distinct
            let uniqueUpdates = uniquePackageInfos |> getUniqueUpdatesByInstallerName
            return uniqueUpdates        
        }) with
        |Result.Error ex -> CreateDriverPackageMessage.Error ex
        |Ok updates -> CreateDriverPackageMessage.UpdateInfosRetrieved updates

    let retriveUpdatesAsync dpcc =
        System.Threading.Tasks.Task.Run(fun () -> retrieveUpdates dpcc)
        |> Async.AwaitTask

    let downloadPackages packageInfos self =
        packageInfos
        |> Array.map (fun p -> 
                        let downloadPackageMessage = CreateDriverPackageMessage.DownloadPackage p
                        self <! downloadPackageMessage
                      )
        |> ignore

    let toDownloadedSccmPackageInfo cacheFolderPath intallerName readmeName releasedDate =
        result{
            let! installerPath = PathOperations.combinePaths2 cacheFolderPath intallerName
            let! existingInstallerPath = FileOperations.ensureFileExists installerPath
            let! readmePath = PathOperations.combinePaths2 cacheFolderPath readmeName
            let! existingReadmePath = FileOperations.ensureFileExists readmePath
            let downloadedSccmPackageInfo =
                {
                    InstallerPath=FileSystem.pathValue existingInstallerPath; 
                    ReadmePath=FileSystem.pathValue existingReadmePath;                     
                    SccmPackage=
                        {
                            ReadmeFile=
                                {
                                    Url=String.Empty;
                                    Checksum=String.Empty; 
                                    FileName=readmeName;
                                    Size=0L
                                }
                            InstallerFile=
                                {
                                    Url=String.Empty;
                                    Checksum=String.Empty;
                                    FileName=intallerName;
                                    Size=0L
                                }
                            Released=releasedDate;
                            Os=String.Empty;
                            OsBuild=String.Empty;
                        }                
                }
            return downloadedSccmPackageInfo
        }

    let retrieveSccmPackageInfo context =
        if(not context.DoNotDownloadSccmPackage) then
            match(result{
                logger.Info("Getting SCCM package info...")
                let getSccmPackage = DriverTool.Updates.getSccmPackageFunc context.Manufacturer                
                let! sccmPackage = getSccmPackage (context.Model,context.OperatingSystem,context.CacheFolderPath)
                logger.Info(msg (sprintf "Sccm packge: %A" sccmPackage))
                return sccmPackage
            })with
            |Result.Error ex -> CreateDriverPackageMessage.Error ex
            |Ok sccmPackage -> CreateDriverPackageMessage.SccmPackageInfoRetrieved sccmPackage
        else
            match(result{
                logger.Info("Attempting to use manually downloaded sccm package...")
                let! downloadedScmPackageInfo = toDownloadedSccmPackageInfo context.CacheFolderPath context.SccmPackageInstaller context.SccmPackageReadme context.SccmPackageReleased
                return downloadedScmPackageInfo            
            }) with
            |Result.Error ex -> CreateDriverPackageMessage.Error ex
            |Ok downloadedSccmPackageInfo -> CreateDriverPackageMessage.DownloadedSccmPackage downloadedSccmPackageInfo

    let retrieveSccmPackageInfoAsync context =
        System.Threading.Tasks.Task.Run(fun () -> retrieveSccmPackageInfo context)
        |> Async.AwaitTask

    let intializePackagingFromResult result =
        match result with
        |Result.Error ex -> CreateDriverPackageMessage.Error ex
        |Result.Ok context -> CreateDriverPackageMessage.InitializePackaging context

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

    let isProgressFinished progress =
        let percentageProgress = (getPercent progress)        
        (percentageProgress - 100.0) < 0.001
        
    let isPackagingFinished packagingContext =
        match packagingContext.Started with
        |true ->
            let isFinished =
                imperative{
                    if(isProgressFinished packagingContext.PackageDownloads) then return false
                    if(isProgressFinished packagingContext.SccmPackageDownloads) then return false
                    if(isProgressFinished packagingContext.PackageExtracts) then return false
                    if(isProgressFinished packagingContext.SccmPackageExtracts) then return false
                    return true
                }
            isFinished
        |false -> false

    let toPackagingProgressString packagingContext =
        let packageDownloadsProgress = (toProgressMessage packagingContext.PackageDownloads)
        let sccmPackageDownloadsProgress = (toProgressMessage packagingContext.SccmPackageDownloads)
        let packageExtractsProgress = (toProgressMessage packagingContext.SccmPackageExtracts)
        let sccmPackageExtractsProgress = (toProgressMessage packagingContext.SccmPackageExtracts)
        sprintf "Progress: %s, %s, %s, %s" packageDownloadsProgress sccmPackageDownloadsProgress packageExtractsProgress sccmPackageExtractsProgress

    let reportProgress packagingContext =
        match (isPackagingFinished packagingContext) with
        |true->
            CreateDriverPackageMessage.FinalizePackaging packagingContext
        |false ->
            CreateDriverPackageMessage.Info (toPackagingProgressString packagingContext)

    let createDriverPackageActor (dpcc:DriverPackageCreationContext) (clientActor:IActorRef) (mailbox:Actor<_>) =
        
        let self = mailbox.Context.Self
        let downloadActor = spawnOpt mailbox.Context "DownloadActor" downloadActor [ SpawnOption.Router(SmallestMailboxPool(10)) ]
        let packagingActor = spawnOpt mailbox.Context "ExtractActor" extractActor [ SpawnOption.Router(SmallestMailboxPool(10)) ]
        
        let rec initialize () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender, mailbox.Context.Self)
                match message with
                |Start -> 
                    logger.Info(sprintf "Initialize packaging for context: %A...." dpcc)
                    let packagingContext = createPackagingContext dpcc.SccmPackageReleased dpcc
                    self <! (intializePackagingFromResult packagingContext)
                    
                    logger.Info(sprintf "Retrieving update infos for context: %A...." dpcc)
                    let updatesRetrievalContext = toUpdatesRetrievalContext dpcc.Manufacturer dpcc.Model dpcc.OperatingSystem true dpcc.LogDirectory dpcc.CacheFolderPath dpcc.BaseOnLocallyInstalledUpdates dpcc.ExcludeUpdateRegexPatterns
                    self <! CreateDriverPackageMessage.RetrieveUpdateInfos updatesRetrievalContext                    
                    
                    logger.Info(sprintf "Retrieving sccm package info for context: %A...." dpcc)
                    let sccmPackageInfoRetrievalContext = toSccmPackageInfoRetrievalContext dpcc.Manufacturer dpcc.Model dpcc.OperatingSystem dpcc.CacheFolderPath dpcc.DoNotDownloadSccmPackage dpcc.SccmPackageInstaller dpcc.SccmPackageReadme dpcc.SccmPackageReleased
                    self <! (CreateDriverPackageMessage.RetrieveSccmPackageInfo sccmPackageInfoRetrievalContext)
                |InitializePackaging context ->
                    logger.Info(sprintf "Initialize packaging for packaging context: %A...." context)
                    self <! (initializePackaging context)
                    return! create context
                | _ ->                    
                    throwNotInitializedException message
                return! initialize ()
            }
        and create (packagingContext:PackagingContext) =
            actor {                                                
                let! message = mailbox.Receive()
                let (system, sender, self) = (mailbox.Context.System, mailbox.Context.Sender, mailbox.Context.Self)
                match message with                
                |Start ->
                    throwAllreadyInitializedException message
                |InitializePackaging _ ->
                    throwAllreadyInitializedException message
                |RetrieveUpdateInfos updatesRetrievalContext ->
                    logger.Info(sprintf "Retrieving update infos for context: %A...." updatesRetrievalContext)
                    (retriveUpdatesAsync updatesRetrievalContext)                    
                    |> pipeToWithSender self sender
                |UpdateInfosRetrieved packageInfos ->
                    logger.Info(sprintf "Information about %i updates has been retrieved. Initiating download of each update...." (Array.length packageInfos) )                    
                    downloadPackages packageInfos self
                |RetrieveSccmPackageInfo sccmPackageInfoRetrievalContext ->
                    logger.Info(sprintf "Retrieving sccm package info for context: %A...." sccmPackageInfoRetrievalContext)
                    (retrieveSccmPackageInfoAsync sccmPackageInfoRetrievalContext)                    
                    |> pipeToWithSender self sender
                |SccmPackageInfoRetrieved sccmPackageInfo ->
                    logger.Info(sprintf "Sccm package info has been retrived: %A. Start download." sccmPackageInfo)
                    let sccmPackageDownloadContext = toSccmPackageInfoDownloadContext dpcc.Manufacturer dpcc.CacheFolderPath sccmPackageInfo
                    downloadActor <! DownloadSccmPackage sccmPackageDownloadContext
                |DownloadPackage package ->
                    logger.Info(sprintf "Request download of package: %A." package)
                    downloadActor <! DownloadPackage package         
                    let updatedPackagingContext = startPackageDownload packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext
                |DownloadedPackage downloadedPackage ->
                    logger.Info(sprintf "Request extraction of downloaded package: %A." downloadedPackage)
                    packagingActor <! ExtractPackage downloadedPackage
                    let updatedPackagingContext = donePackageDownload packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext
                |DownloadSccmPackage sccmPackageDownloadContext ->
                    logger.Info(sprintf "Request download of sccm package: %A." sccmPackageDownloadContext)
                    downloadActor <! DownloadSccmPackage sccmPackageDownloadContext
                    let updatedPackagingContext = startSccmPackageDownload packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext
                |DownloadedSccmPackage downloadedSccmPackage ->
                    logger.Info(sprintf "Request extraction of downloaded sccm package: %A." downloadedSccmPackage)
                    packagingActor <! ExtractSccmPackage downloadedSccmPackage
                    let updatedPackagingContext = doneSccmPackageDownload packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext                
                |ExtractPackage downloadedPackage -> 
                    logger.Info(sprintf "Extracting package: %A." downloadedPackage)
                    throwNotImplementedException logger message
                    let updatedPackagingContext = donePackageExtract packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create (startPackageExtract packagingContext)
                |PackageExtracted extractedPackage -> 
                    logger.Info(sprintf "Package extracted: %A." extractedPackage)
                    throwNotImplementedException logger message
                    let updatedPackagingContext = donePackageExtract packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext
                |ExtractSccmPackage downloadedSccmPackage -> 
                    logger.Info(sprintf "Extracting sccm package: %A." downloadedSccmPackage)
                    throwNotImplementedException logger message
                    let updatedPackagingContext = startSccmPackageExtract packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext
                |SccmPackageExtracted extractedSccmPackage -> 
                    logger.Info(sprintf "Sccm package extracted: %A." extractedSccmPackage)
                    throwNotImplementedException logger message
                    let updatedPackagingContext = doneSccmPackageExtract packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext
                |PackagingProgress packagingContext ->
                    self <! (reportProgress packagingContext)                    
                |FinalizePackaging packagingContext ->
                    logger.Info(sprintf "Finalize packaging for context: %A." packagingContext)
                    match(result{
                        let! destinationContext = updatePackagingContext packagingContext packagingContext.ReleaseDate dpcc                        
                        return destinationContext
                    })with
                    |Result.Error ex -> 
                        self <! CreateDriverPackageMessage.Error ex
                    |Result.Ok updatedPackagingContext -> 
                        self <! (movePackaging packagingContext updatedPackagingContext)
                        return! create updatedPackagingContext
                |PackagingFinalized (sourceContext,destinationContext) ->
                    logger.Info(sprintf "Successfully finalized packaging: %A -> %A" sourceContext.PackageFolderPath destinationContext.PackageFolderPath)
                    logger.Info(sprintf "Requesting shutdown...")
                    self <! Finished
                    return! create destinationContext
                |Finished ->
                    logger.Info(sprintf "Shuting down...")
                    clientActor <! (new QuitHostMessage())
                    self <! (Akka.Actor.PoisonPill.Instance)
                    system.Terminate() |> ignore
                |CreateDriverPackageMessage.Info info ->
                    logger.Info(info)
                |CreateDriverPackageMessage.Error ex ->                     
                    logger.Error(getAccumulatedExceptionMessages ex)
                    logger.Error("Fatal error occured. Terminating application.")
                    self <! (Finished)
                //| _ ->
                //    logger.Warn(sprintf "Message not handled by CreateDriverPackageActor: %A" message)                    
                //    return! create packagingContext
                return! create packagingContext
            }

        initialize ()
        