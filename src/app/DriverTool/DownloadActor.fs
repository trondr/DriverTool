namespace DriverTool

module DownloadActor =
            
    open DriverTool.Library.Messages
    open DriverTool.Library.Logging
    open DriverTool.Library.F
    open DriverTool.Library.F0
    open DriverTool.Library.PathOperations
    open DriverTool.Actors
    open DriverTool.Library.PackageXml    
    open DriverTool.Library.Web
    open FSharp.Collections.ParallelSeq
    open Akka.FSharp
    let logger = getLoggerByName "DownloadActor"

    let downloadSccmPackage (sccmPackageDownloadContext:SccmPackageInfoDownloadContext) =
        match(result{
            let downloadSccmPackage = DriverTool.Updates.downloadSccmPackageFunc sccmPackageDownloadContext.Manufacturer
            let! downloadedSccmPackage = downloadSccmPackage (sccmPackageDownloadContext.CacheFolderPath, sccmPackageDownloadContext.SccmPackage)
            return downloadedSccmPackage
        })with
        |Result.Error ex ->            
            CreateDriverPackageMessage.Error ex
        |Ok downloadedSccmPackage -> 
            DownloadedSccmPackage downloadedSccmPackage

    let downloadSccmPackageAsync context =
        System.Threading.Tasks.Task.Run(fun () -> downloadSccmPackage context)
        |>Async.AwaitTask

    let downloadUpdateBase (downloadInfo:DownloadInfo, ignoreVerificationErrors) =
        downloadIfDifferent (logger,downloadInfo, ignoreVerificationErrors)         

    let downloadUpdate (downloadJob, ignoreVerificationErrors) =
        genericLoggerResult LogLevel.Debug downloadUpdateBase (downloadJob, ignoreVerificationErrors)

    let packageInfosToDownloadedPackageInfos destinationDirectory (packageInfos:seq<PackageInfo>) (downloadJobs:seq<DownloadInfo>) =
        packageInfos
        //Remove packages with no download jobs (download job for the package failed typically)
        |> Seq.filter(fun p ->
                        let downloadJob = downloadJobs|>Seq.tryFind(fun dj -> 
                                                let djFileName = getFileNameFromPath dj.DestinationFile
                                                p.Installer.Name = djFileName
                                            )
                        optionToBoolean downloadJob
                    )
        //Create downloaded package info
        |> Seq.map (fun p -> 
                        {
                            InstallerPath = getDestinationInstallerPath destinationDirectory p;
                            ReadmePath = getDestinationReadmePath destinationDirectory p;
                            PackageXmlPath = getDestinationPackageXmlPath destinationDirectory p;
                            Package = p;
                        }
                    )
        |>Seq.toArray

    let downloadPackage destinationDirectory packageInfo =
        let downloadJobs = 
            packageInfo
            |> packageInfoToDownloadJobs destinationDirectory            
            |> PSeq.map (fun dj -> resultToOption logger (downloadUpdate (dj,ignoreVerificationErrors dj)))
            |> PSeq.toArray
            |> Seq.choose id //Remove all failed downloads            
            |> Seq.toArray            
        let downloadedPackageInfo = 
            packageInfosToDownloadedPackageInfos destinationDirectory (seq{packageInfo}) downloadJobs
            |> Array.tryPick Some
        CreateDriverPackageMessage.DownloadedPackage downloadedPackageInfo

    let downloadPackageAsync destinationDirectory packageInfo  =
        System.Threading.Tasks.Task.Run(fun () -> downloadPackage destinationDirectory packageInfo)
        |>Async.AwaitTask

    let downloadActor (mailbox:Actor<_>) =
        
        let rec loop () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender, mailbox.Context.Self)
                match message with
                |DownloadPackage (package,packagingContext) -> 
                    logger.Info(sprintf "Downloading package %A." package)
                    (downloadPackageAsync packagingContext.CacheFolderPath package)               
                    |>pipeToWithSender self sender
                |DownloadSccmPackage sccmPackageDownloadContext -> 
                    logger.Info(sprintf "Downloading sccm package %A." sccmPackageDownloadContext)
                    (downloadSccmPackageAsync sccmPackageDownloadContext)
                    |>pipeToWithSender self sender                
                |CreateDriverPackageMessage.Error ex ->
                    logger.Warn(sprintf "Download failed due to: %s" (getAccumulatedExceptionMessages ex))
                    logger.Warn("Ignoring download failure and continue processing.")
                | _ ->
                    logger.Warn(sprintf "Message not handled by DownloadActor: %A" message)
                    return! loop()
                return! loop ()
            }
        loop()

    