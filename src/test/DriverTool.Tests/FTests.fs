namespace DriverTool.Tests
open NUnit.Framework
open System

[<TestFixture>]
module FTests =        
    open DriverTool.Library.F0
    open DriverTool.Library.F

    [<Test>]   
    [<Category(TestCategory.UnitTests)>]
    let tryCatchSuccessTest() =
        let testfunctionSuccess p1 =                
            "test value" + p1
        let actualResult = tryCatch None testfunctionSuccess ""
        match actualResult with
        |Ok actual -> Assert.AreEqual("test value",actual,"Unexpected value")
        |Error e -> Assert.Fail(sprintf "This test failed unexpectedly due: %s" e.Message)        

    [<Test>]    
    [<Category(TestCategory.UnitTests)>]
    let tryCatchErrorTest() =
        let testfunctionSuccess p1 =    
            raise (new System.Exception("test"))
            "test value" + p1
        let actualResult = tryCatch None testfunctionSuccess ""
        match actualResult with
        |Ok _ -> Assert.Fail("This test should have failed")
        |Error e -> Assert.AreEqual("test",e.Message,"Unexpected exception message")    

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let tryCatchWithMessageTest () =        
        let errorMessage = "FunctionN failed: "
        let testFunctionN number =
            raise(new Exception(errorMessage + number.ToString()))
            number + 1
        let res = tryCatch (Some "Dummy message.") testFunctionN 10
        match res with
        |Ok v -> Assert.AreEqual(11,v)
        |Error ex -> 
            Assert.IsTrue((getAccumulatedExceptionMessages ex).Contains(errorMessage),ex.Message)
            Assert.IsTrue((ex.Message).Contains("Dummy message"),getAccumulatedExceptionMessages ex)
    
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let exceptionToResultTest () =
        let subfunctionThatThrows() =
            raise (new Exception("subfunctionThatThrows."))
            0
        let functionThatThrows() =
            subfunctionThatThrows()

        let actual = 
            try
        
                Result.Ok (functionThatThrows())
            with
            |ex -> toErrorResult ex  (Some "Test Message.")

        match actual with
        |Ok v -> Assert.Fail("Expected error result, got success result")
        |Error ex -> 
            Assert.AreEqual("Test Message. subfunctionThatThrows.", getAccumulatedExceptionMessages ex)
            printf "%A" ex
            Assert.IsTrue(ex.ToString().StartsWith("System.Exception: Test Message. ---> System.Exception: subfunctionThatThrows."))

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getAllExceptionsTests_One_Exception() =
        let sequence = 
            seq {
                yield Result.Ok(123)
                yield Result.Error(new Exception("Test Exception 1"))
            }
        let actual = 
            getAllExceptions sequence
            |> Seq.toArray
        Assert.AreEqual(1,actual.Length)
        Assert.AreEqual("Test Exception 1",actual.[0])
    
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getAllExceptionsTests_Empty_Sequence() =
        let sequence = Seq.empty<Result<int,Exception>>            
        let actual = 
            getAllExceptions sequence
            |> Seq.toArray
        Assert.AreEqual(0,actual.Length)        

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let toAccumulatedResult_One_Exception() =
        let expectedSuccess = false
        let sequence = 
            seq {
                yield Result.Ok(123)
                yield Result.Error(new Exception("Test Exception 1"))
            }
        let actual = 
            toAccumulatedResult sequence 
        match actual with
        |Error ex -> 
            Assert.False(expectedSuccess,"Expected result to be Ok but was Error.")
            Assert.IsTrue(ex.Message.Contains("Test Exception 1"))
        |Ok v -> Assert.IsTrue(expectedSuccess,"Expected result to be Error but was Ok.")

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let toAccumulatedResult_No_Exceptions() =
        let expectedSuccess = true
        let sequence = 
            seq {
                yield Result.Ok(123)
                yield Result.Ok(234)
            }
        let actual = 
            toAccumulatedResult sequence 
        match actual with
        |Error ex -> Assert.False(expectedSuccess,"Expected result to be Ok but was Error.")
        |Ok v -> 
            Assert.IsTrue(expectedSuccess,"Expected result to be Error but was Ok.")
            Assert.AreEqual(2,(v|>Seq.toArray|>Array.length))

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let toAccumulatedResult_Empty_Sequence() =
        let expectedSuccess = true
        let sequence = Seq.empty<Result<string,Exception>>
        let actual = 
            toAccumulatedResult sequence   
        match actual with
        |Error ex -> Assert.False(expectedSuccess,"Expected result to be Ok but was Error.")
        |Ok v -> 
            Assert.IsTrue(expectedSuccess,"Expected result to be Error but was Ok.")
            Assert.AreEqual(0,(v|>Seq.toArray|>Array.length))
    