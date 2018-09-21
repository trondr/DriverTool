namespace DriverTool
open DriverTool.ExportRemoteUpdates

module CommandProviders =

    let ExportRemoteUdateInfo modelCodeString operatingSystemString csvFilePathString overwrite = 
        let modelCodeResult = ModelCode.create modelCodeString true
        let operatingSystemCodeResult = OperatingSystemCode.create operatingSystemString true
        let csvFilePathResult = Path.create csvFilePathString
        let parametersResult = validateExportRemoteUdateInfoParameters (modelCodeResult, operatingSystemCodeResult, csvFilePathResult)
        match parametersResult with
        |Ok parameters ->            
            let (modelCode, operatingSystemCode, csvFilePath) = parameters
            let result = exportRemoteUpdates modelCode operatingSystemCode csvFilePath overwrite
            match result with
            | Ok p -> NCmdLiner.Result.Ok(0)
            | Error ex -> NCmdLiner.Result.Fail<int>(ex)
        |Error ex -> NCmdLiner.Result.Fail<int>(ex)

        

