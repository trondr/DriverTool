namespace DriverTool.Library

module UpdatesContext =
    open System
    open System.Text.RegularExpressions
    open DriverTool.Library
    open DriverTool.Library.ManufacturerTypes
    
    type UpdatesRetrievalContext = {
        Manufacturer: Manufacturer
        Model:DriverTool.Library.ModelCode
        OperatingSystem: DriverTool.Library.OperatingSystemCode
        Overwrite: bool
        LogDirectory:FileSystem.Path
        CacheFolderPath:FileSystem.Path
        BaseOnLocallyInstalledUpdates:bool
        ExcludeUpdateRegexPatterns: Regex[]
    }

    let toUpdatesRetrievalContext manufacturer model operatingSystem overwrite logDirectory cacheFolderPath baseOnLocallyInstalledUpdates excludeUpdateRegexPatterns = 
        {
            Manufacturer = manufacturer
            Model = model
            OperatingSystem = operatingSystem
            Overwrite = overwrite
            LogDirectory = logDirectory
            CacheFolderPath = cacheFolderPath
            BaseOnLocallyInstalledUpdates = baseOnLocallyInstalledUpdates
            ExcludeUpdateRegexPatterns = excludeUpdateRegexPatterns
        }

    type SccmPackageInfoRetrievalContext = {
        Manufacturer: Manufacturer
        Model:DriverTool.Library.ModelCode
        OperatingSystem: DriverTool.Library.OperatingSystemCode
        CacheFolderPath:FileSystem.Path
        DoNotDownloadSccmPackage:bool
        SccmPackageInstaller:string
        SccmPackageReadme:string
        SccmPackageReleased:DateTime    
    }

    let toSccmPackageInfoRetrievalContext manufacturer model operatingSystem cacheFolderPath doNotDownloadSccmPackage sccmPackageInstaller sccmPackageReadme sccmPackageReleased =
        {
            Manufacturer = manufacturer
            Model = model
            OperatingSystem = operatingSystem
            CacheFolderPath=cacheFolderPath
            DoNotDownloadSccmPackage=doNotDownloadSccmPackage
            SccmPackageInstaller=sccmPackageInstaller
            SccmPackageReadme=sccmPackageReadme
            SccmPackageReleased=sccmPackageReleased        
        }