namespace DriverTool

module DownloadCoordinatorActor =
    open DriverTool.Library.Logging
    open DriverTool.Library.PackageXml
    open DriverTool.Library    
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
