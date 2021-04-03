namespace DriverTool.Library.CmUi
open DriverTool.Library

module UiModels =
    
    open System
    open Microsoft.FSharp.Reflection
    open DriverTool.Library

    type CmPackage = {
        Manufacturer:string
        Model: string
        ModelCodes: string[]
        ReadmeFile:DriverTool.Library.Web.WebFile
        InstallerFile:DriverTool.Library.Web.WebFile        
        Released:DateTime;
        Os:string;
        OsBuild:string
        WmiQuery:string
    }

    let loadSccmPackages () =
        let manufacturers = FSharpType.GetUnionCases typeof<ManufacturerTypes.Manufacturer>
        let updateFunctions = manufacturers|> Array.map(fun m -> 
                                                            let manufacturer = FSharpValue.MakeUnion(m,[|(m.Name:>obj)|]):?> ManufacturerTypes.Manufacturer
                                                            let getFunc = DriverTool.Updates.getSccmPackagesFunc manufacturer
                                                            getFunc
                                                        )
        result{
            let! sccmPackagesArray = updateFunctions |> Array.map (fun f -> f()) |> toAccumulatedResult
            let sccmpackages = sccmPackagesArray |> Seq.toArray |> Array.concat
            return sccmpackages
        }