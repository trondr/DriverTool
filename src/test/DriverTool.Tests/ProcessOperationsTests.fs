namespace DriverTool.Tests

open NUnit.Framework
open DriverTool

[<TestFixture>]
module ProcessOperationsTests =
    open System.Security.Principal
    

    [<Test>]
    let startConsoleProcessTests () =
        let logFile= @"c:\temp\startConsoleProcess.txt"
        if(System.IO.File.Exists(logFile)) then
            System.IO.File.Delete(logFile)
        Assert.IsFalse(System.IO.File.Exists(logFile),"Log file exists: " + logFile)

        let actualResult = ProcessOperations.startConsoleProcess (@"c:\Windows\System32\cmd.exe","/c dir *.* /s","c:\Program Files",-1,null,@"c:\temp\startConsoleProcessTest.txt",false)
        match actualResult with
        |Ok exitCode -> Assert.AreEqual(0,exitCode)
        |Error ex -> Assert.Fail(ex.ToString())

    [<Test>]
    let startConsoleProcess4Test () = 
        Logging.configureLogging|>ignore
        let fileName = @"C:\WINDOWS\System32\schtasks.exe"
        let arguments = "/Delete /tn \"DriverTool Resume BitLocker Protection\" /f"
        let exitCodeResult = ProcessOperations.startConsoleProcess (fileName,arguments,@"c:\Windows\System32",-1,null,null,false)
        match exitCodeResult with
        |Ok exitCode->Assert.AreEqual(1,exitCode,"ExitCode")
        |Error ex->Assert.Fail(ex.Message)

        