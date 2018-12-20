namespace DriverTool

module SystemInfo=

    open F
    open System.Text.RegularExpressions
    open System

    type ManufacturerName = |Dell = 1 |Lenovo =2

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

    let getModelCodeForCurrentSystem () =
        result{
            let! manufacturer = getManufacturerForCurrentSystem()
            let! modelCode = 
                match manufacturer with
                |ManufacturerName.Dell -> WmiHelper.getWmiProperty "Win32_ComputerSystem" "SystemSKUNumber"
                |ManufacturerName.Lenovo -> WmiHelper.getWmiProperty "Win32_ComputerSystem" "Model"
                |_ -> Result.Error (new Exception("Manufacturer not supported."))
            return modelCode            
        } 
