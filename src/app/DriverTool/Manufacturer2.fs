namespace DriverTool

module ManufacturerTypes =

    open System
    open Microsoft.FSharp.Reflection
    open System.Text.RegularExpressions

    type Manufacturer2 =
        |Dell of name:string
        |Lenovo of name:string
    
    let getValidManufacturers () = 
        FSharpType.GetUnionCases typeof<Manufacturer2>
        |>Array.map(fun case -> case.Name)
    
    let getValidManufacturersString () =
        String.Join ("|",getValidManufacturers ())
    
    let getWmiManufacturerForCurrentSystem () =
        (WmiHelper.getWmiProperty "Win32_ComputerSystem" "Manufacturer")
    
    type InvalidManufacturerException (msg:string) =
        inherit Exception((sprintf "%s %s" msg (sprintf "Supported manufacturers: %s." (getValidManufacturersString()) )))

    type WmiManufacturerValueFunc = unit -> Result<string,Exception>
    
    let rec manufacturerStringToManufacturerUnsafeBase (wmiManufacturerValueFunc:WmiManufacturerValueFunc, manufacturer:string, defaultToLocal) =
        match manufacturer with
        |manufacturerName when (String.IsNullOrWhiteSpace(manufacturerName) && defaultToLocal) -> 
            match(wmiManufacturerValueFunc()) with
            |Ok m -> manufacturerStringToManufacturerUnsafeBase (wmiManufacturerValueFunc,m, false)
            |Error ex -> raise ex
        |manufacturerName when Regex.Match(manufacturerName,"Dell",RegexOptions.IgnoreCase).Success -> Manufacturer2.Dell "Dell"
        |manufacturerName when Regex.Match(manufacturerName,"Lenovo",RegexOptions.IgnoreCase).Success -> Manufacturer2.Lenovo "Lenovo"
        |_ -> raise (new InvalidManufacturerException(sprintf "Manufacturer '%s' is not supported." manufacturer))
     
    let manufacturerStringToManufacturerBase (wmiManufacturerValueFunc:WmiManufacturerValueFunc,manufacturer:string,defaultToLocal) =
        tryCatch manufacturerStringToManufacturerUnsafeBase (wmiManufacturerValueFunc,manufacturer,defaultToLocal)

    let manufacturerStringToManufacturer (manufacturer:string,defaultToLocal) =
        manufacturerStringToManufacturerBase (getWmiManufacturerForCurrentSystem,manufacturer,defaultToLocal)
    
    let manufacturerToName (manufacturer:Manufacturer2) =
        match manufacturer with
        |Dell name -> name
        |Lenovo name -> name