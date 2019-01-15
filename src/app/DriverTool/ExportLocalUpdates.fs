namespace DriverTool


module ExportLocalUpdates =
    let logger = Logging.getLoggerByName "ExportLocalUpdates"

    open System

    let exportLocalUpdates (csvFilePath:Path) =
        result{       
            let! localManufacturer = DriverTool.ManufacturerTypes.manufacturerStringToManufacturer ("",true) 
            let! localModelCode = ModelCode.create String.Empty true
            let! localOperatingSystemCode = OperatingSystemCode.create String.Empty true
            let getUpdates = DriverTool.Updates.getUpdates (localManufacturer, true)
            let! localUpdates = getUpdates (localModelCode, localOperatingSystemCode, true, DriverTool.Configuration.getDriverPackageLogDirectoryPath)
            let! exportResult = DriverTool.CsvOperations.exportToCsv (csvFilePath, localUpdates)
            logger.Info("Locally installed updates have been exported to file: " + csvFilePath.Value)
            return exportResult            
        }

