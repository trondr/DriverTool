namespace DriverTool
open FileOperations

module CommandProviders =
    
    
    let ExportRemoteUdateInfo modelCodeString operatingSystemString csvFilePath overwrite = 
        let modelCode = ModelCode.create modelCodeString
        let operatingSystemCode = OperatingSystemCode.create operatingSystemString
        //let csvPath = PathResult (Path.create csvFilePath)
        //let result = ensureFileDoesNotExist csvPath true
        let result = 
            PathResult (Path.create csvFilePath) 
            |> (ensureFileDoesNotExist overwrite)
            //|> (exportRemoteUpdates modelCode operatingSystemCode)
        match result with
        | Ok p -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

        

