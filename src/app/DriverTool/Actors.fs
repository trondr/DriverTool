namespace DriverTool

module Actors=
    
    open DriverTool.Library.Logging    
    open Akka.FSharp
    
    // make a pipe-friendly version of Akka.NET PipeTo for handling async computations
    let pipeToWithSender recipient sender asyncComp = pipeTo asyncComp recipient sender

    let throwNotImplementedException logger actorMessage =            
        throwExceptionWithLogging logger (sprintf "Not implemented. Cannot process message: %A." actorMessage) 
