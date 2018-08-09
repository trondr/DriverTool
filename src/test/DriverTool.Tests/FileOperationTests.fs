namespace DriverTool.Tests
open System
open NUnit.Framework
open DriverTool

open FileOperations

[<TestFixture>]
module FileOperationTests =

    type TemporaryFile() =
        let createTestFile =                        
            match Path.create (System.IO.Path.GetTempFileName()) with
            | Ok path -> path
            | Error ex -> raise ex
        
        member _this.Path = createTestFile
        interface IDisposable with
            member this.Dispose() =
                match System.IO.File.Exists(this.Path.Value) with
                | true -> System.IO.File.Delete(this.Path.Value)
                | false -> ()
        

    [<Test>]
    let ensureFileDoesNotExistTest_FileExists() =
        use temporaryFile = new TemporaryFile()
        let path = temporaryFile.Path
        let actualResult = ensureFileDoesNotExist false path
        match actualResult with
        |Ok p -> Assert.Fail((sprintf "The test should have failed. File %s exists" p.Value))
        |Error ex -> Assert.AreEqual(String.Format("File exists: '{0}'", path.Value),ex.Message)

    [<Test>]
    let ensureFileDoesNotExistTest_FileExists_Overwrite() =
        use temporaryFile = new TemporaryFile()
        let path = temporaryFile.Path
        let actualResult = ensureFileDoesNotExist true path
        match actualResult with
        |Ok p -> Assert.AreEqual(path.Value, p.Value)
        |Error ex -> Assert.Fail("Test failed")

    [<Test>]
    let ensureFileDoesNotExistTest_DoesNotExists() =
        use temporaryFile = new TemporaryFile()
        let path = temporaryFile.Path
        System.IO.File.Delete(path.Value)
        let actualResult = ensureFileDoesNotExist false path
        match actualResult with
        |Ok p -> Assert.AreEqual(path.Value, p.Value)
        |Error ex -> Assert.Fail("Test failed")