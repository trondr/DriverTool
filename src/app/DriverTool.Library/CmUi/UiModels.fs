namespace DriverTool.Library.CmUi
open DriverTool.Library

module UiModels =
    
    open System
    open Microsoft.FSharp.Reflection
    open DriverTool.Library.PackageXml

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

    /// Package CM drivers
    let packageSccmPackage (cacheFolderPath:FileSystem.Path) (reportProgress:(bool->float option->string->unit)) (cmPackage:CmPackage) : Result<DownloadedCmPackage,Exception> =
        result{
            logger.Warn(sprintf "TODO: Packaging '%s'..." cmPackage.Model)
            let! manufacturer = ManufacturerTypes.manufacturerStringToManufacturer(cmPackage.Manufacturer,false)
            reportProgress true None (sprintf "TODO: Download CM Drivers for model %s" cmPackage.Model)            
            let downloadCmPackage = DriverTool.Updates.downloadCmPackageFunc manufacturer
            let! downloadedCmPackage = downloadCmPackage (cacheFolderPath,cmPackage)
            reportProgress true None (sprintf "TODO: Extract CM Drivers for model %s" cmPackage.Model)
            reportProgress true None (sprintf "TODO: Package CM Drivers for model %s" cmPackage.Model)
            let! notImplemented = Result.Error (toException "Not implemented" None)            
            return notImplemented
        }

        
        
