open System
open DriverTool.RunCommand
open DriverTool

let resolveEventHandler =
    new ResolveEventHandler(fun s e -> DriverTool.AssemblyResolver.assemblyResolveHandler(s,e)) 

let setup() =       
    AppDomain.CurrentDomain.add_AssemblyResolve(resolveEventHandler)
    AppDomain.CurrentDomain.UnhandledException.AddHandler(fun _ x -> System.Console.WriteLine(x.ExceptionObject.ToString()))
let teardown() =
    AppDomain.CurrentDomain.remove_AssemblyResolve(resolveEventHandler)
    AppDomain.CurrentDomain.UnhandledException.RemoveHandler(fun _ x -> System.Console.WriteLine(x.ExceptionObject.ToString()))

[<EntryPoint>]
[< STAThread >]
let main argv =
    setup()
    let exitCode = runCommand argv
    teardown()
    exitCode // return an integer exit code
