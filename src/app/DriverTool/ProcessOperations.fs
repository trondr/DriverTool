namespace DriverTool
open F

module ProcessOperations =
    let logger = Logging.getLoggerByName("ProcessOperations")

    type ProcessOperations = class end
    open System.Diagnostics
    
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
    