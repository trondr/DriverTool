namespace DriverTool.Library

module DriverUpdates =
    
    open DriverTool.Library.PackageXml

    //Model info record defintion
    type ModelInfo = {
        Manufacturer: string
        ModelCode : string
        Name : string
        OperatingSystem : string
        OsBuild : string
        DriverUpdates : PackageInfo []
    }

    //Construct a model info record
    let toModelInfo manufacturer modelCode name operatingSystem osBuild driverUpdates =
        {
            Manufacturer = manufacturer
            ModelCode = modelCode
            Name = name
            OperatingSystem = operatingSystem
            OsBuild = osBuild
            DriverUpdates = driverUpdates
        }

    ///Load driver updates for manufacturer, model and operating system
    let loadDriverUpdates reportProgress cacheFolderPath manufacturer model modelName operatingSystem osBuild excludeUpdateRegexPatterns =
        result{                        
            let driverUpdatesFunction = DriverTool.Updates.getDriverUpdatesFunc manufacturer            
            let! driverPackInfos = driverUpdatesFunction reportProgress cacheFolderPath model operatingSystem excludeUpdateRegexPatterns
            let uniqueDriverUpdates = driverPackInfos |> Array.distinct
            let uniqueDriverUpdatesByInstallerName = uniqueDriverUpdates |> DriverTool.CreateDriverPackage.getUniqueUpdatesByInstallerName
            let modelInfo = toModelInfo (ManufacturerTypes.manufacturerToName manufacturer) (model.ToString()) modelName (operatingSystem.ToString()) osBuild uniqueDriverUpdatesByInstallerName
            return modelInfo
        }

