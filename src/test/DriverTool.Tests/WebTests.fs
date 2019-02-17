namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module WebTests =

    
    open log4net
    open Rhino.Mocks
    open DriverTool.Web
    open DriverTool
    open System

    [<Test>]
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
    [<TestCase(30,30,100,"Test message"," 30% (        30 of        100): Test message                                   \r")>]
    let progressMessageTest (percentage,count,totalCount,message,expectedProgressMessage)=
        let actual = progressMessage percentage count totalCount message
        Assert.AreEqual(expectedProgressMessage,actual)
        

[<TestFixture>]
[<Category(TestCategory.ManualTests)>]
module ManualWebTest =
    open DriverTool
    open DriverTool
    
    [<Test>]
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