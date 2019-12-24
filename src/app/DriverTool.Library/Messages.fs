namespace DriverTool.Library

module Messages =
    
    type LenovoSccmPackageInfoRequestMessage() =
        let mutable sourceUri = ""
        member this.SourceUri with get() = sourceUri and set(value) = sourceUri <- value