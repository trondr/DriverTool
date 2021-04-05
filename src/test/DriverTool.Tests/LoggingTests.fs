namespace DriverTool.Tests
open NUnit.Framework
open DriverTool.Library.Logging
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

    open DriverTool.Library.PackageXml

    [<Test>]
    let logComplexObjectTest1 () =
        configureLogging()
        let sccmPacage =
            {
                ReadmeFile =                                         
                    {
                    Url = "";
                    Checksum = "";
                    FileName = "";
                    Size=0L;
                    }
                InstallerFile=
                    {
                        Url="";
                        Checksum="{sometest}"
                        FileName=""
                        Size=0L
                    }
                Released=(new DateTime(2021,03,28))
                Os= "WIN10"
                OsBuild="20H2"
            }        
        Assert.DoesNotThrow(fun () -> logger.Info( sprintf "%A" sccmPacage))
        Assert.DoesNotThrow(fun () -> logger.Info(sprintf "Sccm packge: %A" sccmPacage))
        

        ()