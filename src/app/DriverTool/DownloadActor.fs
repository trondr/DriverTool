namespace DriverTool

module DownloadActor =
    open DriverTool.Library.Messages
    open DriverTool.Library.Logging
    open DriverTool.Library.F
    open DriverTool.Library.F0
    open DriverTool.Library.PathOperations    
    open DriverTool.Library.PackageXml    
    open DriverTool.Library.WebDownload
    open DriverTool.Library.Web        
    let logger = getLoggerByName "DownloadActor"

    let downloadUpdate' (downloadJob,ignoreVerificationErrors) =
        DriverTool.Library.Web.downloadIfDifferent (logger, downloadJob,ignoreVerificationErrors)

    let downloadUpdate (downloadJob,ignoreVerificationErrors) =
        genericLoggerResult LogLevel.Debug downloadUpdate' (downloadJob,ignoreVerificationErrors)

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
    