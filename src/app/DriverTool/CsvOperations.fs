namespace DriverTool

module CsvOperations =
    open DriverTool.Library.F
    open DriverTool.Library

    let exportToCsvUnsafe (csvFilePath:FileSystem.Path, records) =
        use sw = new System.IO.StreamWriter(FileSystem.pathValue csvFilePath)
        use csv = new CsvHelper.CsvWriter(sw)
        csv.Configuration.Delimiter <- ";"
        csv.WriteRecords(records)
        csvFilePath
    
    let exportToCsv (csvFilePath:FileSystem.Path, records) =
        tryCatch exportToCsvUnsafe (csvFilePath, records)
