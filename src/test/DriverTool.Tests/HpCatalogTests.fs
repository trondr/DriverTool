namespace DriverTool.Tests

open DriverTool
open NUnit.Framework
open System

[<TestFixture>]
module HpCatalogTests =
    open Init
    open DriverTool
    open DriverTool.Library.F
    open DriverTool.Library

    let logger = Common.Logging.Simple.ConsoleOutLogger("LenovoUpdateTests",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")
    
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let downloadDriverPackCatalogTest () = 
        let actual = 
            result
                {      
                    use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
                    let! cacheFolderPath = cacheFolder.FolderPath
                    
                    let! xmlPath = HpCatalog.downloadDriverPackCatalog cacheFolderPath
                    return xmlPath
                }
        match actual with
        |Ok p -> 
            printfn "%s" (FileSystem.pathValue p)
            Assert.IsTrue(true)
        |Error e -> Assert.Fail(e.Message)
    
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getSoftPaqsTest () =
        match(result
                {
                    let! destinationFolderPath = FileSystem.path (System.IO.Path.GetTempPath())
                    let assembly = typeof<ThisTestAssembly>.Assembly
                    let! extractedFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase ("HPClientDriverPackCatalog.xml",destinationFolderPath,"HPClientDriverPackCatalog.xml",assembly)
                    let! actual = HpCatalog.getSoftPaqs extractedFilePath
                    Assert.IsTrue(actual.Length > 0, "Did not load any SoftPaq's")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].Category),"Category was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].CvaFileUrl),"CvaFileUrl was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].CvaTitle),"CvaTitle was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].DateReleased),"DateReleased was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].Id),"Id was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].MD5),"MD5 was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].Name),"Name was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].ReleaseNotesUrl),"ReleaseNotesUrl was empty")
                    Assert.IsFalse(actual.[0].Size = 0L,"Size was 0L")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].Url),"Url was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].Version),"Version was empty")
                    return actual
                }) with
        |Ok _ -> Assert.IsTrue(true)
        |Error e -> Assert.Fail(e.Message)
       
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let getProductOSDriverPacksTest () =  
         match(result
                {
                    let! destinationFolderPath = FileSystem.path (System.IO.Path.GetTempPath())
                    let assembly = typeof<ThisTestAssembly>.Assembly
                    let! extractedFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase ("HPClientDriverPackCatalog.xml",destinationFolderPath,"HPClientDriverPackCatalog.xml",assembly)
                    let! actual = HpCatalog.getProductOSDriverPacks extractedFilePath
                    Assert.IsTrue(actual.Length > 0, "Did not load any SoftPaq's")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].OSName),"OSName was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].ProductType),"ProductType was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].SoftPaqId),"SoftPaqId was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].SystemId),"SystemId was empty")
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.[0].SystemName),"SystemName was empty")
                    return actual
                }) with
         |Ok _ -> Assert.IsTrue(true)
         |Error e -> Assert.Fail(e.Message)
        
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase("WIN10X64","83B3")>]
    let getSccmDriverPackageInfoBase (operatingSystemCodeString:string,modelCodeString:string) =
        match(result
                {
                    let! operatingSystemCode = (OperatingSystemCode.create operatingSystemCodeString false)
                    let! modelCode = (ModelCode.create modelCodeString false)
                    let! destinationFolderPath = FileSystem.path (System.IO.Path.GetTempPath())
                    let assembly = typeof<ThisTestAssembly>.Assembly
                    let! extractedFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase ("HPClientDriverPackCatalog.xml",destinationFolderPath,"HPClientDriverPackCatalog.xml",assembly)
                    let! actual = HpCatalog.getSccmDriverPackageInfoBase (extractedFilePath,modelCode,operatingSystemCode)
                    Assert.IsFalse(String.IsNullOrWhiteSpace( actual.InstallerFile.FileName), "InstallerFileName is empty")
                    
                    return actual
                }) with
         |Ok _ -> Assert.IsTrue(true)
         |Error e -> Assert.Fail(e.Message)
        
    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let downloadSmsSdpCatalogTest () =
        let res = result{
            
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath

            let! actual = HpCatalog.downloadSmsSdpCatalog cacheFolderPath
            Assert.IsTrue(FileOperations.directoryExists actual)
            return actual
        }
        match res with
        |Ok s -> Assert.IsTrue(true)
        |Error ex -> Assert.Fail(ex.Message)
