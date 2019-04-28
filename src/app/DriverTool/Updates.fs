namespace DriverTool

open System
open DriverTool.ManufacturerTypes

module Updates =

    let getUpdatesFunc (manufacturer:Manufacturer,baseOnLocallyInstalledUpdates:bool) =
        match manufacturer with
        |Manufacturer.Dell _ -> 
            match baseOnLocallyInstalledUpdates with
            |true -> DellUpdates2.getLocalUpdates
            |false -> DellUpdates2.getRemoteUpdates
        |Manufacturer.HP _ -> 
            match baseOnLocallyInstalledUpdates with
            |true -> HpUpdates.getLocalUpdates
            |false -> HpUpdates.getRemoteUpdates
        |Manufacturer.Lenovo _ ->        
            match baseOnLocallyInstalledUpdates with
            |true -> LenovoUpdates.getLocalUpdates
            |false -> LenovoUpdates.getRemoteUpdates

    let updateDownloadedPackageInfoFunc (manufacturer:Manufacturer) =
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates2.updateDownloadedPackageInfo
        |Manufacturer.HP _ -> HpUpdates.updateDownloadedPackageInfo
        |Manufacturer.Lenovo _ -> LenovoUpdates.updateDownloadedPackageInfo

    let getSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates2.getSccmDriverPackageInfo
        |Manufacturer.HP _ -> HpUpdates.getSccmDriverPackageInfo
        |Manufacturer.Lenovo _ -> LenovoUpdates.getSccmDriverPackageInfo

        
    let downloadSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates2.downloadSccmPackage
        |Manufacturer.HP _ -> HpUpdates.downloadSccmPackage
        |Manufacturer.Lenovo _ -> LenovoUpdates.downloadSccmPackage
        
    let extractSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates2.extractSccmPackage
        |Manufacturer.HP _ -> HpUpdates.extractSccmPackage
        |Manufacturer.Lenovo _->  LenovoUpdates.extractSccmPackage
        
    let extractUpdateFunc (manufacturer:Manufacturer) = 
        match manufacturer with
        |Manufacturer.Dell _ -> DellUpdates2.extractUpdate
        |Manufacturer.HP _ -> HpUpdates.extractUpdate
        |Manufacturer.Lenovo _ -> LenovoUpdates.extractUpdate
