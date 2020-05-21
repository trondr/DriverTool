namespace DriverTool

module ActorSystem =

    open Akka.FSharp
    open Akka.Actor
    open DriverTool.Library
    open DriverTool.Library.HostMessages
    open DriverTool.Library.Logging
    let logger = getLoggerByName "RunHost"

    let exeAssembly() = 
        let exeAssembly = System.Reflection.Assembly.GetEntryAssembly()        
        match exeAssembly with
        |null -> None
        |_ -> Some exeAssembly

    let exePath() =
        match exeAssembly() with
        |Some assembly -> 
            FileSystem.path assembly.Location
        |None -> Result.Error (toException "Unable to get exe folder path due location of entry assembly is unknown." None)

    
    let exeFolderPath() =
        result{
            let! exePath = exePath()
            return! FileSystem.path (System.IO.Path.GetDirectoryName(FileSystem.pathValue exePath))
        }

    let x86HostPath () =
        result{
            let! exeFolderPath = exeFolderPath()
            let! x86HostPath = PathOperations.combinePaths2 exeFolderPath "DriverTool.x86.Host.exe"
            let! existingx86HostPath = FileOperations.ensureFileExists x86HostPath
            return existingx86HostPath
        }

    let startx86HostProcess (context:Akka.Actor.IActorContext) =
        match(result{
            let! exePath = x86HostPath()
            let! existingExePath = FileOperations.ensureFileExists exePath
            ProcessOperations.startProcess existingExePath "RunHost /port=8081" None false |> ignore            
            return ()
        })with
        |Result.Ok _ -> logger.Info("Successfully started x86 host.")
        |Result.Error ex -> 
            logger.Error(sprintf "Failed to start x86 host due to error: %s " (getAccumulatedExceptionMessages ex))
            raise ex
        let hostActor = context.ActorSelection("akka.tcp://HostSystem@localhost:8081/user/HostActor")
        hostActor <! "Ping DriverTool x86 host from client."
        hostActor

    let stopx86HostProcess hostActor = 
        hostActor <! (new QuitHostMessage())
    
    let clientActor (mailbox:Actor<_>) =
        let hostActor = startx86HostProcess mailbox.Context        
        let rec loop () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (actorSystem, sender, self) = mailbox.Context.System,mailbox.Context.Sender, mailbox.Context.Self
                let clientHostMessage = toClientHostMessage message
                match clientHostMessage with
                |Information s -> 
                    logger.Info(s)
                    hostActor <! s
                |Quit q -> 
                    logger.Info("Sending termination request to x86 host.")
                    hostActor <! q                    
                |QuitConfirmed _ ->
                    logger.Info("x86 host has confirmed termination request.")
                    logger.Info("Terminating client actor system.")
                    actorSystem.Terminate()|>ignore
                |StartConfirmed _ ->
                    logger.Info("x86 host has confirmed start.")
                    ()
                return! loop ()
            }
        loop()

    let hoconConfig = """
    akka {  
        loglevel = "INFO"
        actor {
            provider = remote
        }
        remote {
            debug {
                receive = off
                autoreceive = off
                lifecycle = off
                event-stream = off
                unhandled = off
            }
            log-sent-messages = off
            dot-netty.tcp {
		        port = 0
		        hostname = localhost
                }
        }
    }
    """

    let startClientActorSystem () =
        let config = Akka.FSharp.Configuration.parse hoconConfig
        let system = Akka.FSharp.System.create "ClientSystem" config
        let actor = Akka.FSharp.Spawn.spawn system "ClientActor" clientActor
        (system,actor)