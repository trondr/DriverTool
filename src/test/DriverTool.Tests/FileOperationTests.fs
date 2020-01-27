namespace DriverTool.Tests
open NUnit.Framework
open DriverTool.Library.DirectoryOperations
open DriverTool.Library
open DriverTool.Library.FileOperations

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module FileOperationTests =

    let logger = Common.Logging.Simple.ConsoleOutLogger("FileOperationTests",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")
    
    [<Test>]
    let ensureFileDoesNotExistTest_FileExists() =
        use temporaryFile = new TemporaryFile()
        let path = temporaryFile.Path
        let actualResult = ensureFileDoesNotExist false path
        match actualResult with
        |Ok p -> Assert.Fail((sprintf "The test should have failed. File %s exists" (FileSystem.pathValue p)))
        |Error ex -> Assert.AreEqual(sprintf "File allready exists: '%s'. " (FileSystem.pathValue path),ex.Message)

    [<Test>]
    let ensureFileDoesNotExistTest_FileExists_Overwrite() =
        use temporaryFile = new TemporaryFile()
        let path = temporaryFile.Path
        let actualResult = ensureFileDoesNotExist true path
        match actualResult with
        |Ok p -> Assert.AreEqual(FileSystem.pathValue path, FileSystem.pathValue p)
        |Error ex -> Assert.Fail("Test failed")

    [<Test>]
    let ensureFileDoesNotExistTest_DoesNotExists() =
        use temporaryFile = new TemporaryFile()
        let path = temporaryFile.Path
        System.IO.File.Delete(FileSystem.pathValue path)
        let actualResult = ensureFileDoesNotExist false path
        match actualResult with
        |Ok p -> Assert.AreEqual(path, p)
        |Error ex -> Assert.Fail("Test failed")

    [<Test>]
    [<TestCase(@"c:\temp\text.txt","txt",true,"N/A")>]
    [<TestCase(@"c:\temp\text.txt",".txt",true,"N/A")>]
    [<TestCase(@"c:\temp\text.txt",".tst",false,"File does not have extension '.tst'")>]
    let ensureFileExtensionTest (pathString,extension,isSuccess:bool,expectedErrorMessage:string) =
        match(result{
            let! pathToCheck = FileSystem.path pathString
            let! path = ensureFileExtension extension pathToCheck
            return path                         
        }) with
        |Ok p -> 
            Assert.IsTrue(isSuccess, "Did not expect success" )
        |Error ex -> 
            Assert.IsFalse(isSuccess, "Did not expect error:" + ex.Message )
            Assert.IsTrue(ex.Message.StartsWith(expectedErrorMessage),"Error message did not start with: " + expectedErrorMessage)

        
    
    [<Test>]
    let compareFileTest () =
        match(result{
            use temporaryFolder1 = new TemporaryFolder(logger)
            let! temporaryFolder1Path = temporaryFolder1.FolderPath
            let! temporaryFile1 = createRandomFile logger temporaryFolder1Path
            let! existingTemporaryFile1 = FileSystem.existingFilePath temporaryFile1

            use temporaryFolder2 = new TemporaryFolder(logger)
            let! temporaryFolder2Path = temporaryFolder2.FolderPath
            let! temporaryFile2 = createRandomFile logger temporaryFolder2Path
            let! existingTemporaryFile2 = FileSystem.existingFilePath temporaryFile2

            let! actual = compareFile existingTemporaryFile1 existingTemporaryFile2
            Assert.IsFalse(actual, sprintf "'%s' != '%s'" (FileSystem.pathValue existingTemporaryFile1) (FileSystem.pathValue existingTemporaryFile2))
                                                        
            return temporaryFolder1Path              
        })with
        |Result.Ok v -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)
