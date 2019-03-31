namespace DriverToool

module UpdatesContext =
    
    type UpdatesRetrievalContext = {
        Model:DriverTool.ModelCode
        OperatingSystem: DriverTool.OperatingSystemCode
        Overwrite: bool
        LogDirectory:DriverTool.FileSystem.Path
    }

    let toUpdatesRetrievalContext model operatingSystem overwrite logDirectory = 
        {
            Model = model
            OperatingSystem = operatingSystem
            Overwrite = overwrite
            LogDirectory = logDirectory
        }

