namespace DriverTool

module ExportRemoteUpdates = 
    open System
    
    let exportRemoteUpdates (model: ModelCode) (operatingSystem:OperatingSystemCode) (csvFilePath:Result<Path,Exception>) = 
        match csvFilePath with
        |Ok p -> 
            printf "Model: %s, OperatingSystem: %s, CsvPath: %s" model.Value operatingSystem.Value p.Value
            Path.create "C:\\Temp"
        |Error ex -> Result.Error ex
        
        