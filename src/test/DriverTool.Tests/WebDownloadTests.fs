namespace DriverTool.Tests

open NUnit.Framework
open DriverTool.Library
open DriverTool.Library.F
open DriverTool.Library.WebDownload

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module WebDownloadTests =

    let testLogger = Common.Logging.Simple.ConsoleOutLogger("EmbeddedResourceTest",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")

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
    let useCachedVersionTests(destinationFileName:string,fileExists,useCachedFileExists, expected) =        
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
        let destinationFile = (DriverTool.Library.FileSystem.pathUnSafe destinationFileName)
        let actual =  DriverTool.Library.WebDownload.useCachedVersion' logger (stubFileExists destinationFileName useCacheVersionFileName) destinationFile
        Assert.AreEqual(expected,actual,"")


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
    
    [<Test>]
    [<TestCase("c:\\temp\\file1.txt",HasSameFileHash.False, UseCachedVersion.False, Expected.True)>]
    [<TestCase("c:\\temp\\file1.txt",HasSameFileHash.False, UseCachedVersion.True, Expected.False)>]
    [<TestCase("c:\\temp\\file1.txt",HasSameFileHash.True, UseCachedVersion.False, Expected.False)>]    
    [<TestCase("c:\\temp\\file1.txt",HasSameFileHash.True, UseCachedVersion.True, Expected.False)>]

    let downloadIsRequiredTests (destinationFile,doesHaveSameFileHash,doUseCachedVersion, expected) =
        
        let destinationFilePath = (FileSystem.pathUnSafe destinationFile)
        let stubHasSameFileHash (destinationFilePath:FileSystem.Path,sourceChecksum:string option,sourceFileSize:System.Int64 option) =
            doesHaveSameFileHash

        let stubUseCachedVersion (destinationFile:FileSystem.Path) =
            doUseCachedVersion

        let actual = downloadIsRequired' testLogger stubHasSameFileHash stubUseCachedVersion (Some "SomeDummyCheckSum") (Some 12345L) destinationFilePath
        Assert.AreEqual(expected,actual,"")

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase(30,30,100,"Test message"," 30% (        30 of        100): Test message                                   \r")>]
    let progressMessageTest (percentage,count,totalCount,message,expectedProgressMessage)=
        let actual = progressMessage percentage count totalCount message
        Assert.AreEqual(expectedProgressMessage,actual)

    module IsTrusted = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    module IgnoreVerificationErrors = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase(HasSameFileHash.True, IsTrusted.True,IgnoreVerificationErrors.True,Expected.True)>]
    [<TestCase(HasSameFileHash.False, IsTrusted.True,IgnoreVerificationErrors.False,Expected.True)>]
    [<TestCase(HasSameFileHash.False, IsTrusted.False,IgnoreVerificationErrors.False,Expected.False)>]
    [<TestCase(HasSameFileHash.False, IsTrusted.False,IgnoreVerificationErrors.True,Expected.True)>]
    let verifyDownloadTests (hasSameFileHash:bool, isTrusted:bool,ignoreVerificationErrors:bool, expected:bool) =
        let res =
            result
                {
                    
                    let hasSameFileHasStub (destinationFile:FileSystem.Path,sourceHash:string option,sourceSize: System.Int64 option) =
                        hasSameFileHash 
        
                    let isTrustedStub (filePath:FileSystem.Path) =
                        isTrusted

                    let! destinationFilePath = FileSystem.path @"c:\temp\test2312123.exe"

                    let download = 
                        WebFile2.DownloadWebFile ({
                            Url=new System.Uri("https://test.local.com")
                            Checksum=None
                            Size=Some 123L            
                            FileName=System.IO.Path.GetFileName(FileSystem.pathValue destinationFilePath)
                        }, ({DestinationFile=destinationFilePath}))

                    let actual = DriverTool.Library.WebDownload.verifyDownload' logger hasSameFileHasStub isTrustedStub ignoreVerificationErrors download
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

    module DownloadIsRequired = 
           [<Literal>]
           let True = true
           [<Literal>]
           let False = false

    module DownloadSucceeds = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    module VerifyDownloadSucceeds = 
        [<Literal>]
        let True = true
        [<Literal>]
        let False = false

    [<Test>]
    [<TestCase(DownloadIsRequired.True,DownloadSucceeds.True,VerifyDownloadSucceeds.True,Expected.True,"N/A")>]
    [<TestCase(DownloadIsRequired.True,DownloadSucceeds.False,VerifyDownloadSucceeds.True,Expected.False,"Download failed.")>]
    [<TestCase(DownloadIsRequired.True,DownloadSucceeds.True,VerifyDownloadSucceeds.False,Expected.False,"Failed to verify download.")>]
        
    [<TestCase(DownloadIsRequired.False,DownloadSucceeds.True,VerifyDownloadSucceeds.True,Expected.True,"N/A")>]
    [<TestCase(DownloadIsRequired.False,DownloadSucceeds.False,VerifyDownloadSucceeds.True,Expected.True,"N/A")>]
    [<TestCase(DownloadIsRequired.False,DownloadSucceeds.True,VerifyDownloadSucceeds.False,Expected.True,"N/A")>]
    
    let downloadIfDifferentTests (downloadIsRequired,downloadSucceeds,verifyDownloadSucceeds,expectedSuccess:bool,expectedErrorMessage) =

        let sourceWebFile = {Url=toUriUnsafe "http://some/test.txt";Checksum=Some "SomeCheckSum";Size=Some 0L;FileName="test.txt"}
        let destinationFile = {DestinationFile=(FileSystem.pathUnSafe @"c:\temp\test.txt")}
        let ignoreVerificationErrors = true
        let testDownload =
            WebFile2.DownloadWebFile (sourceWebFile,destinationFile)

        let downloadFileStub force (sourceUri:System.Uri,destinationFile:FileSystem.Path) =
            Assert.AreEqual(true,downloadIsRequired,"downloadFile(...) should not have been called.")
            match downloadSucceeds with
            |true ->
                Result.Ok destinationFile
            |false ->
                Result.Error (new System.Exception("Download failed."))

        let downloadIsRequiredStub (checkSum:string option) (size:System.Int64 option) (destinationFile:FileSystem.Path) =
            downloadIsRequired

        let verifyDownloadStub (ignoreVerificationErrors:bool) (download:WebFile2) =
            Assert.AreEqual(true,downloadIsRequired,"verifyDownload(...) should not have been called.")
            match verifyDownloadSucceeds with
            |true ->                
                Result.Ok (WebFile2.DownloadedWebFile destinationFile)
            |false ->
                Result.Error (new System.Exception("Failed to verify download."))
                
        let actual = downloadIfDifferent' testLogger downloadIsRequiredStub downloadFileStub verifyDownloadStub ignoreVerificationErrors testDownload
        match(actual)with
        |Result.Ok v ->
            Assert.IsTrue(expectedSuccess)
            match v with
            |WebFile2.SourceWebFile s ->
                Assert.Fail(sprintf "%A" v)
            |WebFile2.DownloadWebFile (s,d) ->
                Assert.Fail(sprintf "%A" v)            
            |WebFile2.DownloadedWebFile d ->
                Assert.AreEqual(destinationFile,d)
        |Result.Error ex ->
            Assert.IsFalse(expectedSuccess)
            Assert.IsTrue(ex.Message.Contains(expectedErrorMessage))
        