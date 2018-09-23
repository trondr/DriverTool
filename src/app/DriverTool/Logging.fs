namespace DriverTool
        
    open DriverTool.Configuration

    module Logging =
        open System.IO         
        open System.Runtime.CompilerServices
        open log4net
                
        let configureLogging =
            log4net.GlobalContext.Properties.["LogFile"] <- getLogFilePath   
            let appConfigFile = new FileInfo(getAppConfigFilePath)
            for repository in LogManager.GetAllRepositories() do
                log4net.Config.XmlConfigurator.ConfigureAndWatch(repository,appConfigFile)
                |> ignore
        
        let getLogger<'T> = LogManager.GetLogger(typeof<'T>)

        type LoggingExtensions() = 
            [<Extension>]
            static member inline Logger( obj : System.Object) =
                LogManager.GetLogger(obj.GetType())
                

