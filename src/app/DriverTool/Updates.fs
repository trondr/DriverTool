namespace DriverTool

open System
open DriverTool.SystemInfo
open DriverTool.ManufacturerTypes

module Updates =
    let getUpdatesFunc (manufacturer:Manufacturer,baseOnLocallyInstalledUpdates:bool) = 
        match manufacturer.Value with
        |ManufacturerName.Dell ->        
            match baseOnLocallyInstalledUpdates with
            |true -> DellUpdates.getLocalUpdates
            |false -> DellUpdates.getRemoteUpdates
        |ManufacturerName.Lenovo ->        
            match baseOnLocallyInstalledUpdates with
            |true -> LenovoUpdates.getLocalUpdates
            |false -> LenovoUpdates.getRemoteUpdates
        |_ -> raise(new Exception("Manufacturer not supported: " + manufacturer.Value.ToString()))

    let getUpdates (manufacturer:Manufacturer2,baseOnLocallyInstalledUpdates:bool) =
        match manufacturer with
        |Manufacturer2.Dell _ -> 
            match baseOnLocallyInstalledUpdates with
            |true -> DellUpdates.getLocalUpdates
            |false -> DellUpdates.getRemoteUpdates
        |Manufacturer2.Lenovo _ ->        
            match baseOnLocallyInstalledUpdates with
            |true -> LenovoUpdates.getLocalUpdates
            |false -> LenovoUpdates.getRemoteUpdates

    let getSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer.Value with
        |ManufacturerName.Dell ->        
            DellUpdates.getSccmDriverPackageInfo
        |ManufacturerName.Lenovo ->        
            LenovoUpdates.getSccmDriverPackageInfo
        |_ -> raise(new Exception("Manufacturer not supported: " + manufacturer.Value.ToString()))
    
    let downloadSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer.Value with
        |ManufacturerName.Dell ->        
            DellUpdates.downloadSccmPackage
        |ManufacturerName.Lenovo ->        
            LenovoUpdates.downloadSccmPackage
        |_ -> raise(new Exception("Manufacturer not supported: " + manufacturer.Value.ToString()))

    let extractSccmPackageFunc (manufacturer:Manufacturer) = 
        match manufacturer.Value with
        |ManufacturerName.Dell ->        
            DellUpdates.extractSccmPackage
        |ManufacturerName.Lenovo ->        
            LenovoUpdates.extractSccmPackage
        |_ -> raise(new Exception("Manufacturer not supported: " + manufacturer.Value.ToString()))

