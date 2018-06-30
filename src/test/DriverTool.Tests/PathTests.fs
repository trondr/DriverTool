namespace DriverTool.Tests
open NUnit.Framework
open DriverTool

[<TestFixture>]
module PathTests  =
    [<Test>]
    [<TestCase(null,"Empty string",false)>]
    [<TestCase("","Empty string",false)>]
    [<TestCase(@"c:\temp","Local path",true)>]
    [<TestCase(@"c:\temp\test.txt","Local path",true)>]
    [<TestCase(@"c:\temp\test.tx?","Local path with wild card",false)>]
    [<TestCase(@"c:\temp\test.*x?","Local path with wild card",false)>]
    let PathCreateTest (pathString:string) (description:string) (expectedSuccess:bool) =
        let path = Path.create pathString
        match path with
        | Ok p -> Assert.IsTrue(expectedSuccess,System.String.Format("Success was not expected for path '{0}' ('{1}')",p, description))
        | Error e -> Assert.IsTrue(not expectedSuccess,System.String.Format("Error was not expected for path '{0}' ('{1}') Exception: {2}",pathString, description, e.Message))

    [<Test>]
    let PathCompareTest() =
        let testPath = @"c:\temp\test.txt"
        let path1Result = Path.create testPath
        match path1Result with
        | Ok path -> Assert.AreEqual(testPath, path.Value)
        | Error ex -> Assert.Fail(sprintf "%s" ex.Message)
        let path2Result = Path.create testPath            
        Assert.AreEqual(path1Result,path2Result,"paths are not equal")

