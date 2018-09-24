namespace DriverTool.Tests
open NUnit.Framework
open DriverTool
open Logging
open System

[<TestFixture>]
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
        let expected = "[|\"arg1\";\"arg2\"|]"
        let input = [|"arg1";"arg2"|]
        let actual = getParametersString input
        Assert.AreEqual(expected,actual,"Unexpected value") 

    [<Test>]
    let getParametersStringArrayOfInt () =
        let expected = "[|1;2|]"
        let input = [|1;2|]
        let actual = getParametersString input
        Assert.AreEqual(expected,actual,"Unexpected value") 
