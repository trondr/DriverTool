namespace DriverTool.Tests
open NUnit.Framework
open F

[<TestFixture>]
module FTests =    
    [<Test>]    
    let tryCatchSuccessTest() =
        let testfunctionSuccess p1 =                
            "test value" + p1
        let actualResult = tryCatch testfunctionSuccess ""
        match actualResult with
        |Ok actual -> Assert.AreEqual("test value",actual,"Unexpected value")
        |Error e -> Assert.Fail(sprintf "This test failed unexpectedly due: %s" e.Message)        

    [<Test>]    
    let tryCatchErrorTest() =
        let testfunctionSuccess p1 =    
            raise (new System.Exception("test"))
            "test value" + p1
        let actualResult = tryCatch testfunctionSuccess ""
        match actualResult with
        |Ok actual -> Assert.Fail("This test should have failed")
        |Error e -> Assert.AreEqual("test",e.Message,"Unexpected exception message")
    //match result with
    //    | Ok value -> printf "%s" value
    //    | Error ex -> printf "%s" ex.Message
