namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module PathOperationsTests=
    open DriverTool.Library
    
    [<SetUp>]    
    let setup () =
        DriverTool.Library.Logging.configureConsoleLogging()

    [<Test>]
    [<TestCase(@"\\someserver\share",true)>]
    [<TestCase(@"c:\some\local\path",false)>]
    [<Category(TestCategory.UnitTests)>]
    let isUncPathTests (path,exepected) =
        match(result{
            let! testPath = FileSystem.path path
            let actual = PathOperations.isUncPath testPath
            Assert.AreEqual(actual,exepected)
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(getAccumulatedExceptionMessages ex)


    [<Test>]
    [<TestCase(@"\\someserver\share",false,null,"Failed to get drive. Path '\\\\someserver\\share' does not contain a drive.")>]
    [<TestCase(@"c:\some\local\path",true,"c",null)>]
    [<Category(TestCategory.UnitTests)>]
    let getDriveTests (path,isSuccess,exepected:string,expectedErrorMessage:string) =
        match(result{
            let! testPath = FileSystem.path path
            let! actual = PathOperations.getDrive testPath
            Assert.AreEqual(actual,exepected)
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(isSuccess)
        |Result.Error ex -> 
            Assert.IsFalse(isSuccess)
            Assert.AreEqual(expectedErrorMessage,ex.Message)


    [<Test>]
    [<TestCase("Y",@"\\teta410-cm01.teta410.local\PkgSrc$\",true,"")>]
    [<Category(TestCategory.IntegrationTests)>]
    let networkDriveToUncPathTests (drive,expected:string,isSuccess,expectedErrorMessage) =
        match(result{            
            let! actual = PathOperations.networkDriveToUncPath drive
            Assert.AreEqual(expected,FileSystem.pathValue actual)
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(isSuccess)
        |Result.Error ex -> 
            logger.Error(getAccumulatedExceptionMessages ex)
            Assert.IsFalse(isSuccess)
            Assert.AreEqual(expectedErrorMessage,ex.Message)

    [<Test>]
    [<TestCase(@"\\teta410-cm01.teta410.local\PkgSrc$\Applications",false,true,@"\\teta410-cm01.teta410.local\PkgSrc$\Applications","")>]
    [<TestCase(@"y:\Applications",false,true,@"\\teta410-cm01.teta410.local\PkgSrc$\Applications","")>]
    [<TestCase(@"c:\temp",false,false,null,@"Failed to get unc path from 'c:\temp'. The path is a local path.")>]
    [<TestCase(@"c:\temp",true,true,@"\\MININT-MJ4LUGO\c$\temp","")>]
    [<Category(TestCategory.IntegrationTests)>]
    let toUncPathTests (path,useLocalAdminShare,isSuccess,expected:string,expectedErrorMessage:string) =
        match(result{            
            let! testPath = FileSystem.path path
            let! actual = PathOperations.toUncPath useLocalAdminShare testPath            
            Assert.AreEqual(expected,FileSystem.pathValue actual)
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(isSuccess)
        |Result.Error ex -> 
            logger.Error(getAccumulatedExceptionMessages ex)
            Assert.IsFalse(isSuccess)
            Assert.AreEqual(expectedErrorMessage,ex.Message)
    