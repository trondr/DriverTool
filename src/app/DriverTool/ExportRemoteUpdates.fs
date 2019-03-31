namespace DriverTool
open FileOperations

module ExportRemoteUpdates = 
    open DriverTool
    open DriverTool.ManufacturerTypes
    open DriverToool.UpdatesContext
       
    let exportRemoteUpdates (manufacturer:Manufacturer2) (model: ModelCode) (operatingSystem:OperatingSystemCode) csvFilePath overwrite =         
        result {
            let! csvFilePath = ensureFileDoesNotExist overwrite csvFilePath
            let getUpdates = DriverTool.Updates.getUpdatesFunc (manufacturer, false)
            let! logDirectory = FileSystem.path DriverTool.Configuration.getDriverPackageLogDirectoryPath
            let updatesRetrievalContext = toUpdatesRetrievalContext model operatingSystem overwrite logDirectory
            let! r = getUpdates updatesRetrievalContext
            let u = Seq.distinct r
            let! e = CsvOperations.exportToCsv (csvFilePath, u)
            return e
        }        
        
        