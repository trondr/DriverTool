open System
open DriverTool.Commands
open NCmdLiner

let runCommand args =
    let result = NCmdLiner.CmdLinery.RunEx(typedefof<CommandDefinitions>, args)
    let exitCode = 
        match result.IsSuccess with
            |true -> 0
            |false ->                
                result.OnFailure(new Action<exn>(fun ex -> System.Console.WriteLine(ex.Message)))|> ignore
                1
    exitCode

open System.Reflection
open DriverTool

let loadAssemblyFromSearchPath assemblyName (searchPaths : seq<string>) =
    searchPaths
    |> Seq.map (fun p -> 
            let assemblyPath = System.IO.Path.Combine(p,assemblyName + ".dll")
            assemblyPath
        )
    |> Seq.filter (fun p -> System.IO.File.Exists(p))    
    |> Seq.map (fun p -> Assembly.LoadFile(p))
    |> Seq.head

let resolveEventHandler (obj:System.Object) (resolveEventArgs: ResolveEventArgs) : Assembly =
    let assemblyName = new AssemblyName(resolveEventArgs.Name)
    let searchPaths = seq{ yield DriverTool.LenovoSystemUpdate.systemUpdateFolderPathString }
    let assembly = loadAssemblyFromSearchPath assemblyName.Name searchPaths
    assembly

[<EntryPoint>]
let main argv =
    let exitCode = runCommand argv
    Console.ReadLine() |> ignore
    exitCode // return an integer exit code
