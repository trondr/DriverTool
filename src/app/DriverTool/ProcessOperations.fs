namespace DriverTool
open F
open System.IO

module ProcessOperations =
    let logger = Logging.getLoggerByName("ProcessOperations")

    type ProcessOperations = class end
    open System.Diagnostics
    open System.Text    
    open System    
    open System.Threading
    open System.Threading.Tasks
    
    let startProcessUnsafe (filePath, arguments) =
        nullGuard filePath "filePath"
        nullGuard arguments "arguments"

        let currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name
        System.Console.WriteLine("Current method name: " + currentMethodName)
    
        let startInfo = new ProcessStartInfo(filePath,arguments)
        startInfo.WorkingDirectory <- System.IO.Path.GetDirectoryName(filePath)
        startInfo.CreateNoWindow <- true
        startInfo.UseShellExecute <- false
        logger.InfoFormat("Start Process: '{0}' {1}",filePath,arguments)
        let runningProcess = Process.Start(startInfo)
        runningProcess.WaitForExit()
        let exitCode = runningProcess.ExitCode
        logger.InfoFormat("Exit Process: '{0}' {1}. Exit code: {2}",filePath,arguments,exitCode)
        exitCode
    
    let startProcess (filePath, arguments) =
        tryCatch startProcessUnsafe (filePath, arguments)
    
    type ProcessExitData = {FileName:string;Arguments:string;ExitCode:int;StdOutput:string;StdError:string}

    let writeProcessExitDataToLog processExitData logFileName appendToLogFile =
        let writeToFileBase logFileName appendToLogFile (text:string) =
            use sw =
                match appendToLogFile with
                |true -> System.IO.File.AppendText(logFileName)
                |false -> System.IO.File.CreateText(logFileName)
            sw.Write(text)
                        
        let writeToFile text =
            writeToFileBase logFileName appendToLogFile text
        
        let logToFile = 
            not (String.IsNullOrWhiteSpace(logFileName))
        
        let writeToLog logFunction (line:string) =
            seq{ yield line}
            |>Seq.map(fun s -> 
                                logFunction(s)
                                s
                        )
            |>Seq.filter(fun _ -> logToFile)
            |>Seq.map(fun s -> (writeToFile s))
            |>Seq.toArray
            |>ignore

        logger.Debug("writeToLog command line")        
        writeToLog logger.Info (String.Format("'{0}' {1}",processExitData.FileName,processExitData.Arguments))
        
        logger.Debug("writeToLog StdOutput")
        if (not (String.IsNullOrWhiteSpace(processExitData.StdOutput))) then
            writeToLog logger.Info processExitData.StdOutput
         
        logger.Debug("writeToLog StdError")
        if (not (String.IsNullOrWhiteSpace(processExitData.StdError))) then
            writeToLog logger.Error processExitData.StdError

    let startConsoleProcessUnsafe (fileName, arguments, workingDirectory,timeout:int, inputData, logFileName, appendToLogFile) =
        let startInfo = new ProcessStartInfo()
        startInfo.CreateNoWindow <- true
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.FileName <- fileName
        if(not (System.String.IsNullOrWhiteSpace(arguments))) then
            startInfo.Arguments <- arguments
        if(not (System.String.IsNullOrWhiteSpace(workingDirectory))) then
            startInfo.WorkingDirectory <- workingDirectory        
        use consoleProcess = new Process()
        consoleProcess.StartInfo <- startInfo
        if(not (System.String.IsNullOrWhiteSpace(inputData))) then
            consoleProcess.StandardInput.Write(inputData)
            consoleProcess.StandardInput.Close()

        let stdOutBuilder = new StringBuilder()
        let stdErrorBuilder = new StringBuilder()

        let outputDataReceivedHandler =
           new DataReceivedEventHandler(fun s e ->                                             
                                            stdOutBuilder.AppendLine(e.Data) |> ignore
                                       )
        let errorDataReceivedHandler =           
           new DataReceivedEventHandler(fun s e ->                                             
                                            stdErrorBuilder.AppendLine(e.Data) |> ignore                                            
                                            )

        consoleProcess.OutputDataReceived.AddHandler(outputDataReceivedHandler)
        consoleProcess.ErrorDataReceived.AddHandler(errorDataReceivedHandler)

        consoleProcess.EnableRaisingEvents <- true
        consoleProcess.Start() |> ignore
        consoleProcess.BeginOutputReadLine()
        consoleProcess.BeginErrorReadLine()
        match consoleProcess.WaitForExit(timeout) with
        |true -> ignore
        |false ->            
            consoleProcess.Close()
            raise (new Exception(String.Format("Process execution timed out: \"{0}\" {1}",fileName,arguments)))
        |>ignore
            
        consoleProcess.CancelErrorRead()
        consoleProcess.CancelOutputRead()
        consoleProcess.ErrorDataReceived.RemoveHandler(errorDataReceivedHandler)
        consoleProcess.OutputDataReceived.RemoveHandler(outputDataReceivedHandler)
        let processExitData =
            {
                FileName = fileName
                Arguments = arguments
                ExitCode = consoleProcess.ExitCode
                StdOutput = stdOutBuilder.ToString().Trim()
                StdError = stdErrorBuilder.ToString().Trim()
            }
        
        writeProcessExitDataToLog processExitData logFileName appendToLogFile            
        
        processExitData.ExitCode
    
    let startConsoleProcessBase (fileName, arguments, workingDirectory,timeout:int, inputData, logFileName, appendToLogFile) =
        tryCatchWithMessage startConsoleProcessUnsafe (fileName, arguments, workingDirectory,timeout, inputData, logFileName, appendToLogFile) (String.Format("Start of console process ('{0}' {1}) failed.",fileName,arguments))
    
    let startConsoleProcess (fileName, arguments, workingDirectory,timeout:int, inputData, logFileName, appendToLogFile) = 
        Logging.genericLoggerResult Logging.LogLevel.Info startConsoleProcessBase (fileName, arguments, workingDirectory,timeout, inputData, logFileName, appendToLogFile)
    
    let startConsoleProcessUnsafe2 (fileName, arguments, workingDirectory, timeout:int, inputData, logFileName, appendToLogFile) =
        use consoleProcess = new Process()
        consoleProcess.StartInfo.FileName <- fileName
        if(not (System.String.IsNullOrWhiteSpace(arguments))) then
            consoleProcess.StartInfo.Arguments <- arguments
        if(not (System.String.IsNullOrWhiteSpace(workingDirectory))) then
            consoleProcess.StartInfo.WorkingDirectory <- workingDirectory
        consoleProcess.StartInfo.RedirectStandardInput <- true
        consoleProcess.StartInfo.RedirectStandardOutput <- true
        consoleProcess.StartInfo.RedirectStandardError <- true
        consoleProcess.StartInfo.UseShellExecute <- false
        consoleProcess.StartInfo.CreateNoWindow <- true
        consoleProcess.Start()|>ignore
        
        if(not (System.String.IsNullOrWhiteSpace(inputData))) then
            consoleProcess.StandardInput.Write(inputData)
            consoleProcess.StandardInput.Close()
        
        let endTime = 
            if(timeout > 0) then
                DateTime.Now + TimeSpan.FromMilliseconds(float timeout)
            else
                DateTime.MaxValue
        
        let exitData = 
            let mutable hasExited = false
            let mutable hasTimedOut = false            
            let standardOutPutData = new StringBuilder()
            let standardErrorData = new StringBuilder()
            while ((not hasExited) && (not hasTimedOut)) do                
                logger.Debug("Looping to check process status")                                
                
                while(
                        logger.Debug("Check end of StandardOutput")
                        let endOfStream = consoleProcess.StandardOutput.EndOfStream
                        logger.Debug("Endofstream StandardOutput: " + endOfStream.ToString())
                        logger.Debug("Check peek StandardOutput")
                        let peek = consoleProcess.StandardOutput.Peek()
                        logger.Debug("Peek StandardOutput: " + peek.ToString())
                        (not endOfStream) && peek > -1
                     ) do
                    logger.Debug("StandardOutput read data to end")
                    let cancellationTokenSource = new CancellationTokenSource(3000)
                    let task = System.Threading.Tasks.Task.Factory.StartNew(fun () -> standardErrorData.Append(consoleProcess.StandardOutput.Read())|>ignore, cancellationTokenSource.Token)
                    task.Wait()
                    logger.Debug("StandardOutput finished read data to end")
                                
                while(
                        logger.Debug("Check end of StandardError")
                        let endOfStream = consoleProcess.StandardError.EndOfStream
                        logger.Debug("Endofstream StandardError: " + endOfStream.ToString())
                        logger.Debug("Check peek StandardError")
                        let peek = consoleProcess.StandardError.Peek()
                        logger.Debug("Peek StandardError: " + peek.ToString())
                        (not endOfStream) && peek > -1
                     ) do
                    logger.Debug("StandardError read data to end")
                    standardErrorData.Append(consoleProcess.StandardError.Read())|>ignore
                    logger.Debug("StandardError finished read data to end")
                
                hasExited <- consoleProcess.WaitForExit(0)
                logger.Debug("Has exited: " + hasExited.ToString())
                hasTimedOut <- DateTime.Now > endTime
                logger.Debug("Has timed out: " + hasTimedOut.ToString())
                if(hasTimedOut && not hasExited) then
                    logger.Debug("Timeout! Close process and throw exception")
                    consoleProcess.Close()
                    raise (new Exception(String.Format("Process execution timed out: \"{0}\" {1}",fileName,arguments)))
                logger.Debug("Yield the thread")
                System.Threading.Thread.Yield()|>ignore
            
            let processExitData =
                {
                    FileName = fileName
                    Arguments = arguments
                    ExitCode = consoleProcess.ExitCode
                    StdOutput = standardOutPutData.ToString().Trim()
                    StdError = standardErrorData.ToString().Trim()
                }
            processExitData
        
        writeProcessExitDataToLog exitData logFileName appendToLogFile

        exitData.ExitCode        

    let startConsoleProcessBase2 (fileName, arguments, workingDirectory, timeout:int, inputData, logFileName, appendToLogFile) =
        tryCatchWithMessage startConsoleProcessUnsafe2 (fileName, arguments, workingDirectory,timeout,inputData, logFileName, appendToLogFile) (String.Format("Start of console process ('{0}' {1}) failed.",fileName,arguments))
    
    let startConsoleProcess2 (fileName, arguments, workingDirectory,timeout:int, inputData, logFileName, appendToLogFile) = 
        Logging.genericLoggerResult Logging.LogLevel.Info startConsoleProcessBase2 (fileName, arguments, workingDirectory, timeout, inputData, logFileName, appendToLogFile)
    
    let startConsoleProcessUnsafe3 (fileName, arguments, workingDirectory, timeout:int, inputData, logFileName, appendToLogFile) =
        use consoleProcess = new Process()
        consoleProcess.StartInfo.FileName <- fileName
        if(not (System.String.IsNullOrWhiteSpace(arguments))) then
            consoleProcess.StartInfo.Arguments <- arguments
        if(not (System.String.IsNullOrWhiteSpace(workingDirectory))) then
            consoleProcess.StartInfo.WorkingDirectory <- workingDirectory
        consoleProcess.StartInfo.RedirectStandardInput <- true
        consoleProcess.StartInfo.RedirectStandardOutput <- true
        consoleProcess.StartInfo.RedirectStandardError <- true
        consoleProcess.StartInfo.UseShellExecute <- false
        consoleProcess.StartInfo.CreateNoWindow <- true
                
        if(not (System.String.IsNullOrWhiteSpace(inputData))) then
            consoleProcess.StandardInput.Write(inputData)
            consoleProcess.StandardInput.Close()
    
        let standardOutPutData = new StringBuilder()
        let standardErrorData = new StringBuilder()

        use outputWaitHandle = new AutoResetEvent(false)
        use errorWaitHandle = new AutoResetEvent(false)

        let outputDataReceivedHandler =
           new DataReceivedEventHandler(fun _ e -> 
                                            if(e.Data = null) then
                                                errorWaitHandle.Set()|>ignore
                                            else    
                                                logger.Debug("StandardOutPut has data")
                                                standardOutPutData.AppendLine(e.Data) |> ignore
                                                logger.Debug("StandardOutPut data was processed")
                                            
                                       )
        let errorDataReceivedHandler =           
           new DataReceivedEventHandler(fun _ e -> 
                                            if(e.Data = null) then
                                                outputWaitHandle.Set()|>ignore
                                            else    
                                                logger.Debug("StandardError has data")
                                                standardErrorData.AppendLine(e.Data) |> ignore
                                                logger.Debug("StandardError data was processed")
                                       )
        consoleProcess.OutputDataReceived.AddHandler(outputDataReceivedHandler)
        consoleProcess.ErrorDataReceived.AddHandler(errorDataReceivedHandler)
        logger.Debug("Start process")
        consoleProcess.Start()|>ignore
        logger.Debug("BeginOutputReadLine")
        consoleProcess.BeginOutputReadLine()
        logger.Debug("BeginErrorReadLine")
        consoleProcess.BeginErrorReadLine()

        let exitData =
            logger.Debug("Wait for exit")
            if(consoleProcess.WaitForExit(timeout) && outputWaitHandle.WaitOne(timeout) && errorWaitHandle.WaitOne(timeout)) then
                //Process completed
                let processExitData =
                    {
                        FileName = fileName
                        Arguments = arguments
                        ExitCode = consoleProcess.ExitCode
                        StdOutput = standardOutPutData.ToString().Trim()
                        StdError = standardErrorData.ToString().Trim()
                    }
                processExitData
            else
                //Process timed out
                logger.Debug("Timeout! Close process and throw exception")
                consoleProcess.Close()
                raise (new Exception(String.Format("Process execution timed out: \"{0}\" {1}",fileName,arguments)))
         
        writeProcessExitDataToLog exitData logFileName appendToLogFile

        exitData.ExitCode

    let startConsoleProcessBase3 (fileName, arguments, workingDirectory, timeout:int, inputData, logFileName, appendToLogFile) =
        tryCatchWithMessage startConsoleProcessUnsafe3 (fileName, arguments, workingDirectory,timeout,inputData, logFileName, appendToLogFile) (String.Format("Start of console process ('{0}' {1}) failed.",fileName,arguments))
    
    let startConsoleProcess3 (fileName, arguments, workingDirectory,timeout:int, inputData, logFileName, appendToLogFile) = 
        Logging.genericLoggerResult Logging.LogLevel.Info startConsoleProcessBase3 (fileName, arguments, workingDirectory, timeout, inputData, logFileName, appendToLogFile)

    type System.Diagnostics.Process with
        member x.WaitForExitAsync (?cancellationToken0: CancellationToken) =
            let cancellationToken = 
                match cancellationToken0 with
                |Some c -> c
                |None -> Async.DefaultCancellationToken
            x.EnableRaisingEvents <- true
            let taskCompletionSource = new TaskCompletionSource<obj>()
            let mutable exitedHandler: EventHandler = null 
            exitedHandler <- new EventHandler(
                                    fun _ args -> 
                                            x.Exited.RemoveHandler(exitedHandler)
                                            taskCompletionSource.TrySetResult(null)|>ignore
                                        
                             )
            x.Exited.AddHandler(exitedHandler)
            if(cancellationToken <> Async.DefaultCancellationToken) then
                let action = new System.Action((fun ()->
                                               x.Exited.RemoveHandler(exitedHandler) 
                                               taskCompletionSource.TrySetCanceled()|>ignore
                                            ))                
                cancellationToken.Register(action)|>ignore


            
            taskCompletionSource.Task
    
    let readAsync (addHandler:Action<DataReceivedEventHandler>,removeHandler:Action<DataReceivedEventHandler>,textWriter:TextWriter,cancellationToken: CancellationToken) =
        
        let taskCompletionSource = new TaskCompletionSource<obj>()
        
        let mutable handler: DataReceivedEventHandler = null 
        handler <- new DataReceivedEventHandler(
                                    fun _ e -> 
                                            match e.Data with
                                            |null -> 
                                                removeHandler.Invoke(handler)
                                                taskCompletionSource.TrySetResult(null)|>ignore
                                            |_ -> textWriter.WriteLine(e.Data)
                                        
                                        )
        addHandler.Invoke(handler)
        if( cancellationToken <> Async.DefaultCancellationToken) then
            let action = new System.Action((fun ()->
                                               removeHandler.Invoke(handler)
                                               taskCompletionSource.TrySetCanceled()|>ignore
                                            ))                
            cancellationToken.Register(action)|>ignore
        taskCompletionSource.Task
                            
    let startConsoleProcessUnsafe4 (fileName, arguments, workingDirectory, timeout:int, inputData, logFileName, appendToLogFile) =
        use consoleProcess = new Process()
        consoleProcess.StartInfo.FileName <- fileName
        if(not (System.String.IsNullOrWhiteSpace(arguments))) then
            consoleProcess.StartInfo.Arguments <- arguments
        if(not (System.String.IsNullOrWhiteSpace(workingDirectory))) then
            consoleProcess.StartInfo.WorkingDirectory <- workingDirectory
        consoleProcess.StartInfo.RedirectStandardInput <- true
        consoleProcess.StartInfo.RedirectStandardOutput <- true
        consoleProcess.StartInfo.RedirectStandardError <- true
        consoleProcess.StartInfo.UseShellExecute <- false
        consoleProcess.StartInfo.CreateNoWindow <- true
                
        if(not (System.String.IsNullOrWhiteSpace(inputData))) then
            consoleProcess.StandardInput.Write(inputData)
            consoleProcess.StandardInput.Close()
        use oms = new System.IO.MemoryStream()
        use outputTextWriter = new System.IO.StreamWriter(oms)
        use ems = new System.IO.MemoryStream()
        use errorTextWriter = new System.IO.StreamWriter(ems)
        
        consoleProcess.Start()|>ignore
        let cancellationTokenSource =
            match timeout > 0 with
            |true -> new CancellationTokenSource(timeout)
            |false ->new CancellationTokenSource()
        
        let tasks = 
            seq{ 
                yield consoleProcess.WaitForExitAsync(cancellationTokenSource.Token)
                
                yield 
                    readAsync (
                            Action<DataReceivedEventHandler> (fun x -> 
                                consoleProcess.OutputDataReceived.AddHandler(x)
                                consoleProcess.BeginOutputReadLine())
                            ,
                            Action<DataReceivedEventHandler> (fun x -> consoleProcess.OutputDataReceived.RemoveHandler(x))
                            ,
                            outputTextWriter,
                            cancellationTokenSource.Token
                    )
                
                yield 
                    readAsync (
                            Action<DataReceivedEventHandler> (fun x -> 
                                consoleProcess.ErrorDataReceived.AddHandler(x)
                                consoleProcess.BeginErrorReadLine())
                            ,
                            Action<DataReceivedEventHandler> (fun x -> consoleProcess.ErrorDataReceived.RemoveHandler(x))
                            ,
                            errorTextWriter,
                            cancellationTokenSource.Token
                    )
        
            }
            |>Seq.toArray
        Task.WhenAll(tasks).Wait(timeout)|>ignore
        oms.Position <- 0L
        use outputStreamReader = new StreamReader(oms)
        ems.Position <- 0L
        use errorStreamReader = new StreamReader(ems)
        let processExitData =
                    {
                        FileName = fileName
                        Arguments = arguments
                        ExitCode = consoleProcess.ExitCode
                        StdOutput = outputStreamReader.ReadToEnd()
                        StdError = errorStreamReader.ReadToEnd()
                    }
        
        writeProcessExitDataToLog processExitData logFileName appendToLogFile

        processExitData.ExitCode
    
    let startConsoleProcessBase4 (fileName, arguments, workingDirectory, timeout:int, inputData, logFileName, appendToLogFile) =
        tryCatchWithMessage startConsoleProcessUnsafe4 (fileName, arguments, workingDirectory,timeout,inputData, logFileName, appendToLogFile) (String.Format("Start of console process ('{0}' {1}) failed.",fileName,arguments))
    
    let startConsoleProcess4 (fileName, arguments, workingDirectory,timeout:int, inputData, logFileName, appendToLogFile) = 
        Logging.genericLoggerResult Logging.LogLevel.Info startConsoleProcessBase4 (fileName, arguments, workingDirectory, timeout, inputData, logFileName, appendToLogFile)