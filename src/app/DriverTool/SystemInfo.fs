namespace DriverTool

module SystemInfo=

    open F
    open System

    open DriverTool.ManufacturerTypes

    let getModelCodeForCurrentSystem () : Result<string,Exception> =
        result{
            let! manufacturer = DriverTool.ManufacturerTypes.getManufacturerForCurrentSystem ()
            let! modelCode = 
                match manufacturer with
                |Manufacturer.Dell _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "SystemSKUNumber"
                |Manufacturer.Lenovo _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "Model"
                |Manufacturer.HP _ -> WmiHelper.getWmiProperty "root\WMI" "MS_SystemInformation" "BaseBoardProduct"
            return modelCode            
        } 

    let getSystemFamilyForCurrentSystem () : Result<string,Exception> =
        result{
            let! manufacturer = DriverTool.ManufacturerTypes.getManufacturerForCurrentSystem ()
            let! systemFamily = 
                match manufacturer with
                |Manufacturer.Dell _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "Model"
                |Manufacturer.Lenovo _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "SystemFamily"
                |Manufacturer.HP _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "Model"
            return systemFamily            
        } 