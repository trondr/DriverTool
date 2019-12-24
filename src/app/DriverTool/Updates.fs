namespace DriverTool

open System
open DriverTool.Library.ManufacturerTypes

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

        
    let downloadSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.downloadSccmPackage
        |Manufacturer.HP _ -> HpUpdates.downloadSccmPackage
        |Manufacturer.Lenovo _ -> LenovoUpdates.downloadSccmPackage
        
    let extractSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.extractSccmPackage
        |Manufacturer.HP _ -> HpUpdates.extractSccmPackage
        |Manufacturer.Lenovo _->  LenovoUpdates.extractSccmPackage
        
    let extractUpdateFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates.extractUpdate
        |Manufacturer.HP _ -> HpUpdates.extractUpdate
        |Manufacturer.Lenovo _ -> LenovoUpdates.extractUpdate
