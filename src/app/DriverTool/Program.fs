open System
open DriverTool.Commands
open NCmdLiner
open DriverTool
open Logging

let logger = getLoggerByName "DriverTool"

let runCommandSimple args =        
    let result = NCmdLiner.CmdLinery.RunEx(typedefof<CommandDefinitions>, args)    
    let exitCode = 
        match result.IsSuccess with
            |true -> 0
            |false ->                
                result.OnFailure(new Action<exn>(fun ex -> logger.Error(ex.Message)))|> ignore
                1    
    exitCode

let runCommand (args)=
    debugLogger runCommandSimple (args)

[<EntryPoint>]
[< STAThread >]
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
