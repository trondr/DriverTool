namespace DriverTool
open FileOperations

module ExportRemoteUpdates = 
    open System
    open System.Net
    open System.Security.Cryptography
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

    let getModelInfoXmlFilePath (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) = 
        Path.create (System.IO.Path.Combine(System.IO.Path.GetTempPath(), String.Format("{modelCode.Value}_{operatingSystemCode.Value}.xml",modelCode.Value,operatingSystemCode.Value)))

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

    let downloadFile (uri:Uri) (filePath:Path) :Result<Path,Exception> = 
        try
            downloadFileUnsafe uri filePath |> ignore   
            Result.Ok filePath
        with
        | ex -> Result.Error ex


    type PackageXmlInfo = 
        {Location:string; Category:string}



    let getPackagesInfo (modelInfoXmlFilePath:Path) : Result<IEnumerable<PackageXmlInfo>,Exception>= 
        try
            let testpi =  {Location = ""; Category =""}
            let testpis = seq{                    
                        yield testpi
                        }
            Result.Ok testpis
                
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
        
    let downloadPackageInfo packageXmlInfo = 
        let uri = new Uri(packageXmlInfo.Location)
        let tempXmlFileName = getTempXmlFilePathFromUri uri
        match tempXmlFileName with
        | Result.Ok p -> 
            downloadFile uri p               
        | Result.Error ex -> Result.Error ex

    //let downloadPackageXmls packageXmlInfos : IEnumerable<PackageXmlInfo> = 
    //    Seq.map packageXmlInfos (fun pi -> downloadPackageInfo pi)

    //let downloadPackageXmlsR packageXmlInfos : Result<IEnumerable<PackageXmlInfo>,Exception> =
    //    match packageXmlInfos with
    //    |Ok infos -> downloadPackageXmls infos
    //    |Error ex -> Result.Error ex
    

    //let getRemoteUpdates (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) overwrite = 
    //    let modelInfoUri = getModelInfoUri modelCode operatingSystemCode
    //    let modelInfoXmlFilePath = getModelInfoXmlFilePath modelCode operatingSystemCode
    //    match getModelInfoXmlFilePath with
    //    |Ok filePath-> 
    //        result = 
    //            filePath 
    //            |> ensureFileDoesNotExist overwrite
    //            |> downloadFile modelInfoUri
    //            |> ensureFileExists
    //            |> getPackagesInfo
    //            |> downloadPackageXmlsR
    //            |> parsePackageXmls
    //        result
    //    |Error ex -> Result.Error ex


    let exportRemoteUpdates (model: ModelCode) (operatingSystem:OperatingSystemCode) csvFilePath overwrite = 
        let path = ensureFileDoesNotExist overwrite csvFilePath
        match path with
        |Ok p -> 
            printf "Model: %s, OperatingSystem: %s, CsvPath: %s" model.Value operatingSystem.Value p.Value
            Path.create "C:\\Temp"
        |Error ex -> Result.Error ex
        
        