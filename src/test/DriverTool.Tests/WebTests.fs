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

        

