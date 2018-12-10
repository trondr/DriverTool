namespace DriverTool
open F

module ProcessOperations =
    let logger = Logging.getLoggerByName("ProcessOperations")

    type ProcessOperations = class end
    open System.Diagnostics
    open System.Text    
    open System    
    
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

    let startConsoleProcessUnsafe (fileName, arguments, workingDirectory, logFileName, appendToLogFile) =
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
        let consoleProcess = new Process()
        consoleProcess.StartInfo <- startInfo

        let stdOutBuilder = new StringBuilder()
        let stdErrorBuilder = new StringBuilder()

        let outputDataReceivedHandler =
           new DataReceivedEventHandler(fun s e -> stdOutBuilder.AppendLine(e.Data) |> ignore)
        let errorDataReceivedHandler =
           new DataReceivedEventHandler(fun s e -> stdErrorBuilder.AppendLine(e.Data) |> ignore)

        consoleProcess.OutputDataReceived.AddHandler(outputDataReceivedHandler)
        consoleProcess.ErrorDataReceived.AddHandler(errorDataReceivedHandler)

        consoleProcess.Start() |> ignore
        consoleProcess.BeginOutputReadLine()
        consoleProcess.BeginErrorReadLine()
        consoleProcess.WaitForExit()
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
        
        writeToLog logger.Info (String.Format("'{0}' {1}",processExitData.FileName,processExitData.Arguments))
                
        if (not (String.IsNullOrWhiteSpace(processExitData.StdOutput))) then
            writeToLog logger.Info processExitData.StdOutput
         
        if (not (String.IsNullOrWhiteSpace(processExitData.StdError))) then
            writeToLog logger.Error processExitData.StdError
        
        processExitData.ExitCode
    
    let startConsoleProcessBase (fileName, arguments, workingDirectory, logFileName, appendToLogFile) =
        tryCatchWithMessage startConsoleProcessUnsafe (fileName, arguments, workingDirectory, logFileName, appendToLogFile) (String.Format("Start of console process ('{0}' {1}) failed.",fileName,arguments))
    
    let startConsoleProcess (fileName, arguments, workingDirectory, logFileName, appendToLogFile) =        
        Logging.genericLoggerResult Logging.LogLevel.Info startConsoleProcessBase (fileName, arguments, workingDirectory, logFileName, appendToLogFile)