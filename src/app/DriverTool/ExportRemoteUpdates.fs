﻿namespace DriverTool
open FileOperations

module ExportRemoteUpdates = 
    open System
    open DriverTool
    open DriverTool.LenovoUpdates

    let exportToCsv (csvFilePath:Path) packageInfos : Result<Path,Exception> =
        try
            use sw = new System.IO.StreamWriter(csvFilePath.Value)
            use csv = new CsvHelper.CsvWriter(sw)
            csv.Configuration.Delimiter <- ";"
            csv.WriteRecords(packageInfos)
            Result.Ok csvFilePath
        with
        | ex -> 
            Result.Error (new Exception(String.Format("Failed to export package infos to csv file '{0}' due to: {1}.",csvFilePath.Value, ex.Message),ex))
    
    let exportRemoteUpdates (model: ModelCode) (operatingSystem:OperatingSystemCode) csvFilePath overwrite =         
        result {
            let! csvFilePath = ensureFileDoesNotExist (overwrite, csvFilePath)    
            let! r = getRemoteUpdates (model, operatingSystem, overwrite)
            let u = Seq.distinct r
            let! e = exportToCsv csvFilePath u
            return e
        }        
        
        