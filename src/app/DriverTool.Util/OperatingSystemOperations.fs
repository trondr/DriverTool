namespace DriverTool.Util

module OperatingSystemOperations =
    open DriverTool.Util.FSharp
    
    let GetOsShortName =
        OperatingSystem.getOsShortName

    let IsServer =
        OperatingSystem.isServer
    
    let IsX64 =
        OperatingSystem.isOperatingSystemX64


