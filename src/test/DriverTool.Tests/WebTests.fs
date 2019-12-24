namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module WebTests =

    
    open Common.Logging
    open Rhino.Mocks
    open DriverTool.Web
    open DriverTool
    open System
    open NUnit.Framework
    open DriverTool.Library.F

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

                    let actual = DriverTool.Web.verifyDownloadBase (hasSameFileHasStub,isTrustedStub,loggerStub,downloadInfo,ignoreVerificationErrors)
                    let result = 
                        match actual with
                        |Ok _ ->
                            Assert.IsTrue(expected,"Did not return expected error")
                            ()
                        |Error ex -> 
                            Assert.IsFalse(expected,"Did not succeed due to: " + ex.Message)
                    return result
                }
        match res with
        |Ok _ -> Assert.IsTrue(true)
        |Error ex -> Assert.Fail(ex.Message)

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
        let downloadInfo = DriverTool.Web.toDownloadInfo (new Uri("http://dummy")) String.Empty 0L (FileSystem.pathUnSafe destinationFileName)
        let actual =  DriverTool.Web.useCachedVersionBase logger (stubFileExists destinationFileName useCacheVersionFileName) downloadInfo
        Assert.AreEqual(expected,actual,"")

[<TestFixture>]
module ManualWebTest =
    open DriverTool.Library.F
    open DriverTool.Library
    open DriverTool
    
    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    [<TestCase("http://downloads.dell.com/FOLDER05405866M/1/7480-win10-A10-2CHK6.CAB",@"c:\temp\7480-win10-A10-2CHK6.CAB")>]
    let downloadFileTest (sourceUrl,destinationFile) =
        let res = 
            result
                {
                    let! uri = Web.toUri sourceUrl
                    let! destinationFilePath = FileSystem.path destinationFile
                    let! downloadedDestinationFilePath = Web.downloadFile (uri,true,destinationFilePath)
                    return downloadedDestinationFilePath
                }
        match res with
        |Ok _ -> Assert.IsTrue(true)
        |Error ex -> Assert.Fail(ex.Message)
        ()
