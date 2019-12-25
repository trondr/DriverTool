namespace DriverTool

module RunCommand =
    open DriverTool.Commands
    open DriverTool.Library
    open DriverTool.Library.Logging            
    open DriverTool.Library.NCmdLinerMessenger
    open DriverTool.Library.Run

    let applicationVersion = 
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()

    let applicationName =
        "DriverTool"

    let runCommandBase args =        
        configureLogging ()
        let logger = getLoggerByName applicationName        
        let exitCode = run applicationName applicationVersion logger (fun () -> NCmdLiner.CmdLinery.RunEx(typedefof<CommandDefinitions>, args, new NotepadMessenger()))        
        exitCode
    
    let runCommand args =
        Logging.genericLogger Logging.LogLevel.Info runCommandBase args
