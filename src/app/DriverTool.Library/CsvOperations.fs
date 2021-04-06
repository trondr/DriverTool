namespace DriverTool.Library

module CsvOperations =
        
    open System.Globalization

    let exportToCsvUnsafe (csvFilePath:FileSystem.Path, records) =
        use sw = new System.IO.StreamWriter(FileSystem.pathValue csvFilePath)
        
        let csvConfiguration = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        csvConfiguration.Delimiter <- ";"
        use csv = new CsvHelper.CsvWriter(sw, csvConfiguration)
        csv.WriteRecords(records)
        csvFilePath
    
    let exportToCsv (csvFilePath:FileSystem.Path, records) =
        tryCatch (Some (sprintf "Failed to export records to csv file '%A'" csvFilePath)) exportToCsvUnsafe (csvFilePath, records)
        
