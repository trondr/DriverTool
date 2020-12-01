namespace DriverTool.Library

module OperatingSystemOperations =
    
    let GetOsShortName () =
        OperatingSystem.getOsShortName

    let IsServer () =
        OperatingSystem.isServer
    
    let IsX64 () =
        OperatingSystem.isOperatingSystemX64


