namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module HpUpdatesTests =
    open DriverTool
    open System
    open Init
    
    [<Test>]
    [<TestCase("WIN10X64","83B3")>]
    [<Category(TestCategory.IntegrationTests)>]
    let downloadSccmDriverPackageTest (operatingSystemCodeString:string,modelCodeString:string) =
        match(result
                {

                    let! operatingSystemCode = (OperatingSystemCode.create operatingSystemCodeString false)
                    let! modelCode = (ModelCode.create modelCodeString false)
                    let! sccmDriverPackageInfo = HpUpdates.getSccmDriverPackageInfo (modelCode,operatingSystemCode)                
                    let cacheDirectory =   Configuration.getDownloadCacheDirectoryPath
                                        
                    let! actual = HpUpdates.downloadSccmPackage (cacheDirectory,sccmDriverPackageInfo)
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.InstallerPath), "InstallerPath is empty")
                    
                    return actual
                }) with
         |Ok _ -> Assert.IsTrue(true)
         |Error e -> Assert.Fail(String.Format("{0}", e.Message))

    [<Test>]
    [<TestCase("WIN10X64","83B3")>]
    [<Category(TestCategory.IntegrationTests)>]
    let extractSccmDriverPackageTest (operatingSystemCodeString:string,modelCodeString:string) =
        match(result
                {

                    let! operatingSystemCode = (OperatingSystemCode.create operatingSystemCodeString false)
                    let! modelCode = (ModelCode.create modelCodeString false)
                    let! sccmDriverPackageInfo = HpUpdates.getSccmDriverPackageInfo (modelCode,operatingSystemCode)                
                    let cacheDirectory =   Configuration.getDownloadCacheDirectoryPath             
                    let! downloadedSccmPackageInfo = HpUpdates.downloadSccmPackage (cacheDirectory,sccmDriverPackageInfo)
                    let! destinationFolderPath = PathOperations.combine2Paths (PathOperations.getTempPath,"005 Sccm Package Test")
                    Assert.IsTrue((FileSystem.pathValue destinationFolderPath).EndsWith("\\005 Sccm Package Test"))
                    let deletedDestinationDirectory = DirectoryOperations.deleteDirectory true, FileSystem.pathValue  destinationFolderPath
                    let! existingDestinationPath = DirectoryOperations.ensureDirectoryExistsAndIsEmpty (destinationFolderPath,true)
                    let! actual = HpUpdates.extractSccmPackage (downloadedSccmPackageInfo, existingDestinationPath)
                    Assert.IsFalse(String.IsNullOrWhiteSpace(FileSystem.pathValue  actual), "Destination path is empty")
                    
                    return actual
                }) with
         |Ok _ -> Assert.IsTrue(true)
         |Error e -> Assert.Fail(String.Format("{0}", e.Message))

    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    [<TestCase(@"C:\Temp\DriverToolCache\HpCatalogForSms.latest\V2\00004850-0000-0000-5350-000000065111.sdp")>]
    [<TestCase(@"C:\Temp\DriverToolCache\HpCatalogForSms.latest\V2\00004850-0000-0000-5350-000000094780.sdp")>]
    [<TestCase(@"C:\Temp\DriverToolCache\HpCatalogForSms.latest\V2\00004850-0000-0000-5350-000000081886.sdp")>]    
    let toPackageInfoTests (sdpXmlFile) =
        match(result
            {
                let! sdp = SdpCatalog.loadSdpFromFile (FileSystem.pathUnSafe sdpXmlFile)
                let actual = HpUpdates.toPackageInfos "%public%\Logs" sdp                
                Assert.IsTrue(actual.Length > 0)
                return sdp
            })with
        |Ok _ -> Assert.IsTrue(true)
        |Error e -> Assert.Fail(String.Format("{0}", e.Message))
        
        

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let getLocalUpdatesTests () =
        match(result
                {
                    let! currentModelCode = ModelCode.create "" true
                    let! currentOperatingSystem = OperatingSystemCode.create "" true            
                    let! actual = HpUpdates.getLocalUpdates (currentModelCode, currentOperatingSystem,true, @"%public%\Logs")
                    return actual
                }) with
        |Ok _ -> Assert.IsTrue(true)
        |Error e -> Assert.Fail(String.Format("{0}", e.Message))

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let getRemoteUpdatesTests () =
        match(result
                {
                    let! currentModelCode = ModelCode.create "" true
                    let! currentOperatingSystem = OperatingSystemCode.create "" true            
                    let! actual = HpUpdates.getRemoteUpdates (currentModelCode, currentOperatingSystem,true, @"%public%\Logs")
                    return actual
                }) with
        |Ok _ -> Assert.IsTrue(true)
        |Error e -> Assert.Fail(String.Format("{0}", e.Message))
        