namespace DriverTool

open System
open DriverTool.ManufacturerTypes

module Updates =

    let getUpdatesFunc (manufacturer:Manufacturer2,baseOnLocallyInstalledUpdates:bool) =
        match manufacturer with
        |Manufacturer2.Dell _ -> 
            match baseOnLocallyInstalledUpdates with
            |true -> DellUpdates.getLocalUpdates
            |false -> DellUpdates.getRemoteUpdates
        |Manufacturer2.HP _ -> 
            match baseOnLocallyInstalledUpdates with
            |true -> HpUpdates.getLocalUpdates
            |false -> HpUpdates.getRemoteUpdates
        |Manufacturer2.Lenovo _ ->        
            match baseOnLocallyInstalledUpdates with
            |true -> LenovoUpdates.getLocalUpdates
            |false -> LenovoUpdates.getRemoteUpdates

    let updateDownloadedPackageInfoFunc (manufacturer:Manufacturer2) =
        match manufacturer with
        |Manufacturer2.Dell _ -> 
            DellUpdates.updateDownloadedPackageInfo
        |Manufacturer2.HP _ -> 
            HpUpdates.updateDownloadedPackageInfo
        |Manufacturer2.Lenovo _ -> 
            LenovoUpdates.updateDownloadedPackageInfo

    let getSccmPackageFunc (manufacturer:Manufacturer2) = 
        match manufacturer with
        |Manufacturer2.Dell _ -> DellUpdates.getSccmDriverPackageInfo
        |Manufacturer2.HP _ -> HpUpdates.getSccmDriverPackageInfo
        |Manufacturer2.Lenovo _ -> LenovoUpdates.getSccmDriverPackageInfo

        
    let downloadSccmPackageFunc (manufacturer:Manufacturer2) = 
        match manufacturer with
        |Manufacturer2.Dell _ -> DellUpdates.downloadSccmPackage
        |Manufacturer2.HP _ -> HpUpdates.downloadSccmPackage
        |Manufacturer2.Lenovo _ -> LenovoUpdates.downloadSccmPackage
        
    let extractSccmPackageFunc (manufacturer:Manufacturer2) = 
        match manufacturer with
        |Manufacturer2.Dell _ -> DellUpdates.extractSccmPackage
        |Manufacturer2.HP _ -> HpUpdates.extractSccmPackage
        |Manufacturer2.Lenovo _->  LenovoUpdates.extractSccmPackage
        
    let extractUpdateFunc (manufacturer:Manufacturer2) = 
        match manufacturer with
        |Manufacturer2.Dell _ -> DellUpdates.extractUpdate
        |Manufacturer2.HP _ -> HpUpdates.extractUpdate
        |Manufacturer2.Lenovo _ -> LenovoUpdates.extractUpdate
