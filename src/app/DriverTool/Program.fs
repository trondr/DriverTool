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

[<EntryPoint>]
let main argv =
    let exitCode = runCommand argv
    Console.WriteLine("Press any key...")
    Console.ReadLine() |> ignore
    exitCode // return an integer exit code
