namespace DriverTool
open System

module ExportRemoteUpdates = 
    open FileOperations
    
    type Model =
        | ModelCodeResult of Result<ModelCode,Exception>
        | ModelCode of ModelCode

    type OperatingSystem =
        | OperatingSystemCodeResult of Result<OperatingSystemCode,Exception>
        | OperatingSystemCode of OperatingSystemCode
    

    //exportRemoteUpdatesMO     00
    //exportRemoteUpdatesMOR    01
    //exportRemoteUpdatesMRO    10
    //exportRemoteUpdatesMROR   11

    let exportRemoteUpdatesMO (model: ModelCode) (operatingSystem:OperatingSystemCode) =
        printf "Model: %s, OperatingSystem: %s" model.Value operatingSystem.Value
        Path.create "C:\\Temp"
           
    let exportRemoteUpdatesMRO modelResult operatingSystem =
        match modelResult with
        | Ok m -> exportRemoteUpdatesMO m operatingSystem
        | Error ex -> Result.Error ex
    
    let exportRemoteUpdatesMOR model operatingSystemResult =
        match operatingSystemResult with
        |Result.Ok os -> exportRemoteUpdatesMO model os
        |Error ex -> Result.Error ex       

    let exportRemoteUpdatesMROR modelResult operatingSystemResult =
        match modelResult with
        | Ok m -> exportRemoteUpdatesMOR m operatingSystemResult
        | Error ex -> Result.Error ex

    let exportRemoteUpdates model operatingSystem =
        match model with
        | ModelCodeResult(mr) -> 
            match operatingSystem with
            | OperatingSystemCodeResult(osr) -> exportRemoteUpdatesMROR mr osr
            | OperatingSystemCode(os) -> exportRemoteUpdatesMRO mr os
        | ModelCode(m) -> 
            match operatingSystem with
            | OperatingSystemCodeResult(osr) -> exportRemoteUpdatesMOR m osr
            | OperatingSystemCode(os) -> exportRemoteUpdatesMO m os
        