namespace DriverTool.Tests

open NUnit.Framework
open DriverTool
open DriverTool.Library

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module ProcessOperationsTests =
    open DriverTool.Library.Logging

    [<Test>]
    let startConsoleProcessTests () =
        let logFile= @"c:\temp\startConsoleProcess.txt"
        if(System.IO.File.Exists(logFile)) then
            System.IO.File.Delete(logFile)
        Assert.IsFalse(System.IO.File.Exists(logFile),"Log file exists: " + logFile)

        let actualResult = ProcessOperations.startConsoleProcess (FileSystem.pathUnSafe @"c:\Windows\System32\cmd.exe","/c dir *.* /s","c:\Program Files",-1,null,@"c:\temp\startConsoleProcessTest.txt",false)
        match actualResult with
        |Ok exitCode -> Assert.AreEqual(0,exitCode)
        |Result.Error ex -> Assert.Fail(ex.ToString())

    [<Test>]
    let startConsoleProcess4Test () = 
        configureLogging|>ignore
        let fileName = @"C:\WINDOWS\System32\schtasks.exe"
        let arguments = "/Delete /tn \"DriverTool Resume BitLocker Protection\" /f"
        let exitCodeResult = ProcessOperations.startConsoleProcess (FileSystem.pathUnSafe fileName,arguments,@"c:\Windows\System32",-1,null,null,false)
        match exitCodeResult with
        |Ok exitCode->Assert.AreEqual(1,exitCode,"ExitCode")
        |Result.Error ex->Assert.Fail(ex.Message)

        