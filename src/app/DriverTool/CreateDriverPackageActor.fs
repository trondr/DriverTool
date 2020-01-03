namespace DriverTool

module CreateDriverPackageActor =
    
    open System    
    open DriverTool.Library.Messages
    open DriverTool.Library.ManufacturerTypes
    open DriverTool.Library
    open DriverTool.Library.Logging
    open DriverTool.Library.PackageXml
    open DriverTool.Library.UpdatesContext
    open DriverTool.Actors
    open Akka.FSharp    
    open Akka.Actor
    open Akka.Routing
    let logger = getLoggerByName "CreateDriverPackageActor"
    
    // make a pipe-friendly version of Akka.NET PipeTo for handling async computations
    let pipeToWithSender recipient sender asyncComp = pipeTo asyncComp recipient sender

    type DriverPackageCreationContext =
        {
            PackagePublisher:string
            Manufacturer:Manufacturer
            SystemFamily:SystemFamily
            Model:ModelCode
            OperatingSystem:OperatingSystemCode
            DestinationFolderPath:FileSystem.Path
            CacheFolderPath:FileSystem.Path
            BaseOnLocallyInstalledUpdates:bool
            LogDirectory:FileSystem.Path
            ExcludeUpdateRegexPatterns: System.Text.RegularExpressions.Regex[]
            PackageTypeName:string
            ExcludeSccmPackage:bool            
            DoNotDownloadSccmPackage:bool
            SccmPackageInstaller:string
            SccmPackageReadme:string
            SccmPackageReleased:DateTime
        }

    type CreateDriverPackageMessage =
        |Initialize of DriverPackageCreationContext        
        |UpdatesRetrieved of PackageInfo array
        |DownloadPackage of PackageInfo
        |DownloadedPackage of DownloadedPackageInfo
        |DownloadSccmPackage of SccmPackageInfo
        |DownloadedSccmPackage of DownloadedSccmPackageInfo
        |Error of Exception
        |Finished        
    
    let toCreateDriverPackageMessage message =
        match(box message) with
        | :? DriverPackageCreationContext as context -> CreateDriverPackageMessage.Initialize context
        | :? (PackageInfo array) as packageInfos -> CreateDriverPackageMessage.UpdatesRetrieved packageInfos
        | :? PackageInfo as packageInfo -> CreateDriverPackageMessage.DownloadPackage packageInfo
        | :? DownloadedPackageInfo as downloadedPackageInfo -> CreateDriverPackageMessage.DownloadedPackage downloadedPackageInfo
        | :? SccmPackageInfo as sccmPackageInfo -> CreateDriverPackageMessage.DownloadSccmPackage sccmPackageInfo
        | :? DownloadedSccmPackageInfo as downloadedSccmPackageInfo -> CreateDriverPackageMessage.DownloadedSccmPackage downloadedSccmPackageInfo
        | :? Exception as ex -> CreateDriverPackageMessage.Error ex
        | _ -> failwith (sprintf "Unknown downloaded message: %s" (valueToString message))

    let getUniqueUpdatesByInstallerName packageInfos = 
        let uniqueUpdates = 
            packageInfos
            |> Array.groupBy (fun p -> p.Installer.Name)
            |> Array.map (fun (k,v) -> v |>Array.head)
        uniqueUpdates

    let retrieveUpdates dpcc =
        match(result{
            logger.Info("Getting update infos...")
            let getUpdates = DriverTool.Updates.getUpdatesFunc (logger, dpcc.Manufacturer,dpcc.BaseOnLocallyInstalledUpdates) 
            let updatesRetrievalContext = toUpdatesRetrievalContext dpcc.Model dpcc.OperatingSystem true dpcc.LogDirectory dpcc.ExcludeUpdateRegexPatterns                
            let! packageInfos = getUpdates dpcc.CacheFolderPath updatesRetrievalContext
            let uniquePackageInfos = packageInfos |> Array.distinct
            let uniqueUpdates = uniquePackageInfos |> getUniqueUpdatesByInstallerName
            return uniqueUpdates        
        }) with
        |Result.Error ex -> CreateDriverPackageMessage.Error ex
        |Ok updates -> CreateDriverPackageMessage.UpdatesRetrieved updates

    let retriveUpdatesAsync dpcc =
        System.Threading.Tasks.Task.Run(fun () -> retrieveUpdates dpcc)

    let createDriverPackageActor (clientActor:IActorRef) (mailbox:Actor<_>) =
        
        let downloadActor = spawnOpt mailbox.Context "worker" downloadActor [ SpawnOption.Router(SmallestMailboxPool(10)) ]        

        let throwExceptionWithLogging (logger:Common.Logging.ILog) (errorMessage:string) =
            logger.Error(errorMessage)
            failwith errorMessage

        let throwNotInitializedException actorMessage =            
            throwExceptionWithLogging logger (sprintf "Create Driver Package actor has not been initialized. Cannot process message %A" actorMessage)
            
        let throwAllreadyInitializedException actorMessage =            
            throwExceptionWithLogging logger (sprintf "Create Driver Package actor has allready been initialized. Cannot process message %A" actorMessage)

        let throwNotImplementedException actorMessage =            
            throwExceptionWithLogging logger (sprintf "Not implemented. Cannot process message %A" actorMessage) 
        
        let rec initializing () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender, mailbox.Context.Self)
                let createDriverPackageMessage = toCreateDriverPackageMessage message
                match createDriverPackageMessage with
                |Initialize dpcc -> 
                    logger.Info(sprintf "Retrieving update infos for context: %A...." dpcc)
                    (retriveUpdatesAsync dpcc)
                    |> Async.AwaitTask
                    |> pipeToWithSender self sender // send the message back to ourselves but pass the real sender through
                    return! creating()
                |UpdatesRetrieved _ ->
                    throwNotInitializedException createDriverPackageMessage
                |DownloadPackage _ ->
                    throwNotInitializedException createDriverPackageMessage
                |DownloadedPackage _ ->
                    throwNotInitializedException createDriverPackageMessage
                |DownloadSccmPackage _ ->
                    throwNotInitializedException createDriverPackageMessage
                |DownloadedSccmPackage _ ->
                    throwNotInitializedException createDriverPackageMessage
                |Finished ->
                    throwNotInitializedException createDriverPackageMessage
                |Error ex -> 
                    throwNotInitializedException createDriverPackageMessage                    
                return! initializing ()
            }

        and creating () =
            actor {
                let! message = mailbox.Receive()
                let (sender, self) = mailbox.Context.Sender, mailbox.Context.Self
                let createDriverPackageMessage = toCreateDriverPackageMessage message
                match createDriverPackageMessage with
                |Initialize _ -> 
                    throwAllreadyInitializedException createDriverPackageMessage 
                |UpdatesRetrieved packageInfos -> 
                    logger.Info(sprintf "Information about %i updates has been retrieved. Initiating download of each update...." (Array.length packageInfos) )                    
                    packageInfos
                    |> Array.map (fun p -> 
                                    let downloadPackageMessage = CreateDriverPackageMessage.DownloadPackage p
                                    self <! downloadPackageMessage
                                  )
                    |> ignore
                |DownloadPackage package ->
                    downloadActor <! DownloadMessage.DownloadPackage package
                |DownloadedPackage _ ->
                    throwNotImplementedException createDriverPackageMessage
                |DownloadSccmPackage sccmPackage ->
                    downloadActor <! DownloadMessage.DownloadSccmPackage sccmPackage
                |DownloadedSccmPackage _ ->
                    throwNotImplementedException createDriverPackageMessage
                |Finished ->
                    clientActor <! (new QuitHostMessage())
                    self <! (Akka.Actor.PoisonPill.Instance)
                |Error ex -> 
                    logger.Error(getAccumulatedExceptionMessages ex)
                    self <! (Finished)
                return! creating()
            }

        initializing()