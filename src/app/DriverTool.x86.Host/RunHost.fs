namespace DriverTool.x86.Host

module RunHost =
    
    open DriverTool.x86.Host.HostActors
    open Akka.FSharp
    open DriverTool.Library.Messages
    open DriverTool.Library.Logging
    let logger = getLoggerByName "RunHost"

    let hoconConfig port = 
        let configString = """
akka {  
    loglevel = "INFO"
    actor {
        debug {            
            receive = off
            autoreceive = off
            lifecycle = off
            event-stream = off
            unhandled = off
        }        
        provider = remote
    }
    remote {
        log-sent-messages = off
        dot-netty.tcp {
            port = 8081
            hostname = 0.0.0.0
            public-hostname = localhost
        }
    }
}
"""
        configString.Replace("8081", port) //Replace the default 8081 port with specified port.
            
    let runHost port =
        
        let config = Akka.FSharp.Configuration.parse (hoconConfig port)
        use system = Akka.FSharp.System.create "HostSystem" config
        let hostActor = Akka.FSharp.Spawn.spawn system "HostActor" hostActor
        hostActor <! "DriverTool x86 host actor is now listening for requests."
        system.WhenTerminated.Wait()
        logger.Info("DriverTool host has been terminated.")
        0
