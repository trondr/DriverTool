namespace DriverTool

module RunCommand =
    open DriverTool.Commands
    open NCmdLiner
    
    open DriverTool.Logging
    open System
    open System.IO
    
    type NotepadMessenger () =
        let tempFileName = 
            let tempFile = System.IO.Path.GetTempFileName()
            let txtTempFile = tempFile + ".txt"
            System.IO.File.Move(tempFile, txtTempFile)
            txtTempFile
        let streamWriter = 
            new StreamWriter(tempFileName)
        do            
            ()
        
        interface IDisposable with
            member this.Dispose() =                
                streamWriter.Dispose()                
                match (FileSystem.path tempFileName) with
                |Ok fp ->  (FileOperations.deleteFileIfExists fp)
                |Result.Error ex -> ()

        interface IMessenger with
            member x.Write (formatMessage:string,args:obj[]) =
                streamWriter.Write(formatMessage.Replace("\r\n","\n").Replace("\n",Environment.NewLine),args)
                ()
            member x.WriteLine (formatMessage:string,args:obj[]) =
                streamWriter.WriteLine(formatMessage.Replace("\r\n","\n").Replace("\n",Environment.NewLine),args)
                ()
            member x.Show () =
                streamWriter.Close()
                if(Environment.UserInteractive) then
                    System.Diagnostics.Process.Start(tempFileName) |> ignore
                    System.Threading.Thread.Sleep(2000)
                else
                    use sr = new StreamReader(tempFileName)
                    System.Console.WriteLine(sr.ReadToEnd())
                ()
                

    let runCommandBase args =        
        configureLogging ()
        let logger = getLoggerByName "DriverTool"
        let version = 
            typeof<F0.ThisAssembly>.Assembly.GetName().Version.ToString()
        logger.Info(sprintf "Start: DriverTool.%s. Command Line: %s" version System.Environment.CommandLine)
        logger.Info("ComputerName: " + System.Environment.MachineName)
        logger.Info("UserName: " + System.Environment.UserName)
        logger.Info("UserDomain: " + System.Environment.UserDomainName)
        logger.Info("UserInteractive: " + System.Environment.UserInteractive.ToString())
        logger.Info("Is Administrator: " + Requirements.isAdministrator().ToString())
        logger.Info("Is X64 bit Operating System: " + System.Environment.Is64BitOperatingSystem.ToString())
        logger.Info("Process Bit: " + Environment.processBit)
        logger.Info("Is native process bit: " + Environment.isNativeProcessBit.ToString() + "(64 bit process on a 64 bit operating system, 32 bit process on a 32 bit operatings system)")
        use notepadMessenger = new NotepadMessenger()
        let result = NCmdLiner.CmdLinery.RunEx(typedefof<CommandDefinitions>, args,notepadMessenger)
        let exitCode = 
            match result.IsSuccess with
                |true -> result.Value
                |false ->                
                    result.OnFailure(new System.Action<exn>(fun ex -> logger.Error(ex.ToString())))|> ignore
                    1    
        logger.Info(sprintf "Stop: DriverTool.%s Exit code: %s" version  (exitCode.ToString()))
        exitCode
    
    let runCommand (args)=
        Logging.genericLogger Logging.LogLevel.Debug runCommandBase (args)

