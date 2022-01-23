namespace DriverTool

module ExportRemoteUpdates = 
    open DriverTool
    open DriverTool.Library.ManufacturerTypes
    open DriverTool.Library.UpdatesContext
    open DriverTool.Library.F
    open DriverTool.Library
    open DriverTool.Library.FileOperations
       
    let exportRemoteUpdates cacheFolderPath logger (manufacturer:Manufacturer) (model: ModelCode) (operatingSystem:OperatingSystemCode) csvFilePath overwrite excludeUpdatePatterns =         
        result {
            let! logDirectory = FileSystem.path DriverTool.Library.Configuration.getDriverPackageLogDirectoryPath
            let! excludeUpdateRegexPatterns = RegExp.toRegexPatterns true excludeUpdatePatterns 
            let updatesRetrievalContext = toUpdatesRetrievalContext manufacturer model operatingSystem overwrite logDirectory cacheFolderPath false excludeUpdateRegexPatterns            
            let! csvFilePath = ensureFileDoesNotExist overwrite csvFilePath
            let getUpdates = DriverTool.Updates.getUpdatesFunc (logger, manufacturer, updatesRetrievalContext.BaseOnLocallyInstalledUpdates)
            let! r = getUpdates cacheFolderPath updatesRetrievalContext
            let u = Seq.distinct r
            let! e = CsvOperations.exportToCsv (csvFilePath, u)
            return e
        }        
        
        