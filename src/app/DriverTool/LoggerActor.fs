﻿namespace DriverTool

module LoggerActor=    
    open Akka.FSharp
    open DriverTool.Library.Logging
    open DriverTool.Library.Messages
    open System.Collections.Generic
    open Common.Logging
    
    let loggerDictionary = new Dictionary<string,ILog>()

    let getLoggerName (LoggerName name) =
        name

    let getLogger loggerName =
        let name = getLoggerName loggerName
        if(loggerDictionary.ContainsKey(name)) then
            loggerDictionary.[name]
        else
            let logger = getLoggerByName(name)
            loggerDictionary.Add(name,logger)
            logger

    let logFatal loggerName (msg:System.Exception) =
        let logger = getLogger loggerName
        logger.Fatal(msg.Message, msg)

    let logError loggerName (msg:System.Exception) =
        let logger = getLogger loggerName
        logger.Error(msg.Message, msg)

    let logInfo loggerName (msg:string) =
        let logger = getLogger loggerName
        logger.Info(msg)

    let logWarn loggerName (msg:Msg) =
        let logger = getLogger loggerName
        logger.Warn(msg)

    let logDebug loggerName (msg:Msg) =
        let logger = getLogger loggerName
        logger.Debug(msg)

    let logTrace loggerName (msg:Msg) =
        let logger = getLogger loggerName
        logger.Trace(msg)

    let loggerActor (mailbox:Actor<_>) =
        
        let rec loop () =
            actor{
                let! message = mailbox.Receive()
                match message with
                |FatalMsg (loggerName, msg) ->
                    logFatal loggerName msg
                |ErrorMsg (loggerName, msg) ->
                    logError loggerName msg
                |WarnMsg (loggerName, msg) ->
                    logWarn loggerName msg
                |InfoMsg (loggerName, msg) ->
                    logInfo loggerName msg
                |DebugMsg (loggerName, msg) ->
                    logDebug loggerName msg
                |TraceMsg (loggerName, msg) ->
                    logTrace loggerName msg                    
            }
        loop()
