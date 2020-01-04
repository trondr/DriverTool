namespace DriverTool

module CreateDriverPackageActor =
    
    open System    
    open DriverTool.Library.HostMessages
    open DriverTool.Library.Messages    
    open DriverTool.Library
    open DriverTool.Library.Logging
    open DriverTool.Library.PackageXml
    open DriverTool.Library.UpdatesContext
    open DriverTool.Actors
    open DriverTool.DownloadActor
    open DriverTool.PackagingActor
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

    let createDriverPackageActor (dpcc:DriverPackageCreationContext) (clientActor:IActorRef) (mailbox:Actor<_>) =
        
        let self = mailbox.Context.Self
        let downloadActor = spawnOpt mailbox.Context "DownloadActor" downloadActor [ SpawnOption.Router(SmallestMailboxPool(10)) ]
        let packagingActor = spawnOpt mailbox.Context "PackagingActor" (packagingActor self) [ SpawnOption.Router(SmallestMailboxPool(10)) ]

        let rec loop () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (system, sender, self) = (mailbox.Context.System, mailbox.Context.Sender, mailbox.Context.Self)                
                match message with
                |Start -> 
                    logger.Info(sprintf "Initialize packaging for context: %A...." dpcc)
                    let packagingContext = createPackagingContext dpcc.SccmPackageReleased dpcc
                    packagingActor <! (intializePackagingFromResult packagingContext)
                    
                    logger.Info(sprintf "Retrieving update infos for context: %A...." dpcc)
                    let updatesRetrievalContext = toUpdatesRetrievalContext dpcc.Manufacturer dpcc.Model dpcc.OperatingSystem true dpcc.LogDirectory dpcc.CacheFolderPath dpcc.BaseOnLocallyInstalledUpdates dpcc.ExcludeUpdateRegexPatterns
                    self <! CreateDriverPackageMessage.RetrieveUpdateInfos updatesRetrievalContext                    
                    
                    logger.Info(sprintf "Retrieving sccm package info for context: %A...." dpcc)
                    let sccmPackageInfoRetrievalContext = toSccmPackageInfoRetrievalContext dpcc.Manufacturer dpcc.Model dpcc.OperatingSystem dpcc.CacheFolderPath dpcc.DoNotDownloadSccmPackage dpcc.SccmPackageInstaller dpcc.SccmPackageReadme dpcc.SccmPackageReleased
                    self <! (CreateDriverPackageMessage.RetrieveSccmPackageInfo sccmPackageInfoRetrievalContext)

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
                |DownloadedPackage downloadedPackage ->
                    logger.Info(sprintf "Request extraction of downloaded package: %A." downloadedPackage)
                    packagingActor <! ExtractPackage downloadedPackage
                |DownloadSccmPackage sccmPackageDownloadContext ->
                    logger.Info(sprintf "Request download of sccm package: %A." sccmPackageDownloadContext)
                    downloadActor <! DownloadSccmPackage sccmPackageDownloadContext
                |DownloadedSccmPackage downloadedSccmPackage ->
                    logger.Info(sprintf "Request extraction of downloaded sccm package: %A." downloadedSccmPackage)
                    packagingActor <! ExtractSccmPackage downloadedSccmPackage
                //|PackageExtracted extractedPackageInfo  ->
                //    throwNotImplementedException logger message
                //|SccmPackageExtracted extractedSccmPackageInfo  ->
                //    throwNotImplementedException logger message
                |Finished ->
                    clientActor <! (new QuitHostMessage())
                    self <! (Akka.Actor.PoisonPill.Instance)
                    system.Terminate() |> ignore
                |CreateDriverPackageMessage.Error ex ->                     
                    logger.Error(getAccumulatedExceptionMessages ex)
                    logger.Error("Fatal error occured. Terminating application.")
                    self <! (Finished)
                | _ ->
                    logger.Warn(sprintf "Message not handled by CreateDriverPackageActor: %A" message)
                    return! loop()
                return! loop ()
            }

        loop ()