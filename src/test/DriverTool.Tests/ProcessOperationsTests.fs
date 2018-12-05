namespace DriverTool.Tests

open NUnit.Framework
open DriverTool

[<TestFixture>]
module ProcessOperationsTests =
    

    [<Test>]
    let startConsoleProcessTests () =
        let logFile= @"c:\temp\startConsoleProcess.txt"
        if(System.IO.File.Exists(logFile)) then
            System.IO.File.Delete(logFile)
        Assert.IsFalse(System.IO.File.Exists(logFile),"Log file exists: " + logFile)

        let actualResult = ProcessOperations.startConsoleProcess (@"c:\Windows\System32\cmd.exe","/c dir *.* /s","c:\Program Files",@"c:\temp\startConsoleProcess.txt",false)
        match actualResult with
        |Ok exitCode -> Assert.AreEqual(0,exitCode)
        |Error ex -> Assert.Fail(ex.Message)
