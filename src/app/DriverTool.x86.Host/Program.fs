open System

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
        let resourceAssembly = typeof<DriverTool.x86.Host.Init.ThisAssembly>.Assembly
        let resourceNameSpace = typeof<DriverTool.x86.Host.Init.ThisAssembly>.Namespace
        DriverTool.Library.AssemblyResolver.assemblyResolveHandlerPartial resourceAssembly resourceNameSpace assemblySearchPaths

    new ResolveEventHandler(fun s e -> assemblyResolveHandler(s,e))

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
