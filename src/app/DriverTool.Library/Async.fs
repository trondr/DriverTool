namespace DriverTool.Library

[<AutoOpen>]
module Async =    
    open System.Threading
    open System.Threading.Tasks
    open DriverTool.Library.Logging
    let logger = getLoggerByName "Async"
    
    type Microsoft.FSharp.Control.Async with
        static member AwaitTask (t : Task<'T>, timeout : int) =
            async {
                use cts = new CancellationTokenSource()
                use timer = Task.Delay (timeout, cts.Token)
                try
                    let! completed = Async.AwaitTask <| Task.WhenAny(t, timer)
                    if completed = (t :> Task) then
                        let! result = Async.AwaitTask t
                        return Some result
                    else return None
    
                finally cts.Cancel()
            }
    

    let wait seconds continuationMessage =        
        Async.AwaitTask((System.Threading.Tasks.Task.Delay(seconds * 1000).ContinueWith(fun _ -> logger.Info(sprintf "%s" continuationMessage);None)),6000) |> Async.RunSynchronously |>ignore