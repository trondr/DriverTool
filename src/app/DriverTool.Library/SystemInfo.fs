namespace DriverTool.Library

module SystemInfo=
    open System
    open DriverTool.Library.ManufacturerTypes
    open DriverTool.Library.F

    let getModelCodeForCurrentSystem () : Result<string,Exception> =
        result{
            let! manufacturer = DriverTool.Library.ManufacturerTypes.getManufacturerForCurrentSystem ()
            let! modelCode = 
                match manufacturer with
                |Manufacturer.Dell _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "SystemSKUNumber"
                |Manufacturer.Lenovo _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "Model"
                |Manufacturer.HP _ -> WmiHelper.getWmiProperty "root\WMI" "MS_SystemInformation" "BaseBoardProduct"
#if DEBUG
                |Manufacturer.Unknown _ -> WmiHelper.getWmiProperty "root\WMI" "MS_SystemInformation" "BaseBoardProduct"
#endif
            return modelCode            
        } 

    let getSystemFamilyForCurrentSystem () : Result<string,Exception> =
        result{
            let! manufacturer = DriverTool.Library.ManufacturerTypes.getManufacturerForCurrentSystem ()
            let! systemFamily = 
                match manufacturer with
                |Manufacturer.Dell _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "Model"
                |Manufacturer.Lenovo _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "SystemFamily"
                |Manufacturer.HP _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "Model"
#if DEBUG
                |Manufacturer.Unknown _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "Model"
#endif
            return systemFamily            
        } 