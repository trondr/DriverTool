open System

let resolveEventHandler =
    new ResolveEventHandler(fun s e -> DriverTool.Library.AssemblyResolver.assemblyResolveHandler(s,e))

let setup() =       
    AppDomain.CurrentDomain.add_AssemblyResolve(resolveEventHandler)
    AppDomain.CurrentDomain.UnhandledException.AddHandler(fun _ x -> printfn "%s" (x.ExceptionObject.ToString()))
let teardown() =
    AppDomain.CurrentDomain.remove_AssemblyResolve(resolveEventHandler)
    AppDomain.CurrentDomain.UnhandledException.RemoveHandler(fun _ x -> printfn "%s" (x.ExceptionObject.ToString()))

[<EntryPoint>]
let main argv = 
    let exitCode = DriverTool.x86.Host.RunCommand.runCommand argv
    exitCode
