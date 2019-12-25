namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module DellUpdatesTests =
    open DriverTool.Library.UpdatesContext    
    let logger = Common.Logging.Simple.ConsoleOutLogger("DellUpdatesTests",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")
    open DriverTool.Library.F
    open DriverTool.Library.Logging
    open DriverTool.Library
    
    [<Test>]
    [<TestCase("FOLDER03578551M/1/Audio_Driver_D00J4_WN32_6.0.1.6102_A03.EXE","FOLDER03578551M/1","Audio_Driver_D00J4_WN32_6.0.1.6102_A03.EXE")>]
    [<TestCase("FOLDER01766254M/1/9P33_Chipset_Driver_NNGJM_WN_9.4.0.1026_A00.EXE","FOLDER01766254M/1","9P33_Chipset_Driver_NNGJM_WN_9.4.0.1026_A00.EXE")>]
    [<Category(TestCategory.UnitTests)>]
    let pathToDirectoryAndFileTests (path:string,expectedDirectory,expectedFileName) =
       let (actualDirectory,actualFileName) = DriverTool.DellUpdates.pathToDirectoryAndFile path
       Assert.AreEqual(expectedDirectory,actualDirectory,"Directory not expected")
       Assert.AreEqual(expectedFileName,actualFileName,"FileName not expected")
    
    [<Test>]
    [<TestCase("07A7","WIN10X64")>]
    [<Category(TestCategory.IntegrationTests)>]
    let getUpdates2Test (modelCodeString,operatingSystemCodeString) =
        match(result{
            let! modelCode = ModelCode.create modelCodeString false
            let! operatingSystemCode = OperatingSystemCode.create operatingSystemCodeString false
            let! logDirectory = FileSystem.path "%public%\Logs"
            let! patterns = (RegExp.toRegexPatterns [||] true)
            let updatesRetrievalContext = toUpdatesRetrievalContext modelCode operatingSystemCode true logDirectory patterns
            
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath
            
            let! actual = DriverTool.DellUpdates.getRemoteUpdates logger cacheFolderPath updatesRetrievalContext
            printfn "Packages: %A" actual
            Assert.IsTrue(actual.Length > 0,"PackageInfo array is empty")
            System.Console.WriteLine("Number of software components: " + actual.Length.ToString())
            actual
            |>Seq.sortBy(fun p -> p.Name)
            |>Seq.map (fun p -> System.Console.WriteLine((valueToString p)))
            |>Seq.toArray
            |>ignore
            return actual
        }) with
        |Ok _->Assert.IsTrue(true)
        |Result.Error ex ->Assert.Fail(ex.Message)

    open DriverTool.Library.PackageXml
    open DriverTool
    open System

    let packageInfoTestData () =        
        let packageInfo = 
            {
                Name = "";
                Title = "";
                Version = "";                
                Installer = 
                    {
                        Url = toOptionalUri "http://dummy" ""
                        Name = ""
                        Checksum = ""
                        Size = 0L
                        Type = Installer
                    }
                ExtractCommandLine = "";
                InstallCommandLine = "";
                Category = "";
                Readme =
                    {
                        Url = toOptionalUri "http://dummy" ""
                        Name = ""
                        Checksum = ""
                        Size = 0L
                        Type = Installer
                    }
                ReleaseDate= "";
                PackageXmlName="";
            }
        
        seq{
            yield {packageInfo with Name="Name1";Version="1.0.20.0"}
            yield {packageInfo with Name="Name1";Version="2.0.20.0"}
            yield {packageInfo with Name="Name1";Version="3.0.20.0"}
            yield {packageInfo with Name="Name2";Version="1.0.20.0"}
            yield {packageInfo with Name="Name2";Version="2.0.30.0"}
            yield {packageInfo with Name="Name2";Version="002.01.20.0"}
        }
    
    [<Test>]
    [<TestCase("07A0","WIN10X64")>]
    [<Category(TestCategory.IntegrationTests)>]
    let getSccmDriverPackageInfoTest(model, operatingSystem) =
        match(result{
            let! modelCode = ModelCode.create model false
            let! operatingSystemCode = OperatingSystemCode.create operatingSystem false
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath
            let! actual = DriverTool.DellUpdates.getSccmDriverPackageInfo (modelCode, operatingSystemCode, cacheFolderPath)
            return actual
        }) with
        |Ok _->Assert.IsTrue(true)
        |Result.Error ex ->Assert.Fail(ex.Message)