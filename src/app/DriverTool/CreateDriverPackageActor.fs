namespace DriverTool

module CreateDriverPackageActor =
    open System
    open DriverTool.Library.Environment
    open DriverTool.Library.F0
    open DriverTool.Library.F
    open DriverTool.Library.Messages    
    open DriverTool.Library
    open DriverTool.Library.Logging
    open DriverTool.Library.PackageXml
    open DriverTool.Library.UpdatesContext    
    open DriverTool.PackageTemplate
    open DriverTool.Library.PackageDefinition
    open DriverTool.Packaging
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
    
    let throwNotInitializedException actorMessage =            
        throwExceptionWithLogging logger (sprintf "Packaging actor has not been initialized. Cannot process message: %A" actorMessage)

    let throwAllreadyInitializedException actorMessage =            
        throwExceptionWithLogging logger (sprintf "Packaging actor has allready been initialized. Cannot process message %A" actorMessage)

    let isProgressFinished progress =
        let percentageProgress = (getPercent progress)        
        Math.Abs(100.0 - percentageProgress) < 0.001

    let isPackagingFinished (packagingProgress:PackagingProgress) =
        match packagingProgress.Started with
        |true ->
            let isFinished =
                imperative{
                    if(not (isProgressFinished packagingProgress.PackageDownloads)) then return false
                    if(not (isProgressFinished packagingProgress.SccmPackageDownloads)) then return false
                    if(not (isProgressFinished packagingProgress.PackageExtracts)) then return false
                    if(not (isProgressFinished packagingProgress.SccmPackageExtracts)) then return false
                    return true
                }
            isFinished
        |false -> false

    
    let toPackagingProgressString packagingProgress =
        let packageDownloadsProgress = (toProgressMessage packagingProgress.PackageDownloads)
        let sccmPackageDownloadsProgress = (toProgressMessage packagingProgress.SccmPackageDownloads)
        let packageExtractsProgress = (toProgressMessage packagingProgress.PackageExtracts)
        let sccmPackageExtractsProgress = (toProgressMessage packagingProgress.SccmPackageExtracts)
        sprintf "Progress: %s, %s, %s, %s" packageDownloadsProgress sccmPackageDownloadsProgress packageExtractsProgress sccmPackageExtractsProgress

    let checkAndUpdatePackagingContextProgress (packagingContext:PackagingContext) =
        match (isPackagingFinished packagingContext.PackagingProgress) with
        |true->
            {packagingContext with PackagingProgress={packagingContext.PackagingProgress with Finished=true}}
        |false ->
            packagingContext
