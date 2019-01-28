namespace DriverTool
open FileOperations

module ExportRemoteUpdates = 
    open DriverTool
    open DriverTool.ManufacturerTypes
       
    let exportRemoteUpdates (manufacturer:Manufacturer2) (model: ModelCode) (operatingSystem:OperatingSystemCode) csvFilePath overwrite =         
        result {
            let! csvFilePath = ensureFileDoesNotExist overwrite csvFilePath
            let getUpdates = DriverTool.Updates.getUpdates (manufacturer, false)
            let! r = getUpdates (model, operatingSystem, overwrite,DriverTool.Configuration.getDriverPackageLogDirectoryPath)
            let u = Seq.distinct r
            let! e = CsvOperations.exportToCsv (csvFilePath, u)
            return e
        }        
        
        