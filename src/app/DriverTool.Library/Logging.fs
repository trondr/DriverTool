namespace DriverTool.Library
        
    open DriverTool.Library.Configuration
    
    module Logging =
        open System
        open System.IO         
        open System.Runtime.CompilerServices
        
        open System.Text.RegularExpressions                
        open Serilog
        open Serilog.Configuration
        open Serilog.Events
        open Serilog.Formatting.Json
        open Serilog.Sinks
        open Serilog.Sinks.SystemConsole
        open Common.Logging
        
        let toLogLevel logLevelValue =
            match logLevelValue with
            | "All" -> Common.Logging.LogLevel.All
            | "Debug" -> Common.Logging.LogLevel.Debug
            | "Error" -> Common.Logging.LogLevel.Error
            | "Fatal" -> Common.Logging.LogLevel.Fatal
            | "Info" -> Common.Logging.LogLevel.Info
            | "Off" -> Common.Logging.LogLevel.Off
            | "Trace" -> Common.Logging.LogLevel.Trace
            | "Warn" -> Common.Logging.LogLevel.Warn
            | _ -> failwith (sprintf "Invalid loglevel '%s' specified in app config. Valid log level values are: %s" logLevelValue (getEnumValuesToString typeof<Common.Logging.LogLevel>))

        let toSerilogLogEventLevel commonLogingLogLevel =
            match commonLogingLogLevel with            
            | Common.Logging.LogLevel.Debug -> LogEventLevel.Debug
            | Common.Logging.LogLevel.Error -> LogEventLevel.Error
            | Common.Logging.LogLevel.Fatal -> LogEventLevel.Fatal
            | Common.Logging.LogLevel.Info -> LogEventLevel.Information
            | Common.Logging.LogLevel.Trace -> LogEventLevel.Verbose
            | Common.Logging.LogLevel.Warn -> LogEventLevel.Warning            
            | _ -> failwith (sprintf "Common logging LogLevel '%A' is not supported by SeriLog LogEventLevel." commonLogingLogLevel)

        let configureLogging () =
            let logLevel = getLogLevel |> toLogLevel |> toSerilogLogEventLevel
            
            let log = LoggerConfiguration()
                        .MinimumLevel.Is(logLevel)
                        .Enrich.FromLogContext()                      
                        .WriteTo.Console()
                        .WriteTo.File(getLogFilePath)
                        .CreateLogger()
            Log.Logger <- log
            ()

        let configureConsoleLogging () =
            let logLevel = getLogLevel |> toLogLevel |> toSerilogLogEventLevel
            
            let log = LoggerConfiguration()
                        .MinimumLevel.Is(logLevel)
                        .Enrich.FromLogContext()                      
                        .WriteTo.Console()                        
                        .CreateLogger()
            Log.Logger <- log
            ()
            
        let logFactory (loggerType:Type)=
            LogManager.GetLogger(loggerType)

        let cachedLogFactory =
            memoize logFactory
            
        let Logger<'T>() = 
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
        
        let log (logger:ILog) logLevel (message:obj) =
            match logLevel with
            |Info -> logger.Info(message)
            |Warn -> logger.Warn(message)
            |Error -> logger.Error(message)
            |Fatal -> logger.Fatal(message)
            |Debug -> logger.Debug(message)
        
        let logSeq (logger:ILog) logLevel (formatedSprintfF1:(string->string)) records  =
            let writeLog = log logger logLevel
            records
            |> Seq.map  (fun r -> 
                                let valueString = sprintf "%A" r
                                writeLog(formatedSprintfF1 valueString)
                                r
                            )
            |>Seq.toArray
                
        let throwExceptionWithLogging (logger:Common.Logging.ILog) (errorMessage:string) =
            logger.Error(errorMessage)
            failwith errorMessage
        
        /// Make sure progress info is output to the console in a nice and orderly fashion from multiple threads.
        let progressActor = 
            MailboxProcessor.Start(fun inbox -> 
                let rec messageLoop() = async{
                    let! msg = inbox.Receive()
                    printf "%s" msg
                    return! messageLoop()
                    }        
                messageLoop()
                )

        ///Function definition: (activity:string) -> (status:string option) -> (currentOperation:string option) -> (percentComplete: float option) -> (isBusy:bool) -> (id:int option)
        type reportProgressFunction = string -> string -> string -> float option -> bool -> int option -> unit
            
        let min x y = 
            if x < y then x
            else y

        let normalizeLength length (value:string) =            
            let maxLength = min value.Length length
            value.Substring(0,maxLength) + "\r"

        let getConsoleWindowWidth () = 
            match (System.Console.LargestWindowWidth) with
            |0 -> 800
            |_ -> Console.WindowWidth
                
        ///Report progress to stdout
        let reportProgressStdOut' : reportProgressFunction = (fun activity status currentOperation percentComplete isBusy id ->
                let length = getConsoleWindowWidth ()
                match percentComplete with
                |Some p ->
                    match id with
                    |Some i ->
                        progressActor.Post (sprintf "%d: %.2f: %s: %s: %s: (IsBusy: %b)" i p activity currentOperation status isBusy|>normalizeLength length)
                    |None -> 
                        progressActor.Post (sprintf "%.2f: %s: %s: %s: (IsBusy: %b)" p activity currentOperation status isBusy|>normalizeLength length)
                |None -> 
                    match id with
                    |Some i ->
                        progressActor.Post (sprintf "%d: %s: %s: %s: (IsBusy: %b)" i activity status currentOperation isBusy|>normalizeLength length)
                    |None -> 
                        progressActor.Post (sprintf "%s: %s: %s: (IsBusy: %b)" activity status currentOperation isBusy|>normalizeLength length)
            )