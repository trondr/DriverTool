namespace DriverTool

open System
open DriverTool.ManufacturerTypes

module Updates =

    let getUpdates (manufacturer:Manufacturer2,baseOnLocallyInstalledUpdates:bool) =
        match manufacturer with
        |Manufacturer2.Dell _ -> 
            match baseOnLocallyInstalledUpdates with
            |true -> raise (new NotImplementedException("Getting locally installed Dell updates is not implemented"))
            |false -> DellUpdates.getRemoteUpdates
        |Manufacturer2.Lenovo _ ->        
            match baseOnLocallyInstalledUpdates with
            |true -> LenovoUpdates.getLocalUpdates
            |false -> LenovoUpdates.getRemoteUpdates

    let getSccmPackageFunc (manufacturer:Manufacturer2) = 
        match manufacturer with
        |Manufacturer2.Dell _ -> DellUpdates.getSccmDriverPackageInfo
        |Manufacturer2.Lenovo _ -> LenovoUpdates.getSccmDriverPackageInfo
        
    let downloadSccmPackageFunc (manufacturer:Manufacturer2) = 
        match manufacturer with
        |Manufacturer2.Dell _ -> DellUpdates.downloadSccmPackage
        |Manufacturer2.Lenovo _ -> LenovoUpdates.downloadSccmPackage
        
    let extractSccmPackageFunc (manufacturer:Manufacturer2) = 
        match manufacturer with
        |Manufacturer2.Dell _ -> DellUpdates.extractSccmPackage
        |Manufacturer2.Lenovo _->  LenovoUpdates.extractSccmPackage
        
    let extractUpdateFunc (manufacturer:Manufacturer2) = 
        match manufacturer with
        |Manufacturer2.Dell _ -> DellUpdates.extractUpdate
        |Manufacturer2.Lenovo _ -> LenovoUpdates.extractUpdate
