namespace DriverTool.Tests

open NUnit.Framework
open DriverTool.InstallDriverPackage
open DriverTool.InstallXml
open DriverTool.Library.F

[<TestFixture>]
module InstallDriverPackageTests =
    open DriverTool
    
    [<Test>]
    [<TestCase("20EQXXXX","WIN10X64","20EQ","WIN10X64","")>]
    [<TestCase("20EQXXXX","WIN10X64","20EQX","WIN10X64","")>]
    [<TestCase("20EQXXXX","WIN10X64","20EQY","WIN10X64","Computer model '20EQXXXX' is not supported by this driver package. Supported model: '20EQY'.")>]
    [<TestCase("20EQXXXX","WIN10X64","20EQX","WIN7X64","Operating system 'WIN10X64' is not supported by this driver package. Supported operating system: 'WIN7X64'.")>]
    [<TestCase("20EQXXXX","WIN10X64","20EQY","WIN7X64","Computer model '20EQXXXX' is not supported by this driver package. Supported model: '20EQY'. Operating system 'WIN10X64' is not supported by this driver package. Supported operating system: 'WIN7X64'.")>]
    [<Category(TestCategory.UnitTests)>]
    let assertIsSupportedTest (currentModel,currentOs,packagedModel,packagesOs,expectedErrorMessage) =
        let systemInfo = {Model=currentModel;OperatingSystem=currentOs}
        let installConfigurationData = {            
            ComputerModel=packagedModel;
            OsShortName=packagesOs;
            LogDirectory="";
            LogFileName="";
            PackageName="";
            PackageVersion="";
            PackageRevision="000";
            Publisher="";
            ComputerVendor="";            
            ComputerSystemFamiliy="";            
            }
        let actual = assertIsSupported installConfigurationData systemInfo
        match actual with
        |Ok _ -> Assert.AreEqual(expectedErrorMessage, "")
        |Error ex -> Assert.AreEqual(expectedErrorMessage, ex.Message)        
        ()
    
    type ExitCodeTestData = {
        ExitCodes: array<int>
        ExpectedAdjustedExitCode:int
    }

    let testData = 
        [|
            {ExitCodes=[|0;0;0;0;0;0;0;0;0;0;|];ExpectedAdjustedExitCode=0}            
            {ExitCodes=[|0;0;0;0;-2146232576;0;0;0;0;0;|];ExpectedAdjustedExitCode=3010}
            {ExitCodes=[|0;0;0;0;3010;0;0;0;0;0;|];ExpectedAdjustedExitCode=3010}
        |]

    [<Test>]
    [<TestCaseSource("testData")>]
    [<Category(TestCategory.UnitTests)>]
    let getAdjustedExitCodeTests (exitCodeTestData:ExitCodeTestData) =
        let actual = getAdjustedExitCode exitCodeTestData.ExitCodes
        Assert.AreEqual(exitCodeTestData.ExpectedAdjustedExitCode,actual)
        ()

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let copyDriversTest () =
        let result = 
            result{
                let! driverPackagePath = FileSystem.path @"C:\Temp\Drivers\SomeModel\2018-12-29"
                let! destinationDriversFolderPath = FileSystem.path @"C:\Windows\Drivers\_tst_"
                let! copyResult = copyDrivers (driverPackagePath, destinationDriversFolderPath)
                return copyResult
            }
        match result with
        |Error ex -> Assert.Fail(ex.ToString())
        |Ok _ -> Assert.IsTrue(true)
        ()
        
        