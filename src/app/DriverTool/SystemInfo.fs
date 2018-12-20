namespace DriverTool

module SystemInfo=

    type ManufacturerName = |Dell = 1 |Lenovo =2

    open F
    open System.Text.RegularExpressions
    open System

    let arrayToSeq (array:Array) =
        seq{
            for item in array do
                yield item
        }

    let getEnumValuesToString (enumType) =
        let enumValues = 
            Enum.GetValues(enumType)
            |>arrayToSeq
            |>Seq.map(fun v -> v.ToString())
            |>Seq.toArray

        "[" + String.Join("|",enumValues) + "]"
    
    let getValidManufacturerNames () =
        getEnumValuesToString (typeof<ManufacturerName>)
    
    let getWmiManufacturerForCurrentSystem () =
        (WmiHelper.getWmiProperty "Win32_ComputerSystem" "Manufacturer")
    
    type WmiManufacturerValueFunc = unit -> Result<string,Exception>

    let manufacturerToManufacturerNameUnsafe (manufacturer:string) =
        match manufacturer with
        |manufacturerName when Regex.Match(manufacturerName,"Dell",RegexOptions.IgnoreCase).Success -> ManufacturerName.Dell
        |manufacturerName when Regex.Match(manufacturerName,"Lenovo",RegexOptions.IgnoreCase).Success -> ManufacturerName.Lenovo
        |_ -> raise (new Exception(String.Format("Manufacturer '{0}' is not supported. Supported manufacturers: {1}",manufacturer, getValidManufacturerNames())))        

    let manufacturerToManufacturerName (manufacturer:string) =
        tryCatch manufacturerToManufacturerNameUnsafe manufacturer

    let getManufacturerForCurrentSystemBase (wmiManufacturerValueFunc:WmiManufacturerValueFunc) =
        result
                {
                    let! manufacturer = wmiManufacturerValueFunc ()                    
                    return! manufacturerToManufacturerName manufacturer                        
                }

    let getManufacturerForCurrentSystem () =
        getManufacturerForCurrentSystemBase getWmiManufacturerForCurrentSystem
