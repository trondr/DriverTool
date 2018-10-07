namespace DriverTool
open Microsoft.FSharp.Collections
open F

module CreateDriverPackage =
    open System
    open DriverTool
    open System.Net
    open FSharp.Collections.ParallelSeq
    open Checksum

    let validateExportCreateDriverPackageParameters (modelCode:Result<ModelCode,Exception>, operatingSystemCode:Result<OperatingSystemCode,Exception>) = 
        
        let validationResult = 
            match modelCode with
                    |Ok m ->
                        match operatingSystemCode with
                        |Ok os -> Result.Ok (m, os)                            
                        |Error ex -> Result.Error ex
                    |Error ex -> Result.Error ex
        match validationResult with
        |Ok _ -> validationResult
        |Error _ -> 
            //Accumulate all non-empty error messages into an array
            let errorMessages = 
                [|
                    (match modelCode with
                    |Error ex -> ex.Message
                    |Ok _-> String.Empty);

                    (match operatingSystemCode with
                    |Error ex -> ex.Message
                    |Ok _-> String.Empty);
                |] |> Array.filter (fun m -> (not (String.IsNullOrWhiteSpace(m)) ) )            
            Result.Error (new Exception(String.Format("Failed to validate one or more input parameters.{0}{1}",Environment.NewLine, String.Join(Environment.NewLine, errorMessages))))
    
    
    let getUniqueUpdates packageInfos = 
        let uniqueUpdates = 
            packageInfos
            |> Seq.groupBy (fun p -> p.InstallerName)
            |> Seq.map (fun (k,v) -> v |>Seq.head)
        uniqueUpdates

    let getUniqueUpdatesR (updatesResult : Result<seq<PackageInfo>,Exception>) : Result<seq<PackageInfo>,Exception> =
        match updatesResult with
        |Error ex -> Result.Error ex
        |Ok u ->             
            Seq.groupBy (fun p -> p.InstallerName) u            
            |> Seq.map (fun (k,v) -> v |>Seq.head)
            |> Result.Ok
    
    let downloadFilePlain (sourceUri:Uri, destinationFile, force) =
        try
            use webClient = new System.Net.WebClient()
            let webHeaderCollection = new WebHeaderCollection()
            webHeaderCollection.Add("User-Agent", "LenovoUtil/1.0") 
            webClient.Headers <- webHeaderCollection
            
            let destinationPath = Path.createWithContinuation (fun p -> FileOperations.ensureFileDoesNotExist true p) (fun ex -> Result.Error ex) destinationFile  
            
            match destinationPath with
            |Ok path -> 
                Console.WriteLine("Downloading '{0}' -> {1}...", sourceUri.OriginalString, path.Value)
                webClient.DownloadFile(sourceUri.OriginalString,path.Value)
                Result.Ok destinationFile      
            |Error ex -> Result.Error (new Exception(String.Format("Destination file '{0}' allready exists", destinationFile), ex))            
        with
        | ex -> Result.Error (new Exception( String.Format("Failed to download {0} due to {e.Message}",sourceUri.OriginalString, ex.Message),ex))
    
    let downloadFile (sourceUri:Uri, destinationFile, force) =
        Logging.debugLoggerResult downloadFilePlain (sourceUri, destinationFile, force)

    let verifyDownload downloadJob =
        match (hasSameFileHash (downloadJob.DestinationFile, downloadJob.Checksum, downloadJob.Size)) with
        |true  -> Result.Ok downloadJob
        |false -> Result.Error (new Exception(String.Format("Destination file ('{0}') hash does not match source file ('{1}') hash.",downloadJob.DestinationFile,downloadJob.SourceUri.OriginalString)))
 
    let downloadUpdatePlain (downloadJob) =
        match (hasSameFileHash (downloadJob.DestinationFile, downloadJob.Checksum, downloadJob.Size)) with
        |false -> 
            let downloadResult = 
                downloadFile (downloadJob.SourceUri, downloadJob.DestinationFile, true)
            match downloadResult with
            |Ok s -> 
                verifyDownload downloadJob
            |Error ex -> Result.Error (new Exception("Download could not be verified. " + ex.Message))
        |true -> 
            Logging.getLoggerByName("downloadUpdatePlain").Info(String.Format("Destination file '{0}' allready exists", downloadJob.DestinationFile))
            Result.Ok downloadJob

    let downloadUpdate (downloadJob) =
        Logging.debugLoggerResult downloadUpdatePlain (downloadJob)

    let packageInfosToDownloadedPackageInfos destinationDirectory packageInfos =
        packageInfos
        |> Seq.map (fun p -> 
                        {
                            InstallerPath = getDestinationInstallerPath destinationDirectory p;
                            ReadmePath = getDestinationReadmePath destinationDirectory p;
                            Package = p;
                        }
                    )
            
    
    let downloadUpdates destinationDirectory packageInfos = 
        let downloadJobs = 
            packageInfos 
            |> packageInfosToDownloadJobs destinationDirectory
            |> PSeq.map (fun dj -> downloadUpdate (dj))
            |> PSeq.toArray
            |> Seq.ofArray
            |> toAccumulatedResult
        match downloadJobs with
        |Ok _ -> 
            Result.Ok (packageInfosToDownloadedPackageInfos destinationDirectory packageInfos)
        |Error ex -> 
            Result.Error ex

    let downloadUpdatesR destinationDirectory (packageInfos : Result<seq<PackageInfo>,Exception>) =        
        match packageInfos with
        | Ok ps -> downloadUpdates destinationDirectory ps            
        | Error ex -> Result.Error ex
    
    let toTitlePostFix (title:string) (version:string) (releaseDate:string) = 
        nullOrWhiteSpaceGuard title "title"
        let parts = title.Split('-');
        let titlePostfix = 
            match parts.Length with
            | 0 -> String.Empty
            | _ -> parts.[parts.Length - 1]
        toValidDirectoryName (String.Format("{0}_{1}_{2}",titlePostfix,version,releaseDate))
    
    open System.Linq
    open ExistingPath
    open DriverTool

    let toTitlePrefix (title:string) (category:string) (postFixLength: int) = 
        nullOrWhiteSpaceGuard title "title"
        nullGuard category "category"
        let parts = title.Split('-');
        let partsString =
            (parts.[0]).AsEnumerable().Take(57 - postFixLength - category.Length).ToArray()
        let titlePrefix = 
            category + "_" + new String(partsString);
        toValidDirectoryName titlePrefix    

    let getPackageFolderName (packageInfo:PackageInfo) =
        let validDirectoryName = 
            toValidDirectoryName packageInfo.Title
        let postfix = 
            toTitlePostFix validDirectoryName packageInfo.Version packageInfo.ReleaseDate
        let prefix = 
            toTitlePrefix validDirectoryName (packageInfo.Category |? String.Empty) postfix.Length
        let packageFolderName = 
            String.Format("{0}_{1}",prefix,postfix).Replace("__", "_").Replace("__", "_");
        packageFolderName

    let extractUpdateToPackageFolder downloadJob packageFolder =
        Result.Error "Not implemented"

    let downloadedPackageInfoToExtractedPackageInfo downloadedPackageInfo =
        {
            ExtractedDirectoryPath = getPackageFolderName downloadedPackageInfo.Package;
            DownloadedPackage = downloadedPackageInfo;
        }

    let copyFile (sourceFilePath, destinationFilePath) =
        try
            System.IO.File.Copy(sourceFilePath, destinationFilePath, true)
            Result.Ok destinationFilePath
        with
        | ex -> Result.Error (new Exception("Failed to copy file.", ex))
    
    let extractReadme downloadedPackageInfo packageFolderPath  =
        let destinationReadmeFilePath = System.IO.Path.Combine(packageFolderPath,downloadedPackageInfo.Package.ReadmeName)
        match ExistingFilePath.New downloadedPackageInfo.ReadmePath with
        |Ok readmeFilePath -> 
            match (copyFile (readmeFilePath.Value, destinationReadmeFilePath)) with
            |Ok _ -> 
                Result.Ok (downloadedPackageInfoToExtractedPackageInfo downloadedPackageInfo)
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let getFileNameFromCommandLine (commandLine:string) = 
        let fileName = commandLine.Split(' ').[0];
        fileName;

    let extractInstaller downloadedPackageInfo packageFolderPath =
        if(String.IsNullOrWhiteSpace(downloadedPackageInfo.Package.ExtractCommandLine)) then
            //Installer does not support extraction, copy the installer directly to package folder...
           let destinationInstallerFilePath = System.IO.Path.Combine(packageFolderPath,downloadedPackageInfo.Package.InstallerName)
           match ExistingFilePath.New downloadedPackageInfo.InstallerPath with
           |Ok installerPath -> 
                match copyFile (installerPath.Value, destinationInstallerFilePath) with
                |Ok _ -> 
                    Result.Ok (downloadedPackageInfoToExtractedPackageInfo downloadedPackageInfo)
                |Error ex -> Result.Error ex
           |Error ex -> 
                Result.Error ex
        else
            //Installer supports extraction
            let extractCommandLine = downloadedPackageInfo.Package.ExtractCommandLine.Replace("%PACKAGEPATH%",String.Format("\"{0}\"",packageFolderPath))
            let fileName = getFileNameFromCommandLine extractCommandLine
            let arguments = extractCommandLine.Replace(fileName,"")
            match (ExistingFilePath.New downloadedPackageInfo.InstallerPath) with
            |Ok fp -> 
                match DriverTool.ProcessOperations.startProcess (fp.Value, arguments) with
                |Ok _ -> Result.Ok (downloadedPackageInfoToExtractedPackageInfo downloadedPackageInfo)
                |Error ex -> Result.Error ex
            |Error ex -> Result.Error ex

    let extractUpdate (rootDirectory, (downloadedPackageInfo:DownloadedPackageInfo)) =
        let packageFolderName = getPackageFolderName downloadedPackageInfo.Package
        let packageFolderPath = System.IO.Path.Combine(rootDirectory,packageFolderName)
        
        match (extractReadme downloadedPackageInfo packageFolderPath) with
        |Ok _ -> 
            match (extractInstaller downloadedPackageInfo packageFolderPath) with
            |Ok _ -> Result.Ok (downloadedPackageInfoToExtractedPackageInfo downloadedPackageInfo)
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    let downloadedPackageInfosToExtractedPackageInfos (downloadedPackageInfos:seq<DownloadedPackageInfo>) =
        downloadedPackageInfos
        |> Seq.map (fun dp -> 
                        downloadedPackageInfoToExtractedPackageInfo dp
                    )

    let extractUpdates rootDirectory downloadedPackageInfos = 
        downloadedPackageInfos
        |> Seq.map (fun dp -> extractUpdate (rootDirectory, dp))
        |> toAccumulatedResult
     
    open DriverTool.PathOperations

    let createDriverPackageSimple ((model: ModelCode), (operatingSystem:OperatingSystemCode), (destinationFolderPath: Path)) = 
           result {
                let! packageInfos = ExportRemoteUpdates.getRemoteUpdates (model, operatingSystem, true)
                let uniquePackageInfos = packageInfos |> Seq.distinct
                let uniqueUpdates = uniquePackageInfos |> getUniqueUpdates
                let! updates = downloadUpdates (System.IO.Path.GetTempPath()) uniqueUpdates
                let driversPath = combine2Paths destinationFolderPath.Value "Drivers"
                let! extractedUpdates = extractUpdates driversPath updates                
                return extractedUpdates
            }
    
    let createDriverPackage ((modelCode: ModelCode), (operatingSystem:OperatingSystemCode),(destinationFolderPath: Path)) =
        Logging.debugLoggerResult createDriverPackageSimple (modelCode, operatingSystem, destinationFolderPath)

        