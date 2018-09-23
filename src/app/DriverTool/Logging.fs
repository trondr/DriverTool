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
            let repository = LogManager.GetRepository(Assembly.GetEntryAssembly())
            log4net.Config.XmlConfigurator.ConfigureAndWatch(repository,appConfigFile)

        let getLogger<'T> = LogManager.GetLogger(typeof<'T>)

        type LoggingExtensions() = 
            [<Extension>]
            static member inline Logger( obj : System.Object) =
                LogManager.GetLogger(obj.GetType())
                

