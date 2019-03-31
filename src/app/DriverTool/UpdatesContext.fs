namespace DriverToool

module UpdatesContext =
    
    type UpdatesRetrievalContext = {
        Model:DriverTool.ModelCode
        OperatingSystem: DriverTool.OperatingSystemCode
        Overwrite: bool
        LogDirectory:string
    }

