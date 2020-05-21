namespace DriverTool.x86.Host

module HostActors =
    open Akka.FSharp
    open DriverTool.Library.Logging
    open DriverTool.Library.HostMessages
    let logger = getLoggerByName "HostActors"

    let hostActor (mailbox:Actor<_>) =        
        let rec loop () = actor {
                let! message = mailbox.Receive()
                let (actorSystem, sender, self) = mailbox.Context.System,mailbox.Context.Sender, mailbox.Context.Self
                let hostMessage = toHostMessage message
                match hostMessage with
                |HostMessage.Information s ->
                    logger.Info(sprintf "%s" s)
                |HostMessage.Quit _ ->
                    logger.Info("Terminating x86 DriverTool host...")
                    sender <! "Request for termination of host has been received. Terminating x86 DriverTool host..."
                    sender <! new QuitConfirmedHostMessage()
                    actorSystem.Terminate() |> ignore                
                |HostMessage.LenovoSccmPackageInfoRequest r ->                
                    logger.Info(sprintf "Processing %A" r)
                return! loop()
        }
        loop()


