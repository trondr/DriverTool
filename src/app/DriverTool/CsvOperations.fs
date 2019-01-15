namespace DriverTool

module CsvOperations =
    let exportToCsvUnsafe (csvFilePath:Path, records) =
        use sw = new System.IO.StreamWriter(csvFilePath.Value)
        use csv = new CsvHelper.CsvWriter(sw)
        csv.Configuration.Delimiter <- ";"
        csv.WriteRecords(records)
        csvFilePath
    
    let exportToCsv (csvFilePath:Path, records) =
        tryCatch exportToCsvUnsafe (csvFilePath, records)
