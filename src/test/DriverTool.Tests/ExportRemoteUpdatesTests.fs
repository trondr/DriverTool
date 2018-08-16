
namespace DriverTool.Tests
open NUnit.Framework
open DriverTool
open DriverTool.ExportRemoteUpdates
open CommandProviders

[<TestFixture>]
module ExportRemoteUpdatesTests =
    open DriverTool

    [<Test>]
    let exportRemoteUpdatesTest() =
        let readFromLocalMachine = false
        let modelCodeResult = ModelCode.create "20FA" readFromLocalMachine
        let operatingSystemCodeResult = OperatingSystemCode.create "Win10" readFromLocalMachine
        let csvPathResult = Path.create "c:\\temp\\test.csv"

        let parametersResult = validateExportRemoteUdateInfoParameters (modelCodeResult, operatingSystemCodeResult, csvPathResult)
        match parametersResult with
        |Ok parameters -> 
            let (modelCode, operatingSystemCode, csvFilePath) = parameters
            let result = exportRemoteUpdates modelCode operatingSystemCode csvFilePath true
            match result with
            |Ok p -> Assert.AreEqual("C:\\Temp", p.Value)
            |Error ex -> Assert.Fail(ex.Message)
        |Error ex -> Assert.Fail(ex.Message)

    [<Test>]
    [<TestCase("","WIN1","",false,"Failed to validate one or more input parameters.
The model code '' is not valid. ModelCode cannot be null or empty.
The operating system code 'WIN1' is not valid. Valid values are: Win7, Win8, Win10.
The path '' is not valid. Path cannot be empty.")>]
    [<TestCase("LX123","WIN10","c:\\temp\\test.csv",true,"")>]
    [<TestCase("LX123","WIN10","c:\temp\test.csv",false,"Failed to validate one or more input parameters.
The path 'c:	emp	est.csv' is not valid.")>]
    let validateExportRemoteUdateInfoParametersTest modelCodeString operatingSystemString csvFilePathString (isOk:bool) (expectedErrorMessage:string) =
        let modelCodeResult = ModelCode.create modelCodeString false
        let operatingSystemCodeResult = OperatingSystemCode.create operatingSystemString false
        let csvFilePathResult = Path.create csvFilePathString
        let actual = validateExportRemoteUdateInfoParameters (modelCodeResult, operatingSystemCodeResult, csvFilePathResult)
        match actual with
        |Ok p -> Assert.IsTrue(isOk,"")
        |Error ex -> Assert.AreEqual(ex.Message,expectedErrorMessage,"Error message was not expected")

