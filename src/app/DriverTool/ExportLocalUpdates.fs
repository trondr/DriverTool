namespace DriverTool


module ExportLocalUpdates =
    let logger = Logging.getLoggerByName "ExportLocalUpdates"

    open System
    open DriverToool.UpdatesContext

    let exportLocalUpdates (csvFilePath:FileSystem.Path) =
        result{       
            let! localManufacturer = DriverTool.ManufacturerTypes.manufacturerStringToManufacturer ("",true) 
            let! localModelCode = ModelCode.create String.Empty true
            let! localOperatingSystemCode = OperatingSystemCode.create String.Empty true
            let getUpdates = DriverTool.Updates.getUpdatesFunc (localManufacturer, true)
            
            let updatesRetrievalContext : UpdatesRetrievalContext = 
                    {
                        Model = localModelCode
                        OperatingSystem = localOperatingSystemCode
                        Overwrite = true
                        LogDirectory = DriverTool.Configuration.getDriverPackageLogDirectoryPath
                    }
            
            let! localUpdates = getUpdates updatesRetrievalContext
            let! exportResult = DriverTool.CsvOperations.exportToCsv (csvFilePath, localUpdates)
            logger.Info("Locally installed updates have been exported to file: " + FileSystem.pathValue csvFilePath)
            return exportResult            
        }

