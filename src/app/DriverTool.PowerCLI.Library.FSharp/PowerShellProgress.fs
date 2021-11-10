namespace DriverTool.PowerCLI.Library.FSharp

module PowerShellProgress =
    
    open System.Management.Automation
    open System.Threading

    /// Make an operation thread safe by locking access to the operation   
    let lock (lockObj:obj) f =
      Monitor.Enter lockObj
      try
        f()
      finally
        Monitor.Exit lockObj

    //Credits: https://stackoverflow.com/questions/12852494/best-way-to-update-cmdlet-progress-from-a-separate-thread
    type PowerShellProgressAdapter () =

        member val Finished : bool = false with get,set
        member val private _queue : System.Collections.Concurrent.ConcurrentQueue<ProgressRecord> = new System.Collections.Concurrent.ConcurrentQueue<ProgressRecord>() with get
        member val private _sync : AutoResetEvent = new AutoResetEvent(false) with get
        member val private _lockToken : obj = new obj() with get

        member this.WriteQueue(progressRecord:ProgressRecord) =            
            lock this._lockToken (fun () ->
                this._queue.Enqueue(progressRecord)
                this._sync.Set() //Allert that data is available
            )
            
        member this.Listen() =
            () //TODO implement listen method
            //while(not this.Finished)do
            //    while(true) do
            //        lock this._lockToken (fun () ->
            //            if(this._queue.Count > 0) then
            //                this._queue.TryDequeue() |> ignore //TODO: get the 
            //            else
            //                ()
            //        )                    
        
