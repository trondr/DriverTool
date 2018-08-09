namespace DriverTool
open FileOperations
open DriverTool.ExportRemoteUpdates

module CommandProviders =
    open System    
    
    let validateParameters (modelCode:Result<ModelCode,Exception>, operatingSystemCode:Result<OperatingSystemCode,Exception>, csvPath:Result<Path,Exception>) = 
        match modelCode with
        |Ok m ->
            match operatingSystemCode with
            |Ok os ->
                match csvPath with
                |Ok fp -> Result.Ok (m, os, fp)
                |Error ex -> Result.Error ex
            |Error ex -> Result.Error ex
        |Error ex -> Result.Error ex

    
    let ExportRemoteUdateInfo modelCodeString operatingSystemString csvFilePathString overwrite = 
        let modelCodeResult = ModelCode.create modelCodeString true
        let operatingSystemCodeResult = OperatingSystemCode.create operatingSystemString true
        let csvFilePathResult = Path.create csvFilePathString
        let parameterResult = validateParameters (modelCodeResult, operatingSystemCodeResult, csvFilePathResult)
                
        match parameterResult with
        |Ok parameters ->            
            let (modelCode, operatingSystemCode, csvFilePath) = parameters
            let result = 
                csvFilePath 
                |> (ensureFileDoesNotExist overwrite)
                |> (exportRemoteUpdates modelCode operatingSystemCode)
            match result with
            | Ok p -> NCmdLiner.Result.Ok(0)
            | Error ex -> NCmdLiner.Result.Fail<int>(ex)
        |Error ex -> NCmdLiner.Result.Fail<int>(ex)

        

