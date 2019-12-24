namespace DriverTool.Library
        
module NCmdLinerMessenger =
    open System
    open System.IO
    open NCmdLiner
    
    type NotepadMessenger () =
        let tempFileName = 
            let tempFile = System.IO.Path.GetTempFileName()
            let txtTempFile = tempFile + ".txt"
            System.IO.File.Move(tempFile, txtTempFile)
            txtTempFile
        let streamWriter = 
            new StreamWriter(tempFileName)
        do            
            ()
        
        interface IDisposable with
            member this.Dispose() =                
                streamWriter.Dispose()                
                match (FileSystem.path tempFileName) with
                |Ok fp ->  (FileOperations.deleteFileIfExists fp)
                |Result.Error ex -> ()

        interface IMessenger with
            member x.Write (formatMessage:string,args:obj[]) =
                streamWriter.Write(formatMessage.Replace("\r\n","\n").Replace("\n",Environment.NewLine),args)
                ()
            member x.WriteLine (formatMessage:string,args:obj[]) =
                streamWriter.WriteLine(formatMessage.Replace("\r\n","\n").Replace("\n",Environment.NewLine),args)
                ()
            member x.Show () =
                streamWriter.Close()
                if(Environment.UserInteractive) then
                    System.Diagnostics.Process.Start(tempFileName) |> ignore
                    System.Threading.Thread.Sleep(2000)
                else
                    use sr = new StreamReader(tempFileName)
                    printfn "%s" (sr.ReadToEnd())
                ()
                
