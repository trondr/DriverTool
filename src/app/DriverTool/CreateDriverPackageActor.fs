namespace DriverTool

module CreateDriverPackageActor =
    
    open System    

    open DriverTool.Library.Environment
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
    open DriverTool.Library.PackageDefinition
    open DriverTool.Packaging
    open Akka.FSharp    
    open Akka.Actor
    open Akka.Routing
    let logger = getLoggerByName "CreateDriverPackageActor"
    
    let toDriverPackageCreationContext packagePublisher manufacturer systemFamily modelCode operatingSystemCode destinationFolderPath cacheFolderPath baseOnLocallyInstalledUpdates logDirectory excludeUpdateRegexPatterns packageTypeName excludeSccmPackage doNotDownloadSccmPackage sccmPackageInstaller sccmPackageReadme sccmPackageReleased =
        {
            PackagePublisher=packagePublisher
            Manufacturer=manufacturer
            SystemFamily=systemFamily
            Model=modelCode
            OperatingSystem=operatingSystemCode
            DestinationFolderPath=destinationFolderPath
            CacheFolderPath=cacheFolderPath
            BaseOnLocallyInstalledUpdates=baseOnLocallyInstalledUpdates
            LogDirectory=logDirectory
            ExcludeUpdateRegexPatterns=excludeUpdateRegexPatterns
            PackageTypeName=packageTypeName
            ExcludeSccmPackage=excludeSccmPackage
            DoNotDownloadSccmPackage = doNotDownloadSccmPackage
            SccmPackageInstaller = sccmPackageInstaller
            SccmPackageReadme = sccmPackageReadme
            SccmPackageReleased = sccmPackageReleased
        }

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

    let downloadPackages packageInfos packagingContext self =
        packageInfos        
        |> Array.map (fun p -> 
                        let downloadPackageMessage = CreateDriverPackageMessage.DownloadPackage (p,packagingContext)
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
            |Ok downloadedSccmPackageInfo -> CreateDriverPackageMessage.SccmPackageDownloaded downloadedSccmPackageInfo

    open System.Threading
    open System.Threading.Tasks

    let startSTATask<'t> func =
        let tcs = new TaskCompletionSource<'t>()
        let thread = new System.Threading.Thread(
                        fun () -> 
                            try
                                tcs.SetResult(func())
                            with
                            |ex ->
                                tcs.SetException(ex)        
                        )
        thread.SetApartmentState(ApartmentState.STA)
        thread.Start()
        tcs.Task        
    
    let retrieveSccmPackageInfoAsync context =        
        (startSTATask (fun () -> retrieveSccmPackageInfo context))
        |> Async.AwaitTask

    let requestIntializationOfPackaging result =
        match result with
        |Result.Error ex -> CreateDriverPackageMessage.Error ex
        |Result.Ok context -> CreateDriverPackageMessage.InitializePackaging context

    let throwNotInitializedException actorMessage =            
        throwExceptionWithLogging logger (sprintf "Packaging actor has not been initialized. Cannot process message: %A" actorMessage)

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
        |RetryResult.RetrySuccess _ -> CreateDriverPackageMessage.PackagagingMoved

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

    let checkAndReportProgress packagingContext =
        match (isPackagingFinished packagingContext) with
        |true->
            CreateDriverPackageMessage.FinalizePackaging packagingContext
        |false ->
            CreateDriverPackageMessage.Info (toPackagingProgressString packagingContext)

    let updateInstallXml (packagingContext:PackagingContext) =
        match(result{
            let! installXmlPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue packagingContext.PackageFolderPath,"Install.xml"))
            let! existingInstallXmlPath = FileOperations.ensureFileExists installXmlPath
            let! installConfiguration = DriverTool.Library.InstallXml.loadInstallXml existingInstallXmlPath
            
            let updatedInstallConfiguration = 
                { installConfiguration with 
                    LogDirectory = (unExpandEnironmentVariables (FileSystem.pathValue packagingContext.LogDirectory));
                    LogFileName = toValidDirectoryName (sprintf "%s.log" packagingContext.PackageName);
                    PackageName = packagingContext.PackageName;
                    PackageVersion = "1.0"
                    PackageRevision = "000"
                    ComputerModel = packagingContext.Model.Value;
                    ComputerSystemFamiliy = packagingContext.SystemFamily.Value;
                    ComputerVendor = DriverTool.Library.ManufacturerTypes.manufacturerToName packagingContext.Manufacturer;
                    OsShortName = packagingContext.OperatingSystem.Value;
                    Publisher = packagingContext.PackagePublisher
                }
            let! savedInstallConfiguration = DriverTool.Library.InstallXml.saveInstallXml (existingInstallXmlPath, updatedInstallConfiguration)
            logger.Info(msg (sprintf  "Saved install configuration to '%s'. Value: %A" (FileSystem.pathValue existingInstallXmlPath) savedInstallConfiguration))
            return savedInstallConfiguration
        })with
        |Result.Error ex -> CreateDriverPackageMessage.Error ex
        |Result.Ok installConfiguration -> CreateDriverPackageMessage.Info (sprintf "Successfully updated and saved install.xml '%A'" installConfiguration)


    let createPackageDefinitionSms packagingContext installConfiguration =
        match(result{
            logger.Info("Create PackageDefinition.sms")            
            let! packageDefinitionSmsPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue packagingContext.PackageFolderPath,"PackageDefinition.sms"))                
            let packageDefinition = getPackageDefinitionFromInstallConfiguration installConfiguration
            let! packageDefintionWriteResult = 
                packageDefinition
                |> writePackageDefinitionToFile packageDefinitionSmsPath
            logger.Info(sprintf "Created PackageDefinition.sms: %A" packageDefintionWriteResult)
            return packageDefinition
        })with
        |Result.Error ex -> CreateDriverPackageMessage.Error ex
        |Result.Ok packageDefinition -> CreateDriverPackageMessage.PackageDefinitionCreated (packageDefinition,installConfiguration)

    let createPackageDefinitionDismSms packagingContext installConfiguration =
        match(result{
            logger.Info("Create PackageDefinition-DISM.sms")            
            let packageDefinition = getPackageDefinitionFromInstallConfiguration installConfiguration
            let! packageDefinitionDimsSmsPath = FileSystem.path (System.IO.Path.Combine(FileSystem.pathValue packagingContext.PackageFolderPath,"PackageDefinition-DISM.sms"))
            let sccmPackageFolderName = (sccmPackageFolderName packagingContext.SccmReleaseDate )
            let packageDefintionDism = {packageDefinition with PackageDefinition.InstallCommandLine = "DISM.exe /Image:%OSDisk%\\ /Add-Driver /Driver:.\\Drivers\\" + sccmPackageFolderName + "\\ /Recurse"; }
            let! dismPackageDefinitionFilePath = 
                packageDefintionDism
                |> writePackageDefinitionDismToFile packageDefinitionDimsSmsPath                                    
            logger.Info(sprintf "Created PackageDefinition-DISM.sms: %A" dismPackageDefinitionFilePath)
            return dismPackageDefinitionFilePath
        })with
        |Result.Error ex -> CreateDriverPackageMessage.Error ex
        |Result.Ok dismPackageDefinitionFilePath -> CreateDriverPackageMessage.DismPackageDefinitionCreated dismPackageDefinitionFilePath

    let updatePackagingContext packagingContext dpcc =
        match(result{
            let! updatedPackagingContext = DriverTool.Library.Messages.updatePackagingContext packagingContext packagingContext.ReleaseDate dpcc                        
            return updatedPackagingContext
        })with
        |Result.Error ex -> CreateDriverPackageMessage.Error ex
        |Result.Ok updatedPackagingContext -> CreateDriverPackageMessage.PackagingContextUpdated (packagingContext,updatedPackagingContext)

    let assertDriverPackageCreateRequirements () =
        match(result{
                let! isAdministrator = DriverTool.Library.Requirements.assertIsAdministrator "Administrative privileges are required. Please run driver package create from an elevated command prompt."
                logger.Info(msg (sprintf "Installation is running with admin privileges: %b" isAdministrator))
                return isAdministrator
        })with
        |Result.Error ex -> CreateDriverPackageMessage.Error ex
        |Result.Ok isAdministrator -> CreateDriverPackageMessage.Info (sprintf "Installation is running with admin privileges: %b" isAdministrator)

    let createDriverPackageActor (dpcc:DriverPackageCreationContext) (clientActor:IActorRef) (mailbox:Actor<_>) =
        
        let self = mailbox.Context.Self        
        let downloadCoordinatorActor = spawn mailbox.Context.System "DownloadCoordinatorActor" (DownloadCoordinatorActor.downloadCoordinatorActor self)
        let extractCoordinatorActor = spawn mailbox.Context.System "ExtractCoordinatorActor" (ExtractCoordinatorActor.extractCoordinatorActor self)

        let rec initialize () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (system, sender, self) = (mailbox.Context.System, mailbox.Context.Sender, mailbox.Context.Self)
                match message with
                |Start -> 
                    logger.Info("Checking requirements")
                    self <! assertDriverPackageCreateRequirements()

                    logger.Info(sprintf "Request initialization of packaging for context: %A...." dpcc)
                    let packagingContext = createPackagingContext dpcc.SccmPackageReleased dpcc
                    self <! (requestIntializationOfPackaging packagingContext)
                    
                    logger.Info(sprintf "Request retrieval of update infos for context: %A...." dpcc)
                    let updatesRetrievalContext = toUpdatesRetrievalContext dpcc.Manufacturer dpcc.Model dpcc.OperatingSystem true dpcc.LogDirectory dpcc.CacheFolderPath dpcc.BaseOnLocallyInstalledUpdates dpcc.ExcludeUpdateRegexPatterns
                    self <! CreateDriverPackageMessage.RetrieveUpdateInfos updatesRetrievalContext                    
                    
                    logger.Info(sprintf "Request retrieval of sccm package info for context: %A...." dpcc)
                    let sccmPackageInfoRetrievalContext = toSccmPackageInfoRetrievalContext dpcc.Manufacturer dpcc.Model dpcc.OperatingSystem dpcc.CacheFolderPath dpcc.DoNotDownloadSccmPackage dpcc.SccmPackageInstaller dpcc.SccmPackageReadme dpcc.SccmPackageReleased
                    self <! (CreateDriverPackageMessage.RetrieveSccmPackageInfo sccmPackageInfoRetrievalContext)
                |InitializePackaging context ->
                    logger.Info(sprintf "Initialize packaging for packaging context: %A...." context)
                    self <! (initializePackaging context)
                    return! create context
                |Finished ->
                    logger.Info(sprintf "Shuting down...")
                    clientActor <! (new QuitHostMessage())
                    self <! (Akka.Actor.PoisonPill.Instance)
                    system.Terminate() |> ignore
                |CreateDriverPackageMessage.Info info ->
                    logger.Info(info)
                |CreateDriverPackageMessage.Warning info ->
                    logger.Warn(info)
                |CreateDriverPackageMessage.Error ex ->                     
                    logger.Error(getAccumulatedExceptionMessages ex)
                    logger.Error("Fatal error occured. Terminating application.")
                    self <! (Finished)                
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
                    downloadPackages packageInfos packagingContext self
                |RetrieveSccmPackageInfo sccmPackageInfoRetrievalContext ->
                    logger.Info(sprintf "Retrieving sccm package info for context: %A...." sccmPackageInfoRetrievalContext)
                    (retrieveSccmPackageInfoAsync sccmPackageInfoRetrievalContext)                    
                    |> pipeToWithSender self sender
                |SccmPackageInfoRetrieved sccmPackageInfo ->
                    logger.Info(sprintf "Sccm package info has been retrived: %A. Request download." sccmPackageInfo)
                    let sccmPackageDownloadContext = toSccmPackageInfoDownloadContext dpcc.Manufacturer dpcc.CacheFolderPath sccmPackageInfo
                    self <! DownloadSccmPackage sccmPackageDownloadContext
                |DownloadPackage (package,packagingContext) ->
                    logger.Info(sprintf "Request download of package: %A." package)
                    downloadCoordinatorActor <! DownloadPackage (package,packagingContext)
                    let updatedPackagingContext = startPackageDownload packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext
                |PackageDownloaded downloadedPackage ->
                    logger.Info(sprintf "Package has been downloaded: %A. Request extraction..." downloadedPackage)
                    let updatedPackagingContext = donePackageDownload packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    match downloadedPackage with
                    |Some dl ->
                        self <! ExtractPackage (updatedPackagingContext, dl)                                            
                    |None -> ()                    
                    return! create updatedPackagingContext
                |DownloadSccmPackage sccmPackageDownloadContext ->
                    logger.Info(sprintf "Request download of sccm package: %A." sccmPackageDownloadContext)
                    downloadCoordinatorActor <! DownloadSccmPackage sccmPackageDownloadContext
                    let updatedPackagingContext = startSccmPackageDownload packagingContext sccmPackageDownloadContext.SccmPackage.Released
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext
                |SccmPackageDownloaded downloadedSccmPackage ->
                    logger.Info(sprintf "Sccm package has been downloaded: %A. Request extraction..." downloadedSccmPackage)
                    self <! ExtractSccmPackage (packagingContext,downloadedSccmPackage)
                    let updatedPackagingContext = doneSccmPackageDownload packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext                
                |StartDownload webFileDownload ->
                    logger.Error(sprintf "Message not supported %A" message)
                |DownloadFinished webFileDownload -> 
                    logger.Error(sprintf "Message not supported %A" message)
                |ExtractPackage (packagingContext,downloadedPackage) -> 
                    logger.Info(sprintf "Request extract of package: %A." downloadedPackage)
                    extractCoordinatorActor <! ExtractPackage  (packagingContext,downloadedPackage)
                    let updatedPackagingContext = startPackageExtract packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext
                |PackageExtracted extractedPackage -> 
                    logger.Info(sprintf "Package extracted: %A." extractedPackage)                    
                    let updatedPackagingContext = donePackageExtract packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext
                |ExtractSccmPackage (packagingContext,downloadedSccmPackage) ->                     
                    let updatedPackagingContext =
                        if (not dpcc.ExcludeSccmPackage) then
                            logger.Info(sprintf "Request extract of sccm package: %A." downloadedSccmPackage)
                            extractCoordinatorActor <! ExtractSccmPackage (packagingContext,downloadedSccmPackage)
                            startSccmPackageExtract packagingContext
                        else
                            logger.Info(sprintf "Skipping extract of sccm package: %A." downloadedSccmPackage)
                            packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext
                |SccmPackageExtracted extractedSccmPackage -> 
                    logger.Info(sprintf "Sccm package has been extracted: %A." extractedSccmPackage)                    
                    let updatedPackagingContext = doneSccmPackageExtract packagingContext
                    self <! PackagingProgress updatedPackagingContext
                    return! create updatedPackagingContext                   
                |PackagingProgress packagingContext ->
                    self <! (checkAndReportProgress packagingContext)                    
                |FinalizePackaging packagingContext ->
                    logger.Info(sprintf "Finalize packaging for context: %A." packagingContext)
                    self <! CreateDriverPackageMessage.UpdatePackagingContext packagingContext
                |UpdatePackagingContext packagingContext ->
                    logger.Info("Updating packaging context to reflect the latest release date...")
                    self <! (updatePackagingContext packagingContext dpcc)                    
                |PackagingContextUpdated (oldpackagingContext,newPackagingContext) ->
                    logger.Info(sprintf "Packging context has been updated %A -> %A." oldpackagingContext newPackagingContext)
                    logger.Info("Requesting move of pacakging...")
                    self <! CreateDriverPackageMessage.MovePackaging (oldpackagingContext, newPackagingContext)                        
                    return! create packagingContext                
                |MovePackaging (sourceContext,destinationContext) ->
                    logger.Info(sprintf "Moving package from '%A' -> '%A'" sourceContext destinationContext)
                    self <! (movePackaging sourceContext destinationContext)
                    return! create destinationContext
                |PackagagingMoved ->
                    logger.Info(sprintf "Package has been moved.")
                    logger.Info("Requesting update of install xml")
                    self <! CreateDriverPackageMessage.UpdateInstallXml packagingContext
                |UpdateInstallXml packagingContext ->
                    logger.Info(sprintf "Updating install xml...")
                    self <! (updateInstallXml packagingContext)
                |InstallXmlUpdated installConfiguration ->
                    logger.Info(sprintf "Install xml has been updated: %A." installConfiguration)
                    self <! CreateDriverPackageMessage.CreatePackageDefinition (packagingContext, installConfiguration)
                |CreatePackageDefinition (packagingContext,installConfiguration)->
                    logger.Info(sprintf "Creating main package definition...")
                    self <! (createPackageDefinitionSms packagingContext installConfiguration)
                |PackageDefinitionCreated (packageDefinition, installConfiguration) ->
                    logger.Info(sprintf "Main package definition has been created: %A." packageDefinition)
                    self <! CreateDriverPackageMessage.CreateDismPackageDefinition (packagingContext,installConfiguration)
                |CreateDismPackageDefinition (packagingContext,installConfiguration)->
                    logger.Info(sprintf "Creating DISM package definition...")
                    self <! (createPackageDefinitionDismSms packagingContext installConfiguration)
                |DismPackageDefinitionCreated packageDefinitionDismFilePath ->
                    logger.Info(sprintf "DISM package definition has been created: %A." packageDefinitionDismFilePath)
                    self <! CreateDriverPackageMessage.PackagingFinalized 
                |PackagingFinalized  ->
                    logger.Info(sprintf "Successfully finalized packaging.")
                    logger.Info("Requesting shutdown...")
                    self <! Finished                    
                |Finished ->
                    logger.Info(sprintf "Shutting down...")
                    clientActor <! (new QuitHostMessage())
                    logger.Info(sprintf "Shutting down ExtractActor...")
                    system.Stop(extractCoordinatorActor)
                    logger.Info(sprintf "Shutting down DownloadActor...")
                    system.Stop(downloadCoordinatorActor)
                    logger.Info(sprintf "Finished shuting down...")                    
                    self <! (Akka.Actor.PoisonPill.Instance)                                                        
                |CreateDriverPackageMessage.Info info ->
                    logger.Info(info)
                |CreateDriverPackageMessage.Warning info ->
                    logger.Warn(info)
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
        