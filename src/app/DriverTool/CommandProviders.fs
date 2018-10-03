﻿namespace DriverTool
open F

module CommandProviders =

    let exportRemoteUdateInfoSimple (modelCodeString, operatingSystemString, csvFilePathString, overwrite) = 
        match (result {
                let! modelCode = ModelCode.create modelCodeString true
                let! operatingSystemCode = OperatingSystemCode.create operatingSystemString true
                let! csvFilePath = Path.create csvFilePathString
                let result = DriverTool.ExportRemoteUpdates.exportRemoteUpdates modelCode operatingSystemCode csvFilePath overwrite
                return result
            }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let exportRemoteUdateInfo =
        Logging.debugLogger exportRemoteUdateInfoSimple
    
    let createDriverPackageSimple (modelCodeString,operatingSystemString) =
        match (result {
                let! modelCode = ModelCode.create modelCodeString true
                let! operatingSystemCode = OperatingSystemCode.create operatingSystemString true
                let createDriverPackageResult = DriverTool.CreateDriverPackage.createDriverPackage modelCode operatingSystemCode
                return createDriverPackageResult
            }) with
        | Ok _ -> NCmdLiner.Result.Ok(0)
        | Error ex -> NCmdLiner.Result.Fail<int>(ex)

    let createDriverPackage =
        Logging.debugLogger createDriverPackageSimple