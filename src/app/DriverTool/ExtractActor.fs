namespace DriverTool

module ExtractActor =
    
    open DriverTool.Library.Messages
    open DriverTool.Library.Logging
    open DriverTool.Library.F
    open DriverTool.Library.F0
    open DriverTool.Actors
    open Akka.FSharp
    let logger = getLoggerByName "ExtractActor"

    let extractActor (mailbox:Actor<_>) =
        
        let rec loop () = 
            actor {                                                
                let! message = mailbox.Receive()
                let (sender, self) = (mailbox.Context.Sender, mailbox.Context.Self)
                match message with                
                |ExtractPackage downloadedPackage ->
                    throwNotImplementedException logger message
                |ExtractSccmPackage downloadedSccmPackage ->
                    throwNotImplementedException logger message
                |PackageExtracted extractedPackageInfo  ->
                    sender  <! (PackageExtracted extractedPackageInfo)                    
                |SccmPackageExtracted extractedSccmPackageInfo  ->                    
                    sender <! (SccmPackageExtracted extractedSccmPackageInfo)
                | _ ->
                    logger.Warn(sprintf "Message not handled: %A" message)
                    return! loop()
                return! loop ()
            }
        loop()

