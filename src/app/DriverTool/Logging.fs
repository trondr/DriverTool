namespace DriverTool
        
    open DriverTool.Configuration

    module Logging =
        open System.IO         
        open System.Runtime.CompilerServices
        open log4net
        open System.Reflection
                
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
                

