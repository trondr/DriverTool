namespace DriverTool

module DownloadCoordinatorActor =
    
    open Akka.FSharp
    open Akka.Routing
    open DriverTool.Library.Messages
    open DriverTool.DownloadActor    
    open DriverTool.Library.F0
    open DriverTool.Library.Logging
    open DriverTool.Library.PackageXml
    open DriverTool.Library
    open DriverTool.Library.Web
    open DriverTool.Library.WebDownload
    let logger = getLoggerByName "DownloadCoordinatorActor"

    //The download coordinator context is used to keep track of which download jobs have been started.
    type DownloadCoordinatorContext = 
        {            
            PackageDownloads:Map<FileSystem.Path,List<PackageInfo>>
        }

    //Check if download job is allready beeing downloaded.
    let allreadyDownloading downloadCoordinatorContext destinationFile  =
        (downloadCoordinatorContext.PackageDownloads.ContainsKey(destinationFile))

    //Check if download job is allready beeing downloaded.
    let notAllreadyDownloading downloadCoordinatorContext destinationFile  =
        not (allreadyDownloading downloadCoordinatorContext destinationFile)

    let isPackageRegistered downloadCoordinatorContext destinationFile package =
        if(not (allreadyDownloading downloadCoordinatorContext destinationFile)) then
            false
        else
            let currentPackages = downloadCoordinatorContext.PackageDownloads.Item(destinationFile)
            let packageExists = (currentPackages |> List.filter (fun p -> p = package) |> List.length) > 0
            packageExists

    /// Add download destination file and corresponding package to download coordinator context if not allready added.
    let updateDownloadCoordinatorContext downloadCoordinatorContext packageInfo destinationFile =
        if(not (allreadyDownloading downloadCoordinatorContext destinationFile)) then
            //download job not added, register download job with corresponding dependent package
            {downloadCoordinatorContext with PackageDownloads=downloadCoordinatorContext.PackageDownloads.Add(destinationFile, [packageInfo])}
        else
            if(not (isPackageRegistered downloadCoordinatorContext destinationFile packageInfo)) then
                //download destination file allready added but package is not registered
                //register new package depending on the same download destination file
                let currentPackages = downloadCoordinatorContext.PackageDownloads.Item(destinationFile)                
                let updatedPackages = currentPackages  @ [packageInfo]            
                let updatedPackageDownloads = downloadCoordinatorContext.PackageDownloads |> (Map.add (destinationFile) updatedPackages)
                {downloadCoordinatorContext with PackageDownloads=updatedPackageDownloads}                        
            else
                //download destination file and correspondin package is allready added. No change.
                downloadCoordinatorContext                

    let updateDownloadsCoordinatorContext downloadCoordinatorContext packageInfo destinationFiles =        
        let rec fold dlc p dfs =
            match dfs with
            |[] -> dlc
            |h::xs -> 
                let udlc = updateDownloadCoordinatorContext dlc p h
                fold udlc p xs
        fold downloadCoordinatorContext packageInfo destinationFiles

    //Remove destination file from downloadCoordinator context and return any packages that are finished.
    let removeFromDownloadsCoordinatorContext downloadCoordinatorContext destinationFile = 
        if(allreadyDownloading downloadCoordinatorContext destinationFile) then            
            let currentPackagesRemoved = downloadCoordinatorContext.PackageDownloads.Item(destinationFile)
            let updatedDownloadCoordinatorContext = {downloadCoordinatorContext with PackageDownloads=downloadCoordinatorContext.PackageDownloads.Remove(destinationFile)}
            let allRemainingPackages = updatedDownloadCoordinatorContext.PackageDownloads |> Map.toSeq |>  Seq.map (fun (k,v) -> v) |> Seq.concat |> Seq.toList
            let finishedPackages = currentPackagesRemoved |> List.filter(fun p -> not (List.contains p allRemainingPackages) )
            (updatedDownloadCoordinatorContext, finishedPackages)
        else
            (downloadCoordinatorContext,List.empty)

    /// Get unique downloads jobs (installer and reamde) from package. Readme file can be shared by more than one package so
    /// make sure to check if a file is not allready beeing downloaded according to the downloads coordinator context. This is necessary
    /// to avoid any file system sharing vialations when two threads attempts to download to the same destination file.
    let packageToUniqueDownloadJob downloadCoordinatorContext destinationFolderPath package =
        let uniqueDownloadJobs =                 
            (packageInfoToWebFileDownloads destinationFolderPath package)            
            |>Seq.filter(fun wfd -> notAllreadyDownloading downloadCoordinatorContext wfd.Destination.DestinationFile)            
            |>Seq.toArray
        uniqueDownloadJobs

    let webFileDownloadsToDestinationFiles webFileDownloads =
        webFileDownloads |> Seq.map(fun d -> d.Destination.DestinationFile)

    let downloadCoordinatorActor (mailbox:Actor<_>) =
        
        let downloadActor = spawnOpt mailbox.Context "DownloadActor" downloadActor [ SpawnOption.Router(SmallestMailboxPool(10)) ]
        let downloadCoordinatorContext = {PackageDownloads=Map.empty}

        let rec loop downloadCoordinatorContext = 
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender, mailbox.Context.Self)
                match message with
                |DownloadPackage (package,packagingContext) -> 
                    logger.Info(sprintf "Downloading package %A." package)                    
                    let webFileDownloads = packageToUniqueDownloadJob downloadCoordinatorContext packagingContext.CacheFolderPath package
                    webFileDownloads
                    |> Array.map(fun dl -> 
                        logger.Info(sprintf "Request download of job %A for package %A." dl package)
                        downloadActor <! CreateDriverPackageMessage.StartDownload dl
                        ) |> ignore                      
                    let destinationFiles = (webFileDownloadsToDestinationFiles webFileDownloads) |> List.ofSeq
                    let updatedDownloadCoordinatorContext = updateDownloadsCoordinatorContext downloadCoordinatorContext package destinationFiles 
                    return! loop updatedDownloadCoordinatorContext                    
                |StartDownload webFileDownload ->
                    logger.Info(sprintf "Request download of job %A." webFileDownload)
                    downloadActor <! CreateDriverPackageMessage.StartDownload webFileDownload                    
                    return! loop downloadCoordinatorContext
                |DownloadFinished webFileDownload ->
                    logger.Info(sprintf "Download of job %A is finished." webFileDownload)
                    let (updatedDownloadCoordinatorContext, finishedPackages) = removeFromDownloadsCoordinatorContext downloadCoordinatorContext webFileDownload.Destination.DestinationFile
                    let destinationFolderPath = FileSystem.getDirectoryPath webFileDownload.Destination.DestinationFile
                    finishedPackages
                        |> List.map(fun p -> 
                                let downloadedPackage = toDownloadedPackageInfo destinationFolderPath p
                                self <! (CreateDriverPackageMessage.PackageDownloaded (Some downloadedPackage))
                            ) |> ignore
                    return! loop updatedDownloadCoordinatorContext
                |PackageDownloaded downloadedPackage ->
                    logger.Warn("TODO: Handle downloaded packages for further processing (== extract).")
                    ()
                //|DownloadSccmPackage sccmPackageDownloadContext ->
                //    logger.Info(sprintf "Request download of sccm package %A." sccmPackageDownloadContext)
                //    downloadActor <! CreateDriverPackageMessage.DownloadSccmPackage sccmPackageDownloadContext                
                //    return! loop downloadCoordinatorContext
                //|CreateDriverPackageMessage.Error ex ->
                //    logger.Warn(sprintf "Download failed due to: %s" (getAccumulatedExceptionMessages ex))
                //    logger.Warn("Ignoring download failure and continue processing.")
                //    return! loop downloadCoordinatorContext
                | _ ->
                    logger.Warn(sprintf "Message not handled by DownloadActorCoordinator: %A" message)
                    return! loop downloadCoordinatorContext
                return! loop downloadCoordinatorContext
            }
        loop downloadCoordinatorContext

