namespace DriverTool
open Microsoft.FSharp.Collections
open F

module CreateDriverPackage =
    open System
    open DriverTool
    open System.Net
    open FSharp.Collections.ParallelSeq

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
    
    let downloadFile (sourceUri:Uri) destinationFile =
        try
            use webClient = new System.Net.WebClient()
            let webHeaderCollection = new WebHeaderCollection()
            webHeaderCollection.Add("User-Agent", "LenovoUtil/1.0") 
            webClient.Headers <- webHeaderCollection
            match System.IO.File.Exists(destinationFile) with
            |false -> 
                Console.WriteLine("Downloading '{0}' -> {1}...", sourceUri.OriginalString, destinationFile)
                webClient.DownloadFile(sourceUri.OriginalString,destinationFile)
                Result.Ok destinationFile      
            |true -> Result.Error (new Exception(String.Format("Destination file '{0}' allready exists", destinationFile)))
            
        with
        | ex -> Result.Error (new Exception( String.Format("Failed to download {0} due to {e.Message}",sourceUri.OriginalString, ex.Message),ex))


    let downloadUpdate  destinationDirectory packageInfo =
        match String.IsNullOrWhiteSpace(packageInfo.BaseUrl) with
        | true -> Result.Error (new Exception(String.Format("Base url is undefined for update '{0}' ({1}). Please verify that update is still present in the update catlog for the model in question. The model catalog location has the format: https://download.lenovo.com/catalog/<modelcode>_<oscode>.xml",packageInfo.Title, packageInfo.InstallerName)))
        | false ->
            let sourceReadmeUrl = String.Format("{0}/{1}", packageInfo.BaseUrl, packageInfo.ReadmeName)
            let sourceReadmeUri = new Uri(sourceReadmeUrl)
            let destinationReadmePath = System.IO.Path.Combine(destinationDirectory, packageInfo.ReadmeName)
            let downloadReadmeResult = downloadFile sourceReadmeUri destinationReadmePath

            let sourceInstallerUrl = String.Format("{0}/{1}", packageInfo.BaseUrl, packageInfo.InstallerName)
            let sourceInstallerUri = new Uri(sourceInstallerUrl)
            let destinationInstallerPath = System.IO.Path.Combine(destinationDirectory, packageInfo.InstallerName)
            downloadFile sourceInstallerUri destinationInstallerPath

    let downloadUpdates destinationDirectory (packageInfos : Result<seq<PackageInfo>,Exception>) =
        match packageInfos with
        | Ok ps -> 
            ps        
            |> PSeq.map (fun p -> downloadUpdate destinationDirectory p)            
            |> Result.Ok
        | Error ex -> Result.Error ex
        
    let createDriverPackageSimple ((model: ModelCode), (operatingSystem:OperatingSystemCode)) = 
        ExportRemoteUpdates.getRemoteUpdates (model, operatingSystem, true)
        |> getUnique
        |> getUniqueUpdates
        |> downloadUpdates (System.IO.Path.GetTempPath())

    let createDriverPackage ((model: ModelCode), (operatingSystem:OperatingSystemCode)) =
        Logging.debugLogger createDriverPackageSimple ((model: ModelCode), (operatingSystem:OperatingSystemCode))

        