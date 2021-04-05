namespace DriverTool.Library.CmUi
open DriverTool.Library

module UiModels =
    
    open System
    open Microsoft.FSharp.Reflection
    open DriverTool.Library

    let getCacheFolderPath () =
        result{
            let! cacheFolderPath = FileSystem.path DriverTool.Library.Configuration.downloadCacheDirectoryPath
            let! existingCacheFolderPath = DirectoryOperations.ensureDirectoryExists true cacheFolderPath
            return existingCacheFolderPath
        }

    let loadSccmPackages (cacheFolderPath:FileSystem.Path) =
        result{            
            let manufacturers = FSharpType.GetUnionCases typeof<ManufacturerTypes.Manufacturer>
            let updateFunctions = manufacturers|> Array.map(fun m -> 
                                                                let manufacturer = FSharpValue.MakeUnion(m,[|(m.Name:>obj)|]):?> ManufacturerTypes.Manufacturer
                                                                let getFunc = DriverTool.Updates.getSccmPackagesFunc manufacturer
                                                                getFunc                                                            
                                                            )
            let! sccmPackagesArray = updateFunctions |> Array.map (fun f -> f(cacheFolderPath)) |> toAccumulatedResult
            let sccmpackages = sccmPackagesArray |> Seq.toArray |> Array.concat
            return sccmpackages
        }
    