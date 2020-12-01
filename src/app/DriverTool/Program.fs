open System
open DriverTool.RunCommand

let resolveEventHandler =
    let lenovoSystemUpdateFolder=
        let programFilesFolderX86 = 
            System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        System.IO.Path.Combine(programFilesFolderX86,"Lenovo","System Update")

    let assemblySearchPaths =
        [|
            lenovoSystemUpdateFolder        
        |]
        
    let assemblyResolveHandler =
        let resourceAssembly = typeof<DriverTool.Init.ThisAssembly>.Assembly
        let resourceNameSpace = typeof<DriverTool.Init.ThisAssembly>.Namespace
        DriverTool.Library.AssemblyResolver.assemblyResolveHandlerPartial resourceAssembly resourceNameSpace assemblySearchPaths

    new ResolveEventHandler(fun s e -> assemblyResolveHandler(s,e))

let setup() =       
    AppDomain.CurrentDomain.add_AssemblyResolve(resolveEventHandler)
    AppDomain.CurrentDomain.UnhandledException.AddHandler(fun _ x -> printfn "%s" (x.ExceptionObject.ToString()))

let teardown() =
    AppDomain.CurrentDomain.remove_AssemblyResolve(resolveEventHandler)
    AppDomain.CurrentDomain.UnhandledException.RemoveHandler(fun _ x -> printfn "%s" (x.ExceptionObject.ToString()))

[<EntryPoint>]
[< STAThread >]
let main argv =
    setup()
    let exitCode = runCommand argv
    teardown()
    exitCode // return an integer exit code
