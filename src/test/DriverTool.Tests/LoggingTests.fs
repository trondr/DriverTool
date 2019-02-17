namespace DriverTool.Tests
open NUnit.Framework
open DriverTool
open Logging
open System

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module LoggingTests =    
    [<Test>]    
    let getDurationStringTestInMilliseconds () =
        let expected = "43ms"
        let actual = getDurationString (new TimeSpan(0,0,0,0,43))
        Assert.AreEqual(expected,actual,"Unexpected value")
        
    [<Test>]    
    let getDurationStringTestInSeconds () =
        let expected = "2s 43ms"
        let actual = getDurationString (new TimeSpan(0,0,0,2,43))
        Assert.AreEqual(expected,actual,"Unexpected value") 

    [<Test>]
    let getDurationStringTestInMinutes () =
        let expected = "3m 2s 43ms"
        let actual = getDurationString (new TimeSpan(0,0,3,2,43))
        Assert.AreEqual(expected,actual,"Unexpected value") 

    [<Test>]
    let getDurationStringTestInHours () =
        let expected = "4h 3m 2s"
        let actual = getDurationString (new TimeSpan(0,4,3,2,43))
        Assert.AreEqual(expected,actual,"Unexpected value") 


    [<Test>]
    let getParametersStringArrayOfString () =
        let expected = "[\"arg1\",\"arg2\"]:System.String[]"
        let input = [|"arg1";"arg2"|]
        let actual = getParametersString input
        Assert.AreEqual(expected,actual,"Unexpected value") 

    [<Test>]
    let getParametersStringArrayOfInt () =
        let expected = "[1,2]:System.Int32[]"
        let input = [|1;2|]
        let actual = getParametersString input
        Assert.AreEqual(expected,actual,"Unexpected value") 

    [<Test>]
    let getParametersTuple () =
        let expected = "(\"arg1\",\"arg2\")"
        let input = ("arg1","arg2")
        let actual = getParametersString input
        Assert.AreEqual(expected,actual,"Unexpected value") 

    [<Test>]
    let getFuncLoggerNameTest1 () =
        let expected = "DriverTool.Tests.LoggingTests+testFuncOk"
        let testFuncOk () : Result<_,Exception> =
            System.Threading.Thread.Sleep(505)
            Result.Ok expected
        let actual = getFuncLoggerName testFuncOk
        Assert.AreEqual(expected, actual)

    [<Test>]
    let getFuncLoggerNameTest2 () =
        let expected = "DriverTool.Tests.LoggingTests+testFuncError"
        let testFuncError () : Result<_,Exception> =
            System.Threading.Thread.Sleep(505)
            Result.Error (new Exception(expected))
        let actual = getFuncLoggerName testFuncError
        Assert.AreEqual(expected, actual)

    [<Test>]
    let genericLoggerResultTest_Success () =
        Logging.configureLogging()
        let expected = "Status:OK"
        let testFuncOk () : Result<_,Exception> =
            System.Threading.Thread.Sleep(505)
            Result.Ok expected
        let actualResult = genericLoggerResult LogLevel.Info testFuncOk ()
        match actualResult with
        |Ok actual -> Assert.AreEqual(expected,actual)
        |Result.Error ex -> Assert.Fail(ex.Message)
    
    [<Test>]
    let genericLoggerResultTest_Error () =
        Logging.configureLogging()
        let expected = "Status:ERROR"
        let testFuncError () : Result<_,Exception> =
            System.Threading.Thread.Sleep(505)
            Result.Error (new Exception(expected))
        let actualResult = genericLoggerResult LogLevel.Info testFuncError ()
        match actualResult with
        |Ok actual -> Assert.Fail("Test should not return ok result.")
        |Result.Error ex -> Assert.AreEqual(expected,ex.Message)