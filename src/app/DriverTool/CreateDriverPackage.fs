namespace DriverTool
open Microsoft.FSharp.Collections
open F

module CreateDriverPackage =
    open System
    open DriverTool
    open System.Net
    open FSharp.Collections.ParallelSeq
    open System.Text
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

    let downloadUpdatePlain (downloadJob) =
        match hasSameFileHash (downloadJob.DestinationFile, downloadJob.Checksum, downloadJob.Size) with
        |false -> 
            let downloadResult = 
                downloadFile (downloadJob.SourceUri, downloadJob.DestinationFile, true)
            match downloadResult with
            |Ok s -> Result.Ok downloadJob
            |Error ex -> Result.Error ex
        |true -> 
            Logging.getLoggerByName("downloadUpdatePlain").Info(String.Format("Destination file '{0}' allready exists", downloadJob.DestinationFile))
            Result.Ok downloadJob

    let downloadUpdate (downloadJob) =
        Logging.debugLoggerResult downloadUpdatePlain (downloadJob)

    let downloadUpdates destinationDirectory packageInfos = 
        let res = 
            packageInfos 
            |> packageInfosToDownloadJobs destinationDirectory
            |> PSeq.map (fun dj -> downloadUpdate (dj))
            |> PSeq.toArray
            |> Seq.ofArray
            |> toAccumulatedResult
        res    
        
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

    let extractUpdate (rootDirectory, downloadJob) =
        let packageFolderName = getPackageFolderName downloadJob.Package
        let packageFolderDirectoryName = System.IO.Path.Combine(rootDirectory,packageFolderName)
        Result.Error "Not implemented"
        
    let extractUpdates rootDirectory downloadJobs = 
        downloadJobs
        |> Seq.map (fun dj -> extractUpdate (rootDirectory, dj))
        Result.Ok downloadJobs
        
    let createDriverPackageSimple ((model: ModelCode), (operatingSystem:OperatingSystemCode)) = 
        let res = 
            result {
                let! packageInfos = ExportRemoteUpdates.getRemoteUpdates (model, operatingSystem, true)
                let uniquePackageInfos = packageInfos |> Seq.distinct
                let uniqueUpdates = uniquePackageInfos |> getUniqueUpdates
                let! updates = downloadUpdates (System.IO.Path.GetTempPath()) uniqueUpdates
                let! extractedUpdates = extractUpdates "" updates                
                return extractedUpdates
            }
        res

    let createDriverPackage ((model: ModelCode), (operatingSystem:OperatingSystemCode)) =
        Logging.debugLoggerResult createDriverPackageSimple ((model: ModelCode), (operatingSystem:OperatingSystemCode))

        