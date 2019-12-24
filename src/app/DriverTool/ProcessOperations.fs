namespace DriverTool

module ProcessOperations =    
    let logger = DriverTool.Library.Logging.getLoggerByName("ProcessOperations")

    type ProcessOperations = class end
    open System.Diagnostics
    open System.Text    
    open System    
    open DriverTool.Library.Logging
    open DriverTool.Library.F
    
    type ProcessExitData = {FileName:FileSystem.Path;Arguments:string;ExitCode:int;StdOutput:string;StdError:string}

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

        logger.Debug(new Msg(fun m ->m.Invoke((sprintf "writeToLog command line"))|>ignore))        
        writeToLog logger.Info (sprintf "'%s' %s" (FileSystem.pathValue processExitData.FileName) processExitData.Arguments)
        
        logger.Debug(new Msg(fun m ->m.Invoke((sprintf "writeToLog StdOutput"))|>ignore))
        if (not (String.IsNullOrWhiteSpace(processExitData.StdOutput))) then
            writeToLog logger.Info processExitData.StdOutput
         
        logger.Debug(new Msg(fun m ->m.Invoke((sprintf "writeToLog StdError"))|>ignore))
        if (not (String.IsNullOrWhiteSpace(processExitData.StdError))) then
            writeToLog logger.Error processExitData.StdError

    let startConsoleProcessUnsafe (fileName, arguments, workingDirectory, timeout:int, inputData, logFileName, appendToLogFile) =
        let startInfo = new ProcessStartInfo()
        startInfo.CreateNoWindow <- true
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.FileName <- FileSystem.pathValue fileName
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
            raise (new Exception(sprintf "Process execution timed out: \"%A\" %s" fileName arguments))
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
    
    let startConsoleProcessBase (filePath, arguments, workingDirectory,timeout:int, inputData, logFileName, appendToLogFile) =
        tryCatchWithMessage startConsoleProcessUnsafe (filePath, arguments, workingDirectory,timeout, inputData, logFileName, appendToLogFile) (sprintf "Start of console process ('%s' %s) failed." (FileSystem.pathValue filePath) arguments)
    
    let startConsoleProcess (fileName, arguments, workingDirectory,timeout:int, inputData, logFileName, appendToLogFile) = 
        DriverTool.Library.Logging.genericLoggerResult LogLevel.Info startConsoleProcessBase (fileName, arguments, workingDirectory,timeout, inputData, logFileName, appendToLogFile)
    
    