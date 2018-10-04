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
    
    let getUniqueUpdates (updatesResult : Result<seq<PackageInfo>,Exception>) : Result<seq<PackageInfo>,Exception> =
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

    let downloadUpdatePlain (destinationDirectory, packageInfo) =
        match String.IsNullOrWhiteSpace(packageInfo.BaseUrl) with
        | true -> Result.Error (new Exception(String.Format("Base url is undefined for update '{0}' ({1}). Please verify that update is still present in the update catlog for the model in question. The model catalog location has the format: https://download.lenovo.com/catalog/<modelcode>_<oscode>.xml",packageInfo.Title, packageInfo.InstallerName)))
        | false ->
            let sourceReadmeUrl = String.Format("{0}/{1}", packageInfo.BaseUrl, packageInfo.ReadmeName)
            let sourceReadmeUri = new Uri(sourceReadmeUrl)
            let destinationReadmePath = System.IO.Path.Combine(destinationDirectory, packageInfo.ReadmeName)            
            let readmeDownloadResult = 
                match hasSameFileHash (destinationReadmePath, packageInfo.ReadmeCrc) with
                |false -> downloadFile (sourceReadmeUri, destinationReadmePath, true)
                |true -> 
                    Logging.getLoggerByName("downloadUpdate").Info(String.Format("Destination file '{0}' allready exists", destinationReadmePath))
                    Result.Ok destinationReadmePath
            
            let sourceInstallerUrl = String.Format("{0}/{1}", packageInfo.BaseUrl, packageInfo.InstallerName)
            let sourceInstallerUri = new Uri(sourceInstallerUrl)
            let destinationInstallerPath = System.IO.Path.Combine(destinationDirectory, packageInfo.InstallerName)
            match hasSameFileHash (destinationInstallerPath, packageInfo.InstallerCrc) with
            |false -> downloadFile (sourceInstallerUri, destinationInstallerPath, true)
            |true -> 
                Logging.getLoggerByName("downloadUpdate").Info(String.Format("Destination file '{0}' allready exists", destinationInstallerPath))
                Result.Ok destinationInstallerPath
    
    let downloadUpdate (destinationDirectory, packageInfo) =
        Logging.debugLoggerResult downloadUpdatePlain (destinationDirectory, packageInfo)

    let downloadUpdates destinationDirectory packageInfos = 
        let res = 
            packageInfos 
            |> PSeq.map (fun p -> downloadUpdate (destinationDirectory, p))
            |> PSeq.toArray
            |> Seq.ofArray
            |> toAccumulatedResult
        res    
        
    let downloadUpdatesR destinationDirectory (packageInfos : Result<seq<PackageInfo>,Exception>) =        
        match packageInfos with
        | Ok ps -> downloadUpdates destinationDirectory ps            
        | Error ex -> Result.Error ex
        
    let createDriverPackageSimple ((model: ModelCode), (operatingSystem:OperatingSystemCode)) = 
        ExportRemoteUpdates.getRemoteUpdates (model, operatingSystem, true)
        |> getUnique
        |> getUniqueUpdates
        |> downloadUpdatesR (System.IO.Path.GetTempPath())

    let createDriverPackage ((model: ModelCode), (operatingSystem:OperatingSystemCode)) =
        Logging.debugLoggerResult createDriverPackageSimple ((model: ModelCode), (operatingSystem:OperatingSystemCode))

        