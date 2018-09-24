﻿namespace DriverTool
        
    open DriverTool.Configuration

    module Logging =
        open System
        open System.IO         
        open System.Runtime.CompilerServices
        open log4net
        open System.Reflection        
        open System.Text.RegularExpressions        
                
        let configureLogging =
            log4net.GlobalContext.Properties.["LogFile"] <- getLogFilePath   
            let appConfigFile = new FileInfo(getAppConfigFilePath)            
            let loggerRepository = LogManager.GetRepository(Assembly.GetEntryAssembly())
            log4net.Config.XmlConfigurator.ConfigureAndWatch(loggerRepository,appConfigFile)
            |>ignore

        let getLoggerByType<'T> = 
            LogManager.GetLogger(typeof<'T>)

        let getLoggerByName (name:string) =
            LogManager.GetLogger(Assembly.GetEntryAssembly(),name)

        type LoggingExtensions() = 
            [<Extension>]
            static member inline Logger( obj : System.Object) =
                LogManager.GetLogger(obj.GetType())
                
        let getFunctionName func = 
            let functionName = 
                System.Text.RegularExpressions.Regex.Replace(func.GetType().Name,"(@\d+)$","",RegexOptions.None)
            functionName
            
        let getFuncLoggerName func =
            let funcLoggerName = 
                System.Text.RegularExpressions.Regex.Replace(func.GetType().FullName,"(@\d+)$","",RegexOptions.None)
            funcLoggerName

        let getFunctionLogger func =
            let funcLoggerName = getFuncLoggerName func
            let logger = getLoggerByName funcLoggerName
            logger
        
        let getDurationString (duration:TimeSpan) =
            match duration.TotalMilliseconds with
            | t when t < 1000.0 -> String.Format("{0}ms",duration.TotalMilliseconds)
            | t when t < 60000.0 -> String.Format("{0}s {1}ms",duration.Seconds, duration.Milliseconds)
            | t when t < 1000.0 * 60.0 * 60.0 -> String.Format("{0}m {1}s {2}ms",duration.Minutes, duration.Seconds, duration.Milliseconds)
            | _ -> String.Format("{0}h {1}m {2}s",duration.Hours,duration.Minutes, duration.Seconds)

        let debugLogger func input =
            let logger = getFunctionLogger func
            let mutable functionCall = String.Empty
            if(logger.IsDebugEnabled) then
                functionCall <- String.Format("{0}({1})",(getFunctionName func), input.ToString())
                logger.Debug ("Call:   " + functionCall)
            let startTime = DateTime.Now
            
            let result = func input
            
            let stopTime = DateTime.Now
            let duration = stopTime - startTime
            if(logger.IsDebugEnabled) then
                let functionCallResult = String.Format("Return: {0} -> {1} (Duration: {2})", functionCall , result.ToString(), (getDurationString duration))
                logger.Debug (functionCallResult)
            result
        