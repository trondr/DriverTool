namespace DriverTool
open FileOperations

module ExportRemoteUpdates = 
    open System
    
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

    let exportRemoteUpdates (model: ModelCode) (operatingSystem:OperatingSystemCode) csvFilePath overwrite = 
        let path = ensureFileDoesNotExist overwrite csvFilePath
        match path with
        |Ok p -> 
            printf "Model: %s, OperatingSystem: %s, CsvPath: %s" model.Value operatingSystem.Value p.Value
            Path.create "C:\\Temp"
        |Error ex -> Result.Error ex
        
        