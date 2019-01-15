namespace DriverTool.Tests
open NUnit.Framework
open DriverTool.F

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
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
        |Ok _ -> Assert.Fail("This test should have failed")
        |Error e -> Assert.AreEqual("test",e.Message,"Unexpected exception message")    
