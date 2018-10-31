
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
        let testResult = 
            result  {
                let! modelCode = ModelCode.create "20FA" readFromLocalMachine
                let! operatingSystemCode = OperatingSystemCode.create "Win10" readFromLocalMachine
                let! csvFilePath = Path.create "c:\\temp\\test.csv"
                let exportResult = exportRemoteUpdates modelCode operatingSystemCode csvFilePath true
                return! exportResult
            }
        match testResult with
        |Ok p -> Assert.AreEqual("c:\\temp\\test.csv", p.Value)
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

