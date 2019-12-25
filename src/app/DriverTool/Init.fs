namespace DriverTool

[<AutoOpen>]
module Init =
    type ThisAssembly = { Empty:string;}

    let resourceAssembly =
        typeof<ThisAssembly>.Assembly