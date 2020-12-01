namespace DriverTool


module ExportLocalUpdates =
    let logger = DriverTool.Library.Logging.getLoggerByName "ExportLocalUpdates"

    open System
    open DriverTool.Library.UpdatesContext
    open DriverTool.Library.F
    open DriverTool.Library

    let exportLocalUpdates cacheFolderPath (csvFilePath:FileSystem.Path) excludeUpdatePatterns =
        result{       
            let! localManufacturer = DriverTool.Library.ManufacturerTypes.manufacturerStringToManufacturer ("",true) 
            let! localModelCode = ModelCode.create String.Empty true
            let! localOperatingSystemCode = OperatingSystemCode.create String.Empty true
            
            let! logDirectory = FileSystem.path DriverTool.Library.Configuration.getDriverPackageLogDirectoryPath
            let! excludeUpdateRegexPatterns = RegExp.toRegexPatterns excludeUpdatePatterns true
            let updatesRetrievalContext = toUpdatesRetrievalContext localManufacturer localModelCode localOperatingSystemCode true logDirectory cacheFolderPath true excludeUpdateRegexPatterns
            let getUpdates = DriverTool.Updates.getUpdatesFunc (logger,localManufacturer, updatesRetrievalContext.BaseOnLocallyInstalledUpdates)
            let! localUpdates = getUpdates cacheFolderPath updatesRetrievalContext
            let! exportResult = DriverTool.Library.CsvOperations.exportToCsv (csvFilePath, localUpdates)
            logger.Info("Locally installed updates have been exported to file: " + FileSystem.pathValue csvFilePath)
            return exportResult            
        }

