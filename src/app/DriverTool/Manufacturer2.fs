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

    type InvalidManufacturerException (msg:string) =
        inherit Exception((sprintf "%s %s" msg (sprintf "Supported manufacturers: %s." (getValidManufacturersString()) )))

    let manufacturerStringToManufacturerUnsafe (manufacturer:string) =
        match manufacturer with
        |manufacturerName when Regex.Match(manufacturerName,"Dell",RegexOptions.IgnoreCase).Success -> Manufacturer2.Dell "Dell"
        |manufacturerName when Regex.Match(manufacturerName,"Lenovo",RegexOptions.IgnoreCase).Success -> Manufacturer2.Lenovo "Lenovo"
        |_ -> raise (new InvalidManufacturerException(sprintf "Manufacturer '%s' is not supported." manufacturer))
    
    let manufacturerStringToManufacturer (manufacturer:string) =
        tryCatch manufacturerStringToManufacturerUnsafe manufacturer