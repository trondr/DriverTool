namespace DriverTool.Library

module DriverPack =
    open System

    type WmiQuery = {
        Name:string
        NameSpace :string
        Query :string
    }

    ///Information about an enterprise driver pack. An enterprise driver pack contains all drivers for a model. These drivers are "INF" based drivers and consequently injectable using the DISM utility.
    type DriverPackInfo = {
        Manufacturer:string
        Model: string
        ModelCodes: string[]
        ReadmeFile:DriverTool.Library.Web.WebFile option
        InstallerFile:DriverTool.Library.Web.WebFile
        Released:DateTime
        Os:string
        OsBuild:string
        ModelWmiQuery:WmiQuery
        ManufacturerWmiQuery:WmiQuery
    }

    type DownloadedDriverPackInfo = { InstallerPath:string; ReadmePath:string option; DriverPack:DriverPackInfo}
    
    type ExtractedDriverPackInfoInfo = { ExtractedDirectoryPath:string; DownloadedDriverPackInfo:DownloadedDriverPackInfo;}