namespace DriverTool
open FileOperations

module ExportRemoteUpdates = 
    open System
    open System.Net    
    open System.Collections.Generic
    open System.Text.RegularExpressions
        
    let validateExportRemoteUdateInfoParameters (modelCode:Result<ModelCode,Exception>, operatingSystemCode:Result<OperatingSystemCode,Exception>, csvPath:Result<Path,Exception>) = 
        
        let validationResult = 
            match modelCode with
                    |Ok m ->
                        match operatingSystemCode with
                        |Ok os ->
                            match csvPath with
                            |Ok fp -> Result.Ok (m, os, fp)
                            |Error ex -> Result.Error ex
                        |Error ex -> Result.Error ex
                    |Error ex -> Result.Error ex
        match validationResult with
        |Ok _ -> validationResult
        |Error ex -> 
            //Accumulate all non-empty error messages into an array
            let errorMessages = 
                [|
                    (match modelCode with
                    |Error ex -> ex.Message
                    |Ok m-> String.Empty);

                    (match operatingSystemCode with
                    |Error ex -> ex.Message
                    |Ok m-> String.Empty);

                    (match csvPath with
                    |Error ex -> ex.Message
                    |Ok m-> String.Empty)            

                |] |> Array.filter (fun m -> (not (String.IsNullOrWhiteSpace(m)) ) )            
            Result.Error (new Exception(String.Format("Failed to validate one or more input parameters.{0}{1}",Environment.NewLine, String.Join(Environment.NewLine, errorMessages))))

    let getModelInfoUri (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) = 
        new Uri(String.Format("https://download.lenovo.com/catalog/{0}_{1}.xml",modelCode.Value,operatingSystemCode.Value))

    let downloadFileUnsafe (uri:Uri) (filePath:Path) = 
        use webClient = new WebClient()
        webClient.DownloadProgressChanged.Add(fun ea -> System.Console.Write("{0} of {1}\r", ea.BytesReceived, ea.TotalBytesToReceive))
        let webHeaderCollection = new WebHeaderCollection()
        webHeaderCollection.Add("User-Agent", "DriverUtil/1.0")
        webClient.Headers <- webHeaderCollection
        match System.IO.File.Exists(filePath.Value) with
        |false->
            webClient.DownloadFileTaskAsync(uri,filePath.Value) |> Async.AwaitTask |> Async.RunSynchronously
        |true -> raise (new FileExistsException(String.Format("File allready exists: {0}",filePath.Value)))
        filePath

    let downloadFile (uri:Uri) (filePath:Result<Path,Exception>) :Result<Path,Exception> = 
        try
            match filePath with
            |Ok fp -> Result.Ok (downloadFileUnsafe uri fp)
            |Error ex -> Result.Error ex
        with
        | ex -> Result.Error ex


    
    
    open FSharp.Data    
    open DriverTool

    type PackagesXmlProvider = XmlProvider<"https://download.lenovo.com/catalog/20FA_Win7.xml">
    type PackageXmlProvider = XmlProvider<"https://download.lenovo.com/pccbbs/mobiles/n1cx802w_2_.xml">
    
    let getPackagesInfo (modelInfoXmlFilePath:Result<Path,Exception>) : Result<seq<PackageXmlInfo>,Exception>= 
        try
            match modelInfoXmlFilePath with
            |Ok p -> 
                let x = PackagesXmlProvider.Load(p.Value)
                x.Packages
                |> Seq.map (fun p -> { Location = p.Location; Category = p.Category})
                |> Result.Ok
            |Error ex -> Result.Error ex
        with
        |ex -> Result.Error ex
    
    let getTempPath =
        System.IO.Path.GetTempPath()
    
    let getTempFilePath fileName = 
        System.IO.Path.Combine(getTempPath , fileName)

    let getXmlFileNameFromUri (uri: Uri) : Result<string,Exception>= 
        try
            let regExMatch = 
                Regex.Match(uri.OriginalString, @".+/(.+\.xml)$")
            match regExMatch.Success with
            | true -> Result.Ok (regExMatch.Groups.[1].Value)                                        
            | false -> Result.Error (new ArgumentException(String.Format("Uri '{0}' does not represent a xml file.", uri.OriginalString)):> Exception)
        with
        | ex -> Result.Error ex

    let getTempXmlFilePathFromUri uri : Result<Path,Exception> = 
        let xmlFileName = getXmlFileNameFromUri uri
        match xmlFileName with
        |Result.Ok f -> 
            let tempXmlFilePathString = 
                getTempFilePath f
            Path.create tempXmlFilePathString
        |Result.Error ex -> Result.Error ex

    open System.Linq
    open F
    open DriverTool

    let getBaseUrl locationUrl =
        let uri = new Uri(locationUrl)
        uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments.Last().Length).Trim('/');
        
    let downloadPackageInfo (packageXmlInfo:PackageXmlInfo) = 
        let uri = new Uri(packageXmlInfo.Location)
        let tempXmlFileName = 
            getTempXmlFilePathFromUri uri
            |> ensureFileDoesNotExistR true
        let fileResult = downloadFile uri tempXmlFileName               
        match fileResult with
        |Ok p -> 
            let dpi = 
                            {
                            Location = packageXmlInfo.Location;
                            Category = packageXmlInfo.Category
                            FilePath = p;
                            BaseUrl = getBaseUrl packageXmlInfo.Location
                            }
            Result.Ok dpi
        |Error ex -> Result.Error ex
        
    
    let getAllErrorMessages (results:seq<Result<'T,Exception>>) =         
        results
        |> Seq.filter (fun dpi -> 
                            match dpi with
                            |Error _ -> true
                            | _ -> false)
        |> Seq.map (fun dpi -> 
                        match dpi with
                        |Error ex -> getAccumulatedExceptionMessages ex
                        | _ -> String.Empty)

    let getAllSuccesses (results:seq<Result<'T,Exception>>) =
        results
        |> Seq.filter (fun dpi -> 
                                match dpi with
                                |Ok _ -> true
                                | _ -> false
                           )
            |> Seq.map (fun dpi -> 
                            match dpi with
                            |Ok pi -> pi
                            | _ -> failwith "Failed to get all successes due to a bug in the success filter.")

    let downloadPackageXmls packageXmlInfos : Result<seq<DownloadedPackageXmlInfo>,Exception> = 
        let downloadedPackageXmlInfos = 
            packageXmlInfos
            |> Seq.map (fun pi -> downloadPackageInfo pi)

        let objectResults = 
                    downloadedPackageXmlInfos
                    //|> Seq.cast<Result<System.Object,Exception>>

        let allErrorMessages = 
            getAllErrorMessages objectResults

        match allErrorMessages.Count() with
        | 0 ->  
                let allSuccesses = 
                    (getAllSuccesses objectResults)
                    |> Seq.cast<DownloadedPackageXmlInfo>                 
                Result.Ok allSuccesses
        | _ -> 
            let msg = String.Format("Failed to download all package infos due to the following {0} error messages:{1}{2}",allErrorMessages.Count(),Environment.NewLine,String.Join(Environment.NewLine,allErrorMessages))
            Result.Error (new Exception(msg))
     
    let downloadPackageXmlsR (packageXmlInfos: Result<seq<PackageXmlInfo>,Exception>) = 
        match packageXmlInfos with
        |Ok pis -> downloadPackageXmls pis
        |Error ex -> Result.Error ex     

    let getModelInfoXmlFilePath (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) = 
        let fileName = String.Format("{0}_{1}.xml",modelCode.Value,operatingSystemCode.Value)
        let filePathString = getTempFilePath fileName        
        Path.create filePathString

    let getPackageInfo (downloadedPackageInfo : DownloadedPackageXmlInfo) =
        try
            Result.Ok (getPackageInfoUnsafe downloadedPackageInfo)
        with
        |ex -> Result.Error (new Exception(String.Format("Failed to get update info from '{0}'.",downloadedPackageInfo.FilePath.Value),ex))

    let parsePackageXmls (downloadedPackageXmls : seq<DownloadedPackageXmlInfo>) : seq<Result<PackageInfo,Exception>> = 
        downloadedPackageXmls
        |> Seq.map (fun pi -> (getPackageInfo pi))        

    let parsePackageXmlsR (downloadedPackageXmls: Result<seq<DownloadedPackageXmlInfo>,Exception>) : Result<seq<PackageInfo>,Exception> =
        let parsedUpdates = 
            match downloadedPackageXmls with
            |Ok pis -> (parsePackageXmls pis)
            |Error ex -> seq{yield Result.Error ex}
        
        let objectResults = 
                    parsedUpdates
                    //|> Seq.cast<Result<System.Object,Exception>>

        let allErrorMessages = getAllErrorMessages objectResults

        match allErrorMessages.Count() with
        | 0 ->  
                let allSuccesses = 
                    (getAllSuccesses objectResults)
                    |> Seq.cast<PackageInfo>                 
                Result.Ok allSuccesses
        | _ -> 
            let msg = String.Format("Failed to parse all package infos due to the following {0} error messages:{1}{2}",allErrorMessages.Count(),Environment.NewLine,String.Join(Environment.NewLine,allErrorMessages))
            Result.Error (new Exception(msg))

    let getRemoteUpdates (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) overwrite = 
        let modelInfoUri = getModelInfoUri modelCode operatingSystemCode
        let modelInfoXmlFilePath = getModelInfoXmlFilePath modelCode operatingSystemCode
        match modelInfoXmlFilePath with
        |Ok filePath -> 
                let result = 
                    filePath 
                    |> ensureFileDoesNotExist overwrite
                    |> downloadFile modelInfoUri
                    |> ensureFileExistsR
                    |> getPackagesInfo
                    |> downloadPackageXmlsR
                    |> parsePackageXmlsR
                result
        |Error ex -> Result.Error ex

    let getUnique list =
        match list with
        |Error ex -> Result.Error ex
        |Ok l -> 
            Result.Ok (l |> Seq.distinct)
    
    let exportToCsv (csvFilePath:Path) packageInfos : Result<Path,Exception> =
        try
            use sw = new System.IO.StreamWriter(csvFilePath.Value)
            use csv = new CsvHelper.CsvWriter(sw)
            csv.Configuration.Delimiter <- ";"
            csv.WriteRecords(packageInfos)
            Result.Ok csvFilePath
        with
        | ex -> Result.Error ex
    
    let exportToCsvR (csvFilePath:Path) packageInfos : Result<Path,Exception> =
        match packageInfos with
        |Error ex -> Result.Error ex
        |Ok pis -> exportToCsv csvFilePath pis

    let exportRemoteUpdates (model: ModelCode) (operatingSystem:OperatingSystemCode) csvFilePath overwrite =         
        let csvFileStatus = ensureFileDoesNotExist overwrite csvFilePath
        match csvFileStatus with
        |Error ex -> Result.Error ex
        |Ok csvPath ->
            getRemoteUpdates model operatingSystem overwrite
            |> getUnique
            |> exportToCsvR csvPath
        
        