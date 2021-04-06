namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module WebTests =
    open Common.Logging
    open Rhino.Mocks
    open DriverTool.Library.Web
    open System
    open DriverTool.Library.F
    open DriverTool.Library
    open DriverTool.Library.Logging

    [<SetUp>]    
    let setup () =
        DriverTool.Library.Logging.configureConsoleLogging()

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase(true,true,true,true)>]
    [<TestCase(false,true,false,true)>]
    [<TestCase(false,false,false,false)>]
    [<TestCase(false,false,true,true)>]
    let verifyDownloadTests (hasSameFileHash:bool, isTrusted:bool,ignoreVerificationErrors:bool, expected:bool) =
        let res =
            result
                {
                    let loggerStub = MockRepository.GenerateStub<ILog>()
                    let hasSameFileHasStub (downloadInfo:DownloadInfo) =
                        hasSameFileHash 
        
                    let isTrustedStub (filePath:FileSystem.Path) =
                        isTrusted

                    let! destinationFilePath = FileSystem.path @"c:\temp\test2312123.exe"

                    let downloadInfo = 
                        {
                            SourceUri=new Uri("https://test.local.com")
                            SourceChecksum=""
                            SourceFileSize=123L            
                            DestinationFile=destinationFilePath       
                        }

                    let actual = DriverTool.Library.Web.verifyDownloadBase hasSameFileHasStub isTrustedStub loggerStub downloadInfo ignoreVerificationErrors
                    let result = 
                        match actual with
                        |Result.Ok _ ->
                            Assert.IsTrue(expected,"Did not return expected error")
                            ()
                        |Result.Error ex -> 
                            Assert.IsFalse(expected,"Did not succeed due to: " + ex.Message)
                    return result
                }
        match res with
        |Result.Ok _ -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase(30,30,100,"Test message"," 30% (        30 of        100): Test message                                   \r")>]
    let progressMessageTest (percentage,count,totalCount,message,expectedProgressMessage)=
        let actual = progressMessage percentage count totalCount message
        Assert.AreEqual(expectedProgressMessage,actual)
        

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("http://ftp.hp.com/pub/softpaq/sp65001-65500/sp65001.html","sp65001.html")>]
    let getFileNameFromUrlTests (url, expected) =
        let actual = Web.getFileNameFromUrl url
        Assert.AreEqual(expected,actual)
    
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("http://ftp.hp.com/pub/softpaq/sp65001-65500/sp65001.html","http://ftp.hp.com/pub/softpaq/sp65001-65500")>]
    let getFolderNameFromUrlTests (url, expected) =
        let actual = Web.getFolderNameFromUrl url
        Assert.AreEqual(expected,actual)

    module FileExists = 
      [<Literal>]
      let True = true
      [<Literal>]
      let False = false

    module UseCacheFileExists = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    module Expected = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    [<Test>]
    [<TestCase("c:\\temp\\file1.txt",FileExists.True, UseCacheFileExists.True, Expected.True)>]
    [<TestCase("c:\\temp\\file1.txt",FileExists.False, UseCacheFileExists.True, Expected.False)>]
    [<TestCase("c:\\temp\\file1.txt",FileExists.True, UseCacheFileExists.False, Expected.False)>]
    [<TestCase("c:\\temp\\file1.txt",FileExists.False, UseCacheFileExists.False, Expected.False)>]
    let useCachedVersionBaseTests(destinationFileName:string,fileExists,useCachedFileExists, expected) =        
        let useCacheVersionFileName = destinationFileName + ".usecachedversion"
        let stubFileExists (destinationFileName:string) (useCacheVersionFileName:string) (fileName:string) =
            match fileName with
            |fileName when (fileName.Equals(destinationFileName)) ->
                fileExists
            |fileName when (fileName.Equals(useCacheVersionFileName)) ->            
                useCachedFileExists
            |_ -> 
                Assert.Fail(sprintf "File name not expected: '%s'" fileName)
                false
        let downloadInfo = DriverTool.Library.Web.toDownloadInfo (new Uri("http://dummy")) String.Empty 0L (FileSystem.pathUnSafe destinationFileName)
        let actual =  DriverTool.Library.Web.useCachedVersionBase logger (stubFileExists destinationFileName useCacheVersionFileName) downloadInfo
        Assert.AreEqual(expected,actual,"")


    //Source: https://stackoverflow.com/questions/1105593/get-file-name-from-uri-string-in-c-sharp
    [<Test>]    
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("","",false)>]
    [<TestCase("test","test",false)>]
    [<TestCase("/test","test",false)>]
    [<TestCase("/test.xml","test.xml",false)>]
    [<TestCase("/test.xml?q=1&x=3","test.xml",false)>]
    [<TestCase("test.xml?q=1&x=3","test.xml",false)>]
    [<TestCase("http://www.a.com/test.xml?q=1&x=3","test.xml",true)>]
    [<TestCase("http://www.a.com/test.xml?q=1&x=3#aidjsf","test.xml",true)>]
    [<TestCase("http://www.a.com/a/b/c/d","d",true)>]
    [<TestCase("http://www.a.com/a/b/c/d/","",true)>]
    let getFileNameFromUriTests (url:string, expectedFileName:string, isSuccess:bool) =
        match(result{
            let! uri = toUri url
            let actualFileName = getFileNameFromUri uri
            return actualFileName
        }) with
        |Result.Ok f ->
            Assert.AreEqual(expectedFileName,f,"File name was not expected.")
            Assert.IsTrue(isSuccess,"Expected to succede, but failed")
        |Result.Error ex ->
            Assert.IsFalse(isSuccess,sprintf "Expected to fail, but succeded. %s" ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]    
    let toOptionalStringTest_Null_None () =
        let actual = toOptionalString null
        Assert.AreEqual(None,actual)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]    
    let toOptionalStringTest_EmptyString_None () =
        let actual = toOptionalString ""
        Assert.AreEqual(None,actual)
    
    [<Test>]
    [<Category(TestCategory.UnitTests)>]    
    let toOptionalStringTest_StringWithOnlyWhiteSpace_None () =
        let actual = toOptionalString "   "
        Assert.AreEqual(None,actual)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]    
    let toOptionalStringTest_String_Some () =
        let actual = toOptionalString "SomeString"
        Assert.AreEqual(Some "SomeString",actual)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]     
    let toOptionalSize_0_None () =
        let actual = toOptionalSize 0L
        Assert.AreEqual(None,actual)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]     
    let toOptionalSize_Negative_None () =
        let actual = toOptionalSize -12L
        Assert.AreEqual(None,actual)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]     
    let toOptionalSize_Postive_None () =
        let actual = toOptionalSize 12L
        Assert.AreEqual(Some 12L,actual)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]    
    let toWebSourceFileTests_Fail () =
        let isSuccess = false
        match(result{
            let url = ""
            let size = 0L
            let checksum = ""
            let! actual = toWebFileSource url checksum size 
            return actual
        })with
        |Result.Ok v ->            
            Assert.IsTrue(isSuccess,sprintf "Expected to succede, but failed. %A" v)
        |Result.Error ex ->
            Assert.IsFalse(isSuccess,sprintf "Expected to fail, but succeded. %s" ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]    
    let toWebSourceFileTests_Success () =
        let isSuccess = true
        match(result{
            let url = "htt://some.com/test.xml"
            let size = 0L
            let checksum = ""
            let! actual = toWebFileSource url checksum size 
            return actual
        })with
        |Result.Ok actual ->            
            let expected = {Url = new System.Uri("htt://some.com/test.xml");Checksum=None;Size=None;FileName="test.xml"}
            Assert.AreEqual(expected,actual)
            Assert.IsTrue(isSuccess,sprintf "Expected to succede, but failed. %A" actual)
        |Result.Error ex ->
            Assert.IsFalse(isSuccess,sprintf "Expected to fail, but succeded. %s" ex.Message)

    module HasSameFileHash = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    module UseCachedVersion = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false
    
[<TestFixture>]
module ManualWebTest =
    open DriverTool.Library.F
    open DriverTool.Library
    open DriverTool.Library.Logging
    
    [<SetUp>]    
    let setup () =
        DriverTool.Library.Logging.configureConsoleLogging()

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    [<TestCase("http://downloads.dell.com/FOLDER05405866M/1/7480-win10-A10-2CHK6.CAB",@"c:\temp\7480-win10-A10-2CHK6.CAB")>]
    let downloadFileTest (sourceUrl,destinationFile) =
        let res = 
            result
                {
                    let! uri = Web.toUri sourceUrl
                    let! destinationFilePath = FileSystem.path destinationFile
                    let! downloadedDestinationFilePath = Web.downloadFile reportProgressStdOut uri true destinationFilePath
                    return downloadedDestinationFilePath
                }
        match res with
        |Result.Ok _ -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)
        ()
               
            



