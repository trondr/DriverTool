namespace DriverTool.Tests

open NUnit.Framework
open DriverTool.Library.F
open DriverTool
open DriverTool.Library

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module DellCommandUpdatesTests =
    open DriverTool
       
    let logger = Common.Logging.Simple.ConsoleOutLogger("LenovoUpdateTests",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")

    type TestAssebly = { Empty:string;}

    [<Test>]
    let getDownloadedFilesBaseTest () =
        let downloadedFiles =
            result{
                let! destinationFolderPath = FileSystem.path (System.IO.Path.GetTempPath())
                let! extractedActivityLogPath = EmbeddedResource.extractEmbeddedResourceByFileNameBase ("ActivityLog.xml",destinationFolderPath,"ActivityLog.xml",typeof<TestAssebly>.Assembly)
                let! downloadedFiles = DellCommandUpdate.getDownloadedFilesBase extractedActivityLogPath
                let! fileDeleted = FileOperations.deleteFile extractedActivityLogPath
                return downloadedFiles
            }
        match downloadedFiles with
        |Error ex -> Assert.Fail(ex.Message)
        |Ok files -> 
            Assert.IsTrue(files.Length > 0,"File count is 0")
            printfn "Files: %A" files


    [<Test>]
    [<TestCase("http://downloads.dell.com/FOLDER05171783M/1/ASMedia-USB-Extended-Host-Controller-Driver_JCDN0_WIN_1.16.54.1_A10.EXE","http://downloads.dell.com/FOLDER05171783M/1","ASMedia-USB-Extended-Host-Controller-Driver_JCDN0_WIN_1.16.54.1_A10.EXE")>]
    let fileUrlToBaseUrlAndFileNameTest (fileUrl:string,expectedBaseUrl:string,expectedFileName:string) =
        let (actualBaseurl,actualFileName) = DellCommandUpdate.fileUrlToBaseUrlAndFileName fileUrl
        Assert.AreEqual(expectedBaseUrl,actualBaseurl,"BaseUrl")
        Assert.AreEqual(expectedFileName,actualFileName,"FileName")

[<TestFixture>]
[<Category(TestCategory.IntegrationTests)>]
module DellCommandUpdatesIntegrationTests =
    open DriverTool.Library.UpdatesContext
    open DriverTool.Library.ManufacturerTypes

    let logger = Common.Logging.Simple.ConsoleOutLogger("LenovoUpdateTests",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")
    
    [<Test>]
    [<TestCase("07A7","WIN10X64")>]
    [<Category(TestCategory.IntegrationTests)>]
    let getLocalUpdatesBaseTest (modelCodeString,operatingSystemCodeString) =
        let localUpdates =
            result{
                let! manufacturer = getManufacturerForCurrentSystem()
                let! modelCode = ModelCode.create modelCodeString false
                let! operatingSystemCode = OperatingSystemCode.create operatingSystemCodeString false
                let! logDirectory = FileSystem.path @"c:\temp"
                let! patterns = (RegExp.toRegexPatterns [||] true)
                use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
                let! cacheFolderPath = cacheFolder.FolderPath
                let updatesRetrievalContext = toUpdatesRetrievalContext manufacturer modelCode operatingSystemCode true logDirectory cacheFolderPath false patterns
                
                let! remoteUpdates = DellUpdates.getRemoteUpdates logger cacheFolderPath updatesRetrievalContext
                let! localUpdates = DellCommandUpdate.getLocalUpdates (modelCode, operatingSystemCode,remoteUpdates)
                return localUpdates
            }
        match localUpdates with
        |Error ex -> Assert.Fail(ex.Message)
        |Ok updates -> 
            Assert.IsTrue(updates.Length > 0,"Update count is 0")
            printfn "Updates: %A" updates
