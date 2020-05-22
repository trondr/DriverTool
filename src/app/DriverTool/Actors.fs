namespace DriverTool

module Actors=
    
    open DriverTool.Library.Logging    
    open Akka.FSharp
    
    /// <Summary>
    /// A pipe-friendly version of Akka.NET PipeTo for handling async computations
    /// Pipes an output of asychronous expression directly to the recipients mailbox
    /// </Summary>
    let pipeToWithSender recipient sender asyncComp = pipeTo asyncComp recipient sender

    let throwNotImplementedException logger actorMessage =            
        throwExceptionWithLogging logger (sprintf "Not implemented. Cannot process message: %A." actorMessage) 
