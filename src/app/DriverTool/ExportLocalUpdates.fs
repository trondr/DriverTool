namespace DriverTool


module ExportLocalUpdates =
    let logger = Logging.getLoggerByName "ExportLocalUpdates"

    open System

    let exportLocalUpdates (csvFilePath:Path) =
        result{       
            let! actualModelCode = ModelCode.create String.Empty true
            let! actualOperatingSystemCode = OperatingSystemCode.create String.Empty true
            let! localUpdates = LenovoUpdates.getLocalUpdates (actualModelCode, actualOperatingSystemCode, true,DriverTool.Configuration.getDriverPackageLogDirectoryPath)
            let! exportResult = DriverTool.CsvOperations.exportToCsv (csvFilePath, localUpdates)
            logger.Info("Locally installed updates have been exported to file: " + csvFilePath.Value)
            return exportResult            
        }

