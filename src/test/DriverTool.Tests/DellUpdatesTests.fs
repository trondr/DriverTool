namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module DellUpdatesTests =
    open DriverTool
    
    [<Test>]
    [<TestCase("FOLDER03578551M/1/Audio_Driver_D00J4_WN32_6.0.1.6102_A03.EXE","FOLDER03578551M/1","Audio_Driver_D00J4_WN32_6.0.1.6102_A03.EXE")>]
    [<TestCase("FOLDER01766254M/1/9P33_Chipset_Driver_NNGJM_WN_9.4.0.1026_A00.EXE","FOLDER01766254M/1","9P33_Chipset_Driver_NNGJM_WN_9.4.0.1026_A00.EXE")>]
    let pathToDirectoryAndFileTests (path:string,expectedDirectory,expectedFileName) =
       let (actualDirectory,actualFileName) = DriverTool.DellUpdates.pathToDirectoryAndFile path
       Assert.AreEqual(expectedDirectory,actualDirectory,"Directory not expected")
       Assert.AreEqual(expectedFileName,actualFileName,"FileName not expected")
    
    [<Test>]
    [<TestCase("07A7","WIN10X64")>]
    let getUpdates2Test (modelCodeString,operatingSystemCodeString) =
        match(result{
            let! modelCode = ModelCode.create modelCodeString false
            let! operatingSystemCode = OperatingSystemCode.create operatingSystemCodeString false
            let! actual = DriverTool.DellUpdates.getRemoteUpdates (modelCode, operatingSystemCode,true)
            Assert.IsTrue(actual.Length > 0,"PackageInfo array is empty")
            System.Console.WriteLine("Number of software components: " + actual.Length.ToString())
            actual
            |>Seq.sortBy(fun p -> p.Name)
            |>Seq.map (fun p -> System.Console.WriteLine((DriverTool.Logging.valueToString p)))
            |>Seq.toArray
            |>ignore
            return actual
        }) with
        |Ok _->Assert.IsTrue(true)
        |Error ex ->Assert.Fail(ex.Message)

    open DriverTool.PackageXml
    open DriverTool

    let packageInfoTestData () =        
        let packageInfo = {Name = "";Title = "";Version = "";BaseUrl = "";InstallerName = "";InstallerCrc = "";InstallerSize = 0L;ExtractCommandLine = "";InstallCommandLine = "";Category = "";ReadmeName = "";ReadmeCrc = "";ReadmeSize=0L;ReleaseDate= "";PackageXmlName="";}
        
        seq{
            yield {packageInfo with Name="Name1";Version="1.0.20.0"}
            yield {packageInfo with Name="Name1";Version="2.0.20.0"}
            yield {packageInfo with Name="Name1";Version="3.0.20.0"}
            yield {packageInfo with Name="Name2";Version="1.0.20.0"}
            yield {packageInfo with Name="Name2";Version="2.0.30.0"}
            yield {packageInfo with Name="Name2";Version="002.01.20.0"}
        }
    
    [<Test>]    
    let getLatestPackageInfoVersionTest () =
        let packageInfos = packageInfoTestData ()
        let actual = DriverTool.DellUpdates.getLatestPackageInfoVersion (packageInfos) |> Seq.toArray
        Assert.AreEqual(2,actual.Length,"Selected package info should be 1")
        let actualPackageInfo1 = actual.[0]
        Assert.AreEqual("Name1",actualPackageInfo1.Name,"Name")
        Assert.AreEqual("3.0.20.0",actualPackageInfo1.Version,"Version")
        let actualPackageInfo2 = actual.[1]
        Assert.AreEqual("Name2",actualPackageInfo2.Name,"Name")
        Assert.AreEqual("002.01.20.0",actualPackageInfo2.Version,"Version")
        ()