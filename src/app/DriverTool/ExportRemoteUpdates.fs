namespace DriverTool
open FileOperations

module ExportRemoteUpdates = 
    open System
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

    let operatingSystemCode2DownloadableCode (operatingSystemCode: OperatingSystemCode) =
        operatingSystemCode.Value.Replace("X86","").Replace("x86","").Replace("X64","").Replace("x64","")
    
    let modelCode2DownloadableCode (modelCode: ModelCode) =
        modelCode.Value.Substring(0,4)
     
    let getModelInfoUri (modelCode: ModelCode) (operatingSystemCode: OperatingSystemCode) = 
        new Uri(String.Format("https://download.lenovo.com/catalog/{0}_{1}.xml", (modelCode2DownloadableCode modelCode), (operatingSystemCode2DownloadableCode operatingSystemCode)))

    open FSharp.Data    
    open DriverTool
    open DriverTool.PackageXml

    type PackagesXmlProvider = XmlProvider<"https://download.lenovo.com/catalog/20FA_Win7.xml">
    type PackageXmlProvider = XmlProvider<"https://download.lenovo.com/pccbbs/mobiles/n1cx802w_2_.xml">
    
    let getPackagesInfo (modelInfoXmlFilePath:Path) : Result<seq<PackageXmlInfo>,Exception>= 
        try
            let x = PackagesXmlProvider.Load(modelInfoXmlFilePath.Value)
            x.Packages
            |> Seq.map (fun p -> { Location = p.Location; Category = p.Category; CheckSum = p.Checksum.Value})
            |> Result.Ok            
        with
        |ex -> Result.Error ex

    let getPackagesInfoR (modelInfoXmlFilePath:Result<Path,Exception>) : Result<seq<PackageXmlInfo>,Exception>= 
        try
            match modelInfoXmlFilePath with
            |Ok p -> 
                let x = PackagesXmlProvider.Load(p.Value)
                x.Packages
                |> Seq.map (fun p -> { Location = p.Location; Category = p.Category; CheckSum = p.Checksum.Value})
                |> Result.Ok
            |Error ex -> Result.Error ex
        with
        |ex -> Result.Error ex
    
    let getTempPath =
        try
            DriverTool.Configuration.getDownloadCacheDirectoryPath
        with
        | _ as ex -> 
            System.Console.WriteLine("Failed to get download cache directory due to " + ex.Message + "Using %TEMP% path instead.")
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
    open DriverTool.Web

    let getBaseUrl locationUrl =
        let uri = new Uri(locationUrl)
        uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments.Last().Length).Trim('/');
    
    let packageXmlInfo2downloadedPackageXmlInfo (packageXmlInfo:PackageXmlInfo, filePath) =
        {
            Location = packageXmlInfo.Location;
            Category = packageXmlInfo.Category;
            FilePath = filePath;
            BaseUrl = getBaseUrl packageXmlInfo.Location;
            CheckSum = packageXmlInfo.CheckSum;
        }
    open DriverTool.Checksum

    let verifyDownload (sourceUri:Uri, destinationFile, checksum, fileSize, verificationWarningOnly) =
        match (hasSameFileHash (destinationFile, checksum, fileSize)) with
        |true  -> Result.Ok destinationFile
        |false -> 
            let msg = String.Format("Destination file ('{0}') hash does not match source file ('{1}') hash.",destinationFile,sourceUri.OriginalString)
            match verificationWarningOnly with
            |true ->
                Logging.getLoggerByName("verifyDownload").Warn(msg)
                Result.Ok destinationFile
            |false->Result.Error (new Exception(msg))

    let downloadPackageInfo (packageXmlInfo:PackageXmlInfo) = 
            result {
                let sourceUri = new Uri(packageXmlInfo.Location)
                let! destinationFilePath = getTempXmlFilePathFromUri sourceUri
                let downloadInfo = {SourceUri=sourceUri;SourceChecksum=packageXmlInfo.CheckSum;SourceFileSize=0L;DestinationFile=destinationFilePath;}
                let! downloadInfo2 = downloadIfDifferent (downloadInfo, false)                             
                let dpi = packageXmlInfo2downloadedPackageXmlInfo (packageXmlInfo, downloadInfo2.DestinationFile)
                return dpi
            }
    
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

    

    let getRemoteUpdatesBase (modelCode: ModelCode, operatingSystemCode: OperatingSystemCode, overwrite) =
        result{
            let modelInfoUri = getModelInfoUri modelCode operatingSystemCode
            let! path = getModelInfoXmlFilePath modelCode operatingSystemCode
            let! modelInfoXmlFilePath = ensureFileDoesNotExist (overwrite, path)
            let! downloadedFile = downloadFile (modelInfoUri, overwrite, modelInfoXmlFilePath)            
            let! packageXmlInfos = getPackagesInfo downloadedFile
            let! downloadedPackageXmls = downloadPackageXmls packageXmlInfos
            let! packageInfos = 
                (parsePackageXmls downloadedPackageXmls)
                |>toAccumulatedResult
            return packageInfos
        }

    let getRemoteUpdates (modelCode: ModelCode, operatingSystemCode: OperatingSystemCode, overwrite) =
        Logging.genericLoggerResult Logging.LogLevel.Debug getRemoteUpdatesBase (modelCode, operatingSystemCode, overwrite)

    let exportToCsv (csvFilePath:Path) packageInfos : Result<Path,Exception> =
        try
            use sw = new System.IO.StreamWriter(csvFilePath.Value)
            use csv = new CsvHelper.CsvWriter(sw)
            csv.Configuration.Delimiter <- ";"
            csv.WriteRecords(packageInfos)
            Result.Ok csvFilePath
        with
        | ex -> 
            Result.Error (new Exception(String.Format("Failed to export package infos to csv file '{0}' due to: {1}.",csvFilePath.Value, ex.Message),ex))
    
    let exportRemoteUpdates (model: ModelCode) (operatingSystem:OperatingSystemCode) csvFilePath overwrite =         
        result {
            let! csvFilePath = ensureFileDoesNotExist (overwrite, csvFilePath)    
            let! r = getRemoteUpdates (model, operatingSystem, overwrite)
            let u = getUnique r
            let! e = exportToCsv csvFilePath u
            return e
        }        
        
        