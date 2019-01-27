namespace DriverTool.Tests
open System
open NUnit.Framework
open DriverTool

open FileOperations

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module FileOperationTests =

    type TemporaryFile() =
        let createTestFile =                        
            match FileSystem.path (System.IO.Path.GetTempFileName()) with
            | Ok path -> path
            | Error ex -> raise ex
        
        member _this.Path = createTestFile
        interface IDisposable with
            member this.Dispose() =
                match System.IO.File.Exists(FileSystem.pathValue this.Path) with
                | true -> System.IO.File.Delete(FileSystem.pathValue this.Path)
                | false -> ()
        

    [<Test>]
    let ensureFileDoesNotExistTest_FileExists() =
        use temporaryFile = new TemporaryFile()
        let path = temporaryFile.Path
        let actualResult = ensureFileDoesNotExist (false, path)
        match actualResult with
        |Ok p -> Assert.Fail((sprintf "The test should have failed. File %s exists" (FileSystem.pathValue p)))
        |Error ex -> Assert.AreEqual(String.Format("File allready exists: '{0}'. ", FileSystem.pathValue path),ex.Message)

    [<Test>]
    let ensureFileDoesNotExistTest_FileExists_Overwrite() =
        use temporaryFile = new TemporaryFile()
        let path = temporaryFile.Path
        let actualResult = ensureFileDoesNotExist (true, path)
        match actualResult with
        |Ok p -> Assert.AreEqual(FileSystem.pathValue path, FileSystem.pathValue p)
        |Error ex -> Assert.Fail("Test failed")

    [<Test>]
    let ensureFileDoesNotExistTest_DoesNotExists() =
        use temporaryFile = new TemporaryFile()
        let path = temporaryFile.Path
        System.IO.File.Delete(FileSystem.pathValue path)
        let actualResult = ensureFileDoesNotExist (false, path)
        match actualResult with
        |Ok p -> Assert.AreEqual(path, p)
        |Error ex -> Assert.Fail("Test failed")