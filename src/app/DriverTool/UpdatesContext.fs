namespace DriverToool

module UpdatesContext =
    open System.Text.RegularExpressions
    
    type UpdatesRetrievalContext = {
        Model:DriverTool.ModelCode
        OperatingSystem: DriverTool.OperatingSystemCode
        Overwrite: bool
        LogDirectory:DriverTool.FileSystem.Path
        ExcludeUpdateRegexPatterns: Regex[]
    }

    let toUpdatesRetrievalContext model operatingSystem overwrite logDirectory excludeUpdateRegexPatterns = 
        {
            Model = model
            OperatingSystem = operatingSystem
            Overwrite = overwrite
            LogDirectory = logDirectory
            ExcludeUpdateRegexPatterns = excludeUpdateRegexPatterns
        }

