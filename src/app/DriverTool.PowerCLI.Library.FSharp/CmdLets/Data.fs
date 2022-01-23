namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

module Data =    
    open DriverTool.Library
    open DriverTool.Library.Logging
    
    let reportProgressSilently:reportProgressFunction = (fun activity status currentOperation percentComplete isBusy id -> 
        ()//Do not report progress
    )
    
    let getAllDriverPacks () =
        match(result{
            let! cacheFolder = DriverTool.Library.FileSystem.path (DriverTool.Library.Configuration.getDownloadCacheDirectoryPath())                
            let! driverPackInfos = DriverTool.Library.DriverPacks.loadDriverPackInfos cacheFolder reportProgressSilently
            return driverPackInfos
        })with
        |Result.Ok dps -> dps
        |Result.Error ex -> 
            raise ex
            
    let allDriverPacks = lazy (getAllDriverPacks ())

    ///Get model name
    let getModelName manufacturer modelCode (operatingSystem:string) =
        let model = 
            allDriverPacks.Value
            |>Array.filter(fun dp -> dp.Manufacturer = manufacturer)
            |>Array.filter(fun dp -> dp.ModelCodes|>Array.contains modelCode)
            |>Array.filter(fun dp -> dp.Os.ToUpper() = operatingSystem.ToUpper())
            |>Array.tryHead
        match model with
        |Some m -> m.Model
        |None -> modelCode

    let getModelCodes manufacturer (driverPacks:DriverPack.DriverPackInfo[]) =
        driverPacks
        |>Array.filter(fun dp -> dp.Manufacturer = manufacturer)
        |>Array.map(fun dp -> dp.ModelCodes |> Array.map(fun m -> m))
        |>Array.collect id

    let getOperatingSystems manufacturer modelCode (driverPacks:DriverPack.DriverPackInfo[]) =
        driverPacks
        |>Array.filter(fun dp -> dp.Manufacturer = manufacturer)
        |>Array.filter(fun dp ->  dp.ModelCodes |> Array.contains modelCode)
        |>Array.map(fun dp-> dp.Os)
        |>Array.distinct

    let getOsBuild manufacturer modelCode operatingSystem (driverPacks:DriverPack.DriverPackInfo[]) =
        driverPacks
        |>Array.filter(fun dp -> dp.Manufacturer = manufacturer)
        |>Array.filter(fun dp ->  dp.ModelCodes |> Array.contains modelCode)
        |>Array.filter(fun dp -> dp.Os = operatingSystem)
        |>Array.map(fun dp-> dp.OsBuild)
        |>Array.distinct
    

    

