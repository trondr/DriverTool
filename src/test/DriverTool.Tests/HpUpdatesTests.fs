namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module HpUpdatesTests =
    open DriverTool
    open System
    type ThisAssembly = { Empty:string;}
    let logger = Common.Logging.Simple.ConsoleOutLogger("HpUpdatesTests",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")
    open DriverTool.Library.F
    open DriverTool.Library
    
    [<Test>]
    [<TestCase("WIN10X64","83B3")>]
    [<Category(TestCategory.IntegrationTests)>]
    let downloadSccmDriverPackageTest (operatingSystemCodeString:string,modelCodeString:string) =
        match(result
                {

                    let! operatingSystemCode = (OperatingSystemCode.create operatingSystemCodeString false)
                    let! modelCode = (ModelCode.create modelCodeString false)
                    use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
                    let! cacheFolderPath = cacheFolder.FolderPath
                    let! sccmDriverPackageInfo = HpUpdates.getSccmDriverPackageInfo (modelCode,operatingSystemCode,cacheFolderPath)                
                    let! actual = HpUpdates.downloadSccmPackage (cacheFolderPath,sccmDriverPackageInfo)
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.InstallerPath), "InstallerPath is empty")
                    
                    return actual
                }) with
         |Result.Ok _ -> Assert.IsTrue(true)
         |Result.Error e -> Assert.Fail(String.Format("{0}", e.Message))

    [<Test>]
    [<TestCase("WIN10X64","83B3")>]
    [<Category(TestCategory.IntegrationTests)>]
    let extractSccmDriverPackageTest (operatingSystemCodeString:string,modelCodeString:string) =
        match(result
                {

                    let! operatingSystemCode = (OperatingSystemCode.create operatingSystemCodeString false)
                    let! modelCode = (ModelCode.create modelCodeString false)
                    use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
                    let! cacheFolderPath = cacheFolder.FolderPath
                    let! sccmDriverPackageInfo = HpUpdates.getSccmDriverPackageInfo (modelCode,operatingSystemCode, cacheFolderPath)                
                    let! downloadedSccmPackageInfo = HpUpdates.downloadSccmPackage (cacheFolderPath,sccmDriverPackageInfo)
                    let! destinationFolderPath = PathOperations.combine2Paths (PathOperations.getTempPath,"005 Sccm Package Test")
                    Assert.IsTrue((FileSystem.pathValue destinationFolderPath).EndsWith("\\005 Sccm Package Test"))
                    let deletedDestinationDirectory = DirectoryOperations.deleteDirectory true, FileSystem.pathValue  destinationFolderPath
                    let! existingDestinationPath = DirectoryOperations.ensureDirectoryExistsAndIsEmpty (destinationFolderPath,true)
                    let! (actual,_) = HpUpdates.extractSccmPackage (downloadedSccmPackageInfo, existingDestinationPath)
                    Assert.IsFalse(String.IsNullOrWhiteSpace(FileSystem.pathValue  actual), "Destination path is empty")
                    
                    return actual
                }) with
         |Result.Ok _ -> Assert.IsTrue(true)
         |Result.Error e -> Assert.Fail(String.Format("{0}", e.Message))

    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    [<TestCase(@"C:\Temp\DriverToolCache\HpCatalogForSms.latest\V2\00004850-0000-0000-5350-000000065111.sdp")>]
    [<TestCase(@"C:\Temp\DriverToolCache\HpCatalogForSms.latest\V2\00004850-0000-0000-5350-000000094780.sdp")>]
    [<TestCase(@"C:\Temp\DriverToolCache\HpCatalogForSms.latest\V2\00004850-0000-0000-5350-000000081886.sdp")>]    
    let toPackageInfoTests (sdpXmlFile) =
        match(result
                {
                    let! sdp = sdpeval.fsharp.Sdp.loadSdpFromFile (sdpXmlFile)
                    let actual = HpUpdates.toPackageInfos sdp                
                    Assert.IsTrue(actual.Length > 0)
                    return sdp
                })with
        |Result.Ok _ -> Assert.IsTrue(true)
        |Result.Error e -> Assert.Fail(String.Format("{0}", e.Message))
        
    open DriverTool.Library.UpdatesContext    

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let getLocalUpdatesTests () =
        match(result
                {
                    let! currentModelCode = ModelCode.create "" true
                    let! currentOperatingSystem = OperatingSystemCode.create "" true
                    let! logDirectory = FileSystem.path "%public%\Logs"
                    let! patterns = (RegExp.toRegexPatterns [||] true)
                    let updatesRetrievalContext = toUpdatesRetrievalContext currentModelCode currentOperatingSystem true logDirectory patterns

                    use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
                    let! cacheFolderPath = cacheFolder.FolderPath

                    let! actual = HpUpdates.getLocalUpdates logger cacheFolderPath updatesRetrievalContext
                    return actual
                }) with
        |Result.Ok _ -> Assert.IsTrue(true)
        |Result.Error e -> Assert.Fail(String.Format("{0}", e.Message))

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let getRemoteUpdatesTests () =
        match(result
                {
                    let! currentModelCode = ModelCode.create "" true
                    let! currentOperatingSystem = OperatingSystemCode.create "" true  
                    let! logDirectory = FileSystem.path "%public%\Logs"
                    let! patterns = (RegExp.toRegexPatterns [||] true)
                    let updatesRetrievalContext = toUpdatesRetrievalContext currentModelCode currentOperatingSystem true logDirectory patterns

                    use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
                    let! cacheFolderPath = cacheFolder.FolderPath

                    let! actual = HpUpdates.getRemoteUpdates logger cacheFolderPath updatesRetrievalContext
                    return actual
                }) with
        |Result.Ok _ -> Assert.IsTrue(true)
        |Result.Error e -> Assert.Fail(String.Format("{0}", e.Message))

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("HP_sp92489.html","Driver-Keyboard,Mouse and Input Devices", true)>]
    [<TestCase("HP_sp95015.html","Driver-Audio", true)>]
    [<TestCase("HP_sp95xxx.html","Default",true)>]
    [<TestCase("HP_sp99341.html","Default",true)>]
    let getCategoryFromReadmeHtmlTest (htmlFileName, expectedCategory,isSuccess:bool) =
        match(result{
            let! tempDestinationFolderPath = FileSystem.path (PathOperations.getTempPath)            
            let! readmeHtmlPath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (htmlFileName,tempDestinationFolderPath,htmlFileName,typeof<ThisAssembly>.Assembly)            
            let actual = HpUpdates.getCategoryFromReadmeHtml readmeHtmlPath "Default"
            return actual
        })with
        |Result.Ok a -> 
            Assert.IsTrue(isSuccess,sprintf "Expected failed, but suceeded. Actual value: %A" a)
            Assert.AreEqual(expectedCategory,a)

        |Result.Error e -> 
            Assert.IsFalse(isSuccess,sprintf "Expected success, but failed due to: %A" e)
            Assert.AreEqual("Invalid HTML",e.InnerException.Message,"Inner exception not as expected.")
            printf "%A" e
        