open System
open DriverTool.Commands
open NCmdLiner
open DriverTool
open Logging

type DriverTool = String
let logger = getLogger<DriverTool>

let runCommand args =        
    let result = NCmdLiner.CmdLinery.RunEx(typedefof<CommandDefinitions>, args)
    let exitCode = 
        match result.IsSuccess with
            |true -> 0
            |false ->                
                result.OnFailure(new Action<exn>(fun ex -> logger.Error(ex.Message)))|> ignore
                1    
    exitCode

[<EntryPoint>]
let main argv =
    configureLogging
    logger.Info("Start: DriverTool. Command Line: " + Environment.CommandLine)
    let exitCode = runCommand argv
    logger.Info("Stop: DriverTool. Exit code: " + exitCode.ToString())
#if DEBUG
    Console.WriteLine("Press any key...")
    Console.ReadLine() |> ignore
#endif
    exitCode // return an integer exit code
