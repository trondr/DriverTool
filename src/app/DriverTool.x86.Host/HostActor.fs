namespace DriverTool.x86.Host

module HostActors =
    open Akka.FSharp
    open DriverTool.Library.Logging
        
    let hostActor (mailbox:Actor<_>) =        
        let rec loop () = actor {
                let! message = mailbox.Receive()                               
                printfn "Host message: %s" (valueToString message) |> ignore                
                return! loop()
        }
        loop()


