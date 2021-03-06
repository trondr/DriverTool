﻿namespace DriverTool.Library
module Run=
    
    open NCmdLiner
    open DriverTool.Library.Logging

    let run applicationName applicationVersion (logger:Common.Logging.ILog) (runex: unit -> Result<int>)  =
        logger.Info(sprintf "Start: %s.%s. Command Line: %s" applicationName applicationVersion System.Environment.CommandLine)
        logger.Info("ComputerName: " + System.Environment.MachineName)
        logger.Info("UserName: " + System.Environment.UserName)
        logger.Info("UserDomain: " + System.Environment.UserDomainName)
        logger.Info(sprintf "UserInteractive: %b" System.Environment.UserInteractive)
        logger.Info(sprintf "Is Administrator: %b" (Environment.isAdministrator()) )
        logger.Info(sprintf "Is X64 bit Operating System: %b" System.Environment.Is64BitOperatingSystem)
        logger.Info("Process Bit: " + DriverTool.Library.Environment.processBit)
        logger.Info(sprintf "Is native process bit: %b (64 bit process on a 64 bit operating system, 32 bit process on a 32 bit operatings system)" DriverTool.Library.Environment.isNativeProcessBit)
        let result = runex()
        let exitCode = 
            match result.IsSuccess with
                |true -> result.Value
                |false ->                
                    result.OnFailure(new System.Action<exn>(fun ex -> logger.Error(sprintf  "%A" ex)))|> ignore
                    1    
        logger.Info(sprintf "Stop: %s.%s Exit code: %i" applicationName applicationVersion exitCode)
        exitCode

