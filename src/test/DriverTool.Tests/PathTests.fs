namespace DriverTool.Tests
open NUnit.Framework
open DriverTool.Library

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module PathTests  =
    [<Test>]
    [<TestCase(null,"Empty string",false)>]
    [<TestCase("","Empty string",false)>]
    [<TestCase(@"c:\temp","Local path",true)>]
    [<TestCase(@"c:\temp\test.txt","Local path",true)>]
    [<TestCase(@"c:\temp\test.tx?","Local path with wild card",false)>]
    [<TestCase(@"c:\temp\test.*x?","Local path with wild card",false)>]
    let PathCreateTest (pathString:string) (description:string) (expectedSuccess:bool) =
        let path = FileSystem.path pathString
        match path with
        | Ok p -> Assert.IsTrue(expectedSuccess,sprintf "Success was not expected for path '%s' ('%s')" (FileSystem.pathValue p) description)
        | Error e -> Assert.IsTrue(not expectedSuccess,sprintf "Error was not expected for path '%s' ('%s') Exception: %s" pathString description e.Message)
    
    [<Test>]
    [<TestCase(null,"Empty string",false)>]
    [<TestCase("","Empty string",false)>]
    [<TestCase(@"c:\temp","Local path",true)>]
    [<TestCase(@"c:\temp\test.txt","Local path",true)>]
    [<TestCase(@"c:\temp\test.tx?","Local path with wild card",false)>]
    [<TestCase(@"c:\temp\test.*x?","Local path with wild card",false)>]
    let Path2CreateTest (pathString:string) (description:string) (expectedSuccess:bool) =
        let path = FileSystem.path pathString
        match path with
        | Ok p -> 
            printfn "Path: %A" p
            Assert.IsTrue(expectedSuccess,sprintf "Success was not expected for path '%s' ('%s')" (FileSystem.pathValue p) description)
        | Error e -> 
            printfn "Expected error: %s" e.Message
            Assert.IsTrue(not expectedSuccess,sprintf "Error was not expected for path '%s' ('%s') Exception: %s" pathString description e.Message)

    [<Test>]
    let PathCompareTest() =
        let testPath = @"c:\temp\test.txt"
        let path1Result = FileSystem.path testPath
        match path1Result with
        | Ok path -> Assert.AreEqual(testPath, FileSystem.pathValue path)
        | Error ex -> Assert.Fail(sprintf "%s" ex.Message)
        let path2Result = FileSystem.path testPath            
        Assert.AreEqual(path1Result,path2Result,"paths are not equal")

