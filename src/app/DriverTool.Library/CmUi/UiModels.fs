namespace DriverTool.Library.CmUi

module UiModels =
    
    open System

    type CmPackage = {
        Manufacturer:string
        Model: string
        ModelCodes: string[]
        ReadmeFile:DriverTool.Library.Web.WebFile
        InstallerFile:DriverTool.Library.Web.WebFile        
        Released:DateTime;
        Os:string;
        OsBuild:string
        WmiQuery:string
    }

