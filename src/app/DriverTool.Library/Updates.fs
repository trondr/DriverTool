﻿namespace DriverTool

open System

open DriverTool.Library
open DriverTool.Library.ManufacturerTypes
open DriverTool.Library.PackageXml
open DriverTool.Library.Web
open DriverTool.Library.DriverPack

module Updates =

    let getUpdatesFunc (logger, manufacturer:Manufacturer,baseOnLocallyInstalledUpdates:bool) =
        match manufacturer with
        |Manufacturer.Dell _ -> 
            match baseOnLocallyInstalledUpdates with
            |true -> DellUpdates.getLocalUpdates logger
            |false -> DellUpdates.getRemoteUpdates logger
        |Manufacturer.HP _ -> 
            match baseOnLocallyInstalledUpdates with
            |true -> HpUpdates.getLocalUpdates logger
            |false -> HpUpdates.getRemoteUpdates logger
        |Manufacturer.Lenovo _ ->        
            match baseOnLocallyInstalledUpdates with
            |true -> LenovoUpdates.getLocalUpdates logger
            |false -> LenovoUpdates.getRemoteUpdates logger

    ///Get driver updates function
    let getDriverUpdatesFunc (manufacturer:Manufacturer) =
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.getDriverUpdates
        |Manufacturer.HP _ -> HpUpdates.getDriverUpdates
        |Manufacturer.Lenovo _ -> LenovoUpdates.getDriverUpdates

    let isDriverUpdateRequiredFunc (manufacturer:Manufacturer) =
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.isDriverUpdateRequired
        |Manufacturer.HP _ -> HpUpdates.isDriverUpdateRequired
        |Manufacturer.Lenovo _ -> LenovoUpdates.isDriverUpdateRequired

    let updateDownloadedPackageInfoFunc (manufacturer:Manufacturer) =
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.updateDownloadedPackageInfo
        |Manufacturer.HP _ -> HpUpdates.updateDownloadedPackageInfo
        |Manufacturer.Lenovo _ -> LenovoUpdates.updateDownloadedPackageInfo

    let getSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.getSccmDriverPackageInfo
        |Manufacturer.HP _ -> HpUpdates.getSccmDriverPackageInfo
        |Manufacturer.Lenovo _ -> LenovoUpdates.getSccmDriverPackageInfo

    let getDriverPacksFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.getDriverPackInfos
        |Manufacturer.HP _ -> HpUpdates.getDriverPackInfos
        |Manufacturer.Lenovo _ -> LenovoUpdates.getDriverPackInfos
        
    let downloadSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.downloadSccmPackage
        |Manufacturer.HP _ -> HpUpdates.downloadSccmPackage
        |Manufacturer.Lenovo _ -> LenovoUpdates.downloadSccmPackage

    let downloadDriverPackInfo cacheFolderPath reportProgress (driverPack:DriverPackInfo) =
        result{            
            let! installerInfo = Web.downloadWebFile logger reportProgress cacheFolderPath driverPack.InstallerFile
            let installerPath = FileSystem.pathValue installerInfo.DestinationFile
            let! readmePath =
                match driverPack.ReadmeFile with
                |Some readmeFile ->
                    result{                        
                        let! readmeInfo = Web.downloadWebFile logger reportProgress cacheFolderPath readmeFile
                        let readmePath = FileSystem.pathValue readmeInfo.DestinationFile
                        return Some readmePath
                    }
                |None -> Result.Ok None
            return {
                InstallerPath = installerPath
                ReadmePath = readmePath
                DriverPack = driverPack;
            }            
        }

    let downloadDriverPackInfoFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> downloadDriverPackInfo
        |Manufacturer.HP _ -> downloadDriverPackInfo
        |Manufacturer.Lenovo _ -> downloadDriverPackInfo
        
    let extractSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.extractSccmPackage
        |Manufacturer.HP _ -> HpUpdates.extractSccmPackage
        |Manufacturer.Lenovo _->  LenovoUpdates.extractSccmPackage
        
    let extractDriverPackInfoFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.extractDriverPackInfo
        |Manufacturer.HP _ -> HpUpdates.extractDriverPackInfo
        |Manufacturer.Lenovo _->  LenovoUpdates.extractDriverPackInfo


    let extractUpdateFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.extractUpdate
        |Manufacturer.HP _ -> HpUpdates.extractUpdate
        |Manufacturer.Lenovo _ -> LenovoUpdates.extractUpdate
