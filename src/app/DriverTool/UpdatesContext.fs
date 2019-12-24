namespace DriverTool

module UpdatesContext =
    open System.Text.RegularExpressions
    open DriverTool.Library
    
    type UpdatesRetrievalContext = {
        Model:DriverTool.Library.ModelCode
        OperatingSystem: DriverTool.Library.OperatingSystemCode
        Overwrite: bool
        LogDirectory:FileSystem.Path
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

