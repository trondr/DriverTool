namespace DriverTool.Library
        
    open DriverTool.Library.Configuration
    
    module Logging =
        open System
        open System.IO         
        open System.Runtime.CompilerServices
        open Common.Logging                
        open System.Text.RegularExpressions        
                
        let configureLogging () =
            log4net.GlobalContext.Properties.["LogFile"] <- getLogFilePath   
            let appConfigFile = new FileInfo(getAppConfigFilePath)            
            let loggerRepository = log4net.LogManager.GetRepository(typeof<F0.ThisAssembly>.Assembly)
            log4net.Config.XmlConfigurator.ConfigureAndWatch(loggerRepository,appConfigFile)
            |>ignore

        let logFactory (loggerType:Type)=
            LogManager.GetLogger(loggerType)

        let cachedLogFactory =
            memoize logFactory
            
        let Logger<'T> = 
            cachedLogFactory(typeof<'T>)

        type System.Object with
            member x.Logger() = 
                cachedLogFactory(x.GetType())

        let getLoggerByName (name:string) =
            LogManager.GetLogger(name)

        type LoggingExtensions() = 
            [<Extension>]
            static member inline Logger( obj : System.Object) =
                LogManager.GetLogger(obj.GetType())

        type Msg = System.Action<Common.Logging.FormatMessageHandler>
        let msg message =
            new Msg(fun m -> m.Invoke(message)|>ignore)
                
        let getFunctionName func = 
            let functionName = 
                let funcTypeName = func.GetType().Name
                System.Text.RegularExpressions.Regex.Replace(funcTypeName,"(@[0-9a-zA-Z\-]+)$","",RegexOptions.None)
            functionName

        let getFunctionFullName func =
            let functionName = getFunctionName func
            let functionFullName =
                let reflectedType = func.GetType().ReflectedType
                reflectedType.FullName + "+" + functionName
            functionFullName
            
        let getFuncLoggerName func =
            getFunctionFullName func            

        let getFunctionLogger func =
            let funcLoggerName = getFuncLoggerName func
            let logger = getLoggerByName funcLoggerName
            logger
        
        let getDurationString (duration:TimeSpan) =
            match duration.TotalMilliseconds with
            | t when t < 1000.0 -> sprintf "%.0fms" duration.TotalMilliseconds
            | t when t < 60000.0 -> sprintf "%is %ims" duration.Seconds duration.Milliseconds
            | t when t < 1000.0 * 60.0 * 60.0 -> sprintf "%im %is %ims" duration.Minutes duration.Seconds duration.Milliseconds
            | _ -> sprintf "%ih %im %is" duration.Hours duration.Minutes duration.Seconds

        let valueToString value =   
            match box value with
            | null -> ""
            |_ -> Newtonsoft.Json.JsonConvert.SerializeObject(value) + ":" + value.GetType().ToString()
        
        let getParametersString (input:obj) =            
            let parametersString = 
                match input with
                |null -> "()"
                |_ ->
                    match input.GetType() with
                    | t when Microsoft.FSharp.Reflection.FSharpType.IsTuple(t) -> 
                        let inputValues = Microsoft.FSharp.Reflection.FSharpValue.GetTupleFields input
                        let stringValues = inputValues |> Array.map (fun x -> sprintf "%A" x)
                        "(" + (stringValues |> String.concat ",") + ")"                    
                    | _ -> valueToString input
            parametersString

        let exceptionToString (ex:Exception) =
            let exceptionType = ex.GetType().FullName
            let exceptionMessage = ex.Message
            sprintf "Exception : %s : %s" exceptionType exceptionMessage
        
        let (|As|_|) (p:'T) : 'U option =
            let p = p :> obj
            if p :? 'U then Some (p :?> 'U) else None
        
        let (|NCmdLinerResultType|_|) t =
            let bt = box t
            match bt with
            | :? NCmdLiner.Result<int> as r -> Some(r)
            |_ -> None

        let resultToString result = 
            let resultString = 
                match result with
                | NCmdLinerResultType r -> 
                    if(r.IsSuccess) then
                        "Ok : " + valueToString r.Value
                    else
                        "Error : " + (exceptionToString r.Exception)
                | _ -> valueToString result
            resultString            

        type LogLevel = Info|Warn|Error|Fatal|Debug

        let isLoggingEnabled (logger:ILog) logLevel =
            match logLevel with
            |Info -> logger.IsInfoEnabled
            |Warn -> logger.IsWarnEnabled
            |Error -> logger.IsErrorEnabled
            |Fatal -> logger.IsFatalEnabled
            |Debug -> logger.IsDebugEnabled
        
        let log (logger:ILog) logLevel =
            match logLevel with
            |Info -> logger.Info
            |Warn -> logger.Warn
            |Error -> logger.Error
            |Fatal -> logger.Fatal
            |Debug -> logger.Debug
        
        let genericLoggerResult logLevel func input : Result<'T,Exception> =
            let logger = getFunctionLogger func
            let doLog = isLoggingEnabled logger logLevel
            let writeLog = log logger logLevel
            let writeErrorLog = log logger LogLevel.Error
            
            let mutable functionCall = String.Empty
            if(doLog) then
                let functionName = getFunctionName func
                let parametersString = (getParametersString input)
                functionCall <- sprintf "%s(%s)" functionName parametersString
                writeLog (msg (sprintf "Call: %s" functionCall))
            
            let startTime = DateTime.Now
            
            let result = func input
            
            let stopTime = DateTime.Now
            let duration = stopTime - startTime
            if(doLog) then
                let resultString = 
                    match result with
                    |Ok v -> "OK: " + valueToString v
                    |Result.Error ex -> "ERROR: " + getAccumulatedExceptionMessages ex
                let functionCallResult = sprintf "Return: %s -> %s (Duration: %s)" functionCall resultString (getDurationString duration)
                match result with
                |Ok _ -> writeLog (msg functionCallResult)
                |Result.Error _ -> writeErrorLog (msg functionCallResult)
            result
        

        let genericLogger logLevel func input =
            let logger = getFunctionLogger func
            let doLog = isLoggingEnabled logger logLevel
            let writeLog = log logger logLevel
            let writeErrorLog = log logger LogLevel.Error

            let mutable functionCall = String.Empty
            if(doLog) then
                let functionName = getFunctionName func
                let parametersString = (getParametersString input)
                functionCall <- sprintf "%s(%s)" functionName  parametersString
                writeLog (msg (sprintf "Call: %s" functionCall))
            
            let startTime = DateTime.Now
            let mutable returnValue = box null
            try
                try
                    returnValue <- func input
                with
                |ex -> 
                    let functionName = getFunctionName func
                    let parametersString = (getParametersString input)
                    functionCall <- sprintf "%s(%s)" functionName parametersString
                    writeErrorLog (msg (sprintf "'%s' failed due to: %s" functionCall (ex.ToString())))
                    raise (sourceException ex)
            finally
                let stopTime = DateTime.Now
                let duration = stopTime - startTime
                if(doLog) then                
                    let functionCallResult = sprintf "Return: %s -> %s (Duration: %s)" functionCall (resultToString returnValue) (getDurationString duration)
                    writeLog (msg functionCallResult)
            unbox returnValue
    
        let logSeq (logger:ILog) records  =
            records
            |> Seq.map  (fun dj -> 
                                logger.Info(valueToString dj)
                                dj
                            )
            |>Seq.toArray

        let logSeqWithFormatString (logger:ILog) formatedSprintfF1 records  =
            records
            |> Seq.map  (fun r -> 
                                let valueString = sprintf "%A" r
                                logger.Info(msg (formatedSprintfF1 valueString))
                                r
                            )
            |>Seq.toArray
                
        let logSeqToConsoleWithFormatString formatStringF1 records  =
            records
            |> Seq.map  (fun dj -> 
                                printfn formatStringF1 (valueToString dj)
                                dj
                            )
        
        let logSeqToConsole records =
            logSeqToConsoleWithFormatString "%s" records

        let logToConsole record =
            printfn "%s" (valueToString record)
            record
        
