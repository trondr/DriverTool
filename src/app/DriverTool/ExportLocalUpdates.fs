namespace DriverTool


module ExportLocalUpdates =
    let logger = Logging.getLoggerByName "ExportLocalUpdates"

    open System
    open DriverToool.UpdatesContext

    let exportLocalUpdates (csvFilePath:FileSystem.Path) excludeUpdatePatterns =
        result{       
            let! localManufacturer = DriverTool.ManufacturerTypes.manufacturerStringToManufacturer ("",true) 
            let! localModelCode = ModelCode.create String.Empty true
            let! localOperatingSystemCode = OperatingSystemCode.create String.Empty true
            let getUpdates = DriverTool.Updates.getUpdatesFunc (localManufacturer, true)
            let! logDirectory = FileSystem.path DriverTool.Configuration.getDriverPackageLogDirectoryPath
            let! excludeUpdateRegexPatterns = RegExp.toRegexPatterns excludeUpdatePatterns true
            let updatesRetrievalContext = toUpdatesRetrievalContext localModelCode localOperatingSystemCode true logDirectory excludeUpdateRegexPatterns
            let! localUpdates = getUpdates updatesRetrievalContext
            let! exportResult = DriverTool.CsvOperations.exportToCsv (csvFilePath, localUpdates)
            logger.Info("Locally installed updates have been exported to file: " + FileSystem.pathValue csvFilePath)
            return exportResult            
        }

