﻿namespace DriverTool

module SystemInfo=

    open F
    open System

    open DriverTool.ManufacturerTypes

    let getModelCodeForCurrentSystem () :Result<string,Exception> =
        result{
            let! manufacturer = DriverTool.ManufacturerTypes.getManufacturerForCurrentSystem ()
            let! modelCode = 
                match manufacturer with
                |Manufacturer2.Dell _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "SystemSKUNumber"
                |Manufacturer2.Lenovo _ -> WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "Model"
                |Manufacturer2.HP _ -> WmiHelper.getWmiProperty "root\WMI" "MS_SystemInformation" "BaseBoardProduct"
            return modelCode            
        } 