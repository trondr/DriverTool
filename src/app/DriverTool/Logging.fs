namespace DriverTool
        
    open DriverTool.Configuration
    
    module Logging =
        open System
        open System.IO         
        open System.Runtime.CompilerServices
        open log4net
        open System.Reflection        
        open System.Text.RegularExpressions        
                
        let configureLogging () =
            log4net.GlobalContext.Properties.["LogFile"] <- getLogFilePath   
            let appConfigFile = new FileInfo(getAppConfigFilePath)            
            let loggerRepository = LogManager.GetRepository(typeof<F0.ThisAssembly>.Assembly)
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
            LogManager.GetLogger(typeof<F0.ThisAssembly>.Assembly,name)

        type LoggingExtensions() = 
            [<Extension>]
            static member inline Logger( obj : System.Object) =
                LogManager.GetLogger(obj.GetType())
                
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
            | t when t < 1000.0 -> String.Format("{0}ms",duration.TotalMilliseconds)
            | t when t < 60000.0 -> String.Format("{0}s {1}ms",duration.Seconds, duration.Milliseconds)
            | t when t < 1000.0 * 60.0 * 60.0 -> String.Format("{0}m {1}s {2}ms",duration.Minutes, duration.Seconds, duration.Milliseconds)
            | _ -> String.Format("{0}h {1}m {2}s",duration.Hours,duration.Minutes, duration.Seconds)

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
                        let stringValues = inputValues |> Array.map (fun x -> valueToString x)
                        "(" + (stringValues |> String.concat ",") + ")"                    
                    | _ -> valueToString input
            parametersString

        let exceptionToString (ex:Exception) =
            let exceptionType = ex.GetType().FullName
            let exceptionMessage = ex.Message
            String.Format("Exception : {0} : {1}",exceptionType, exceptionMessage)
        
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

        let debugLogger func input =
            let logger = getFunctionLogger func
            let mutable functionCall = String.Empty
            if(logger.IsDebugEnabled) then
                functionCall <- String.Format("{0}({1})",(getFunctionName func), (getParametersString input))
                logger.Debug ("Call:   " + functionCall)
            let startTime = DateTime.Now
            
            let result = func input
            
            let stopTime = DateTime.Now
            let duration = stopTime - startTime
            if(logger.IsDebugEnabled) then                
                let functionCallResult = String.Format("Return: {0} -> {1} (Duration: {2})", functionCall , (resultToString result), (getDurationString duration))
                logger.Debug (functionCallResult)
            result
        
        

        let debugLoggerResult func input =
            let logger = getFunctionLogger func
            let mutable functionCall = String.Empty
            if(logger.IsDebugEnabled) then
                functionCall <- String.Format("{0}({1})",(getFunctionName func), (getParametersString input))
                logger.Debug ("Call:   " + functionCall)
            let startTime = DateTime.Now
            
            let result = func input
            
            let stopTime = DateTime.Now
            let duration = stopTime - startTime
            if(logger.IsDebugEnabled) then
                let resultString = 
                    match result with
                    |Ok v -> "OK" + valueToString v
                    |Error ex -> "ERROR:" + getAccumulatedExceptionMessages ex
                let functionCallResult = String.Format("Return: {0} -> {1} (Duration: {2})", functionCall , resultString, (getDurationString duration))
                logger.Debug (functionCallResult)
                
            result
        
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
        
        let logFormat (logger:ILog) logLevel =
            match logLevel with
            |Info -> logger.InfoFormat
            |Warn -> logger.WarnFormat
            |Error -> logger.ErrorFormat
            |Fatal -> logger.FatalFormat
            |Debug -> logger.DebugFormat
        
        let genericLoggerResult logLevel func input : Result<'T,Exception> =
            let logger = getFunctionLogger func
            let doLog = isLoggingEnabled logger logLevel
            let writeLog = log logger logLevel
            
            let mutable functionCall = String.Empty
            if(doLog) then
                let functionName = getFunctionName func
                let parametersString = (getParametersString input)
                functionCall <- String.Format("{0}({1})",functionName, parametersString)
                writeLog ("Call: " + functionCall)
            
            let startTime = DateTime.Now
            
            let result = func input
            
            let stopTime = DateTime.Now
            let duration = stopTime - startTime
            if(doLog) then
                let resultString = 
                    match result with
                    |Ok v -> "OK" + valueToString v
                    |Result.Error ex -> "ERROR:" + getAccumulatedExceptionMessages ex
                let functionCallResult = String.Format("Return: {0} -> {1} (Duration: {2})", functionCall , resultString, (getDurationString duration))
                writeLog (functionCallResult)
            
            result