namespace DriverTool

module RunCommand =
    open DriverTool.Commands
    open NCmdLiner
    open System
    open DriverTool.Logging

    let runCommandBase args =        
        configureLogging ()
        let logger = getLoggerByName "DriverTool"
        logger.Info("Start: DriverTool. Command Line: " + Environment.CommandLine)
        let result = NCmdLiner.CmdLinery.RunEx(typedefof<CommandDefinitions>, args)    
        let exitCode = 
            match result.IsSuccess with
                |true -> 0
                |false ->                
                    result.OnFailure(new Action<exn>(fun ex -> logger.Error(ex.ToString())))|> ignore
                    1    
        logger.Info("Stop: DriverTool. Exit code: " + exitCode.ToString())
        exitCode
    
    let runCommand (args)=
        debugLogger runCommandBase (args)

