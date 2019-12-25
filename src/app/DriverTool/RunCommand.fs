namespace DriverTool

module RunCommand =
    open DriverTool.Commands
    open NCmdLiner
    
    open DriverTool.Library.Logging
    open System    
    open DriverTool.Library
    open DriverTool.Library.NCmdLinerMessenger
    
    let runCommandBase args =        
        configureLogging ()
        let logger = getLoggerByName "DriverTool"
        let assembly = System.Reflection.Assembly.GetExecutingAssembly()
        let version = assembly.GetName().Version.ToString()
        logger.Info(msg (sprintf "Start: DriverTool.%s. Command Line: %s" version System.Environment.CommandLine))
        logger.Info("ComputerName: " + System.Environment.MachineName)
        logger.Info("UserName: " + System.Environment.UserName)
        logger.Info("UserDomain: " + System.Environment.UserDomainName)
        logger.Info(msg (sprintf "UserInteractive: %b" System.Environment.UserInteractive))
        logger.Info(msg (sprintf "Is Administrator: %b" (Requirements.isAdministrator())))
        logger.Info(msg (sprintf "Is X64 bit Operating System: %b" System.Environment.Is64BitOperatingSystem))
        logger.Info("Process Bit: " + Environment.processBit)
        logger.Info(msg (sprintf "Is native process bit: %b (64 bit process on a 64 bit operating system, 32 bit process on a 32 bit operatings system)" Environment.isNativeProcessBit))
        use notepadMessenger = new NotepadMessenger()
        let result = NCmdLiner.CmdLinery.RunEx(typedefof<CommandDefinitions>, args,notepadMessenger)
        let exitCode = 
            match result.IsSuccess with
                |true -> result.Value
                |false ->                
                    result.OnFailure(new System.Action<exn>(fun ex -> logger.Error(msg (sprintf  "%A" ex))))|> ignore
                    1    
        logger.Info(msg (sprintf "Stop: DriverTool.%s Exit code: %i" version exitCode))
        exitCode
    
    let runCommand (args)=
        genericLogger LogLevel.Debug runCommandBase (args)

