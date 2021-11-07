namespace DriverTool.Library

module DriverPacks = 
    open Microsoft.FSharp.Reflection
    open DriverTool.Library.ManufacturerTypes
    open DriverTool.Library.Logging

    ///Load driver pack infos for all manufacturers   
    let loadDriverPackInfos (cacheFolderPath:FileSystem.Path) (reportProgress:reportProgressFunction) =
        result{                        
            let updateFunctions = 
                getValidManufacturers()
                |> Array.map (DriverTool.Updates.getDriverPacksFunc)
            let! driverPackInfosArrayofArrays = updateFunctions |> Array.map (fun f -> f cacheFolderPath reportProgress) |> toAccumulatedResult
            let driverPackInfos = driverPackInfosArrayofArrays |> Seq.toArray |> Array.concat
            return driverPackInfos
        }