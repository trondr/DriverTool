namespace DriverTool.Tests
open NUnit.Framework
open DriverTool.F
open System

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module FTests =    
    open DriverTool

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

    [<Test>]
    let tryCatchWithMessageTest () =        
        let errorMessage = "FunctionN failed: "
        let testFunctionN number =
            raise(new Exception(errorMessage + number.ToString()))
            number + 1
        let res = tryCatchWithMessage testFunctionN 10 "Dummy message."
        match res with
        |Ok v -> Assert.AreEqual(11,v)
        |Error ex -> 
            Assert.IsTrue((F0.getAccumulatedExceptionMessages ex).Contains(errorMessage),ex.Message)
            Assert.IsTrue((ex.Message).Contains("Dummy message"),F0.getAccumulatedExceptionMessages ex)