namespace DriverTool.x86.Host
        
module RunCommand =
    open System    
    open NCmdLiner
    open DriverTool.x86.Host.CommandDefinitions
    open DriverTool.Library
    open DriverTool.Library.Logging            
    open DriverTool.Library.NCmdLinerMessenger
    
    let runCommandBase args =        
        configureLogging ()
        let logger = getLoggerByName "DriverTool.x86.Host"
        let assembly = System.Reflection.Assembly.GetExecutingAssembly()
        let version = assembly.GetName().Version.ToString()
        logger.Info(msg (sprintf "Start: DriverTool.x86.Host.%s. Command Line: %s" version System.Environment.CommandLine))
        logger.Info("ComputerName: " + System.Environment.MachineName)
        logger.Info("UserName: " + System.Environment.UserName)
        logger.Info("UserDomain: " + System.Environment.UserDomainName)
        logger.Info(msg (sprintf "UserInteractive: %b" System.Environment.UserInteractive))
        logger.Info(msg (sprintf "Is Administrator: %b" (Environment.isAdministrator()) ))
        logger.Info(msg (sprintf "Is X64 bit Operating System: %b" System.Environment.Is64BitOperatingSystem))
        logger.Info("Process Bit: " + DriverTool.Library.Environment.processBit)
        logger.Info(msg (sprintf "Is native process bit: %b (64 bit process on a 64 bit operating system, 32 bit process on a 32 bit operatings system)" DriverTool.Library.Environment.isNativeProcessBit))
        use notepadMessenger = new NotepadMessenger()
        let result = NCmdLiner.CmdLinery.RunEx(typedefof<CommandDefinitions>, args,notepadMessenger)
        let exitCode = 
            match result.IsSuccess with
                |true -> result.Value
                |false ->                
                    result.OnFailure(new System.Action<exn>(fun ex -> logger.Error(msg (sprintf  "%A" ex))))|> ignore
                    1    
        logger.Info(msg (sprintf "Stop: DriverTool.x86.Host.%s Exit code: %i" version exitCode))
        exitCode
    
    let runCommand args =
        Logging.genericLogger Logging.LogLevel.Info runCommandBase args
