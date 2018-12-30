namespace DriverTool

open System
open DriverTool.SystemInfo

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

