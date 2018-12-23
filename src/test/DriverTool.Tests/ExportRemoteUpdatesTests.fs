
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
                let! operatingSystemCode = OperatingSystemCode.create "WIN10X64" readFromLocalMachine
                let! csvFilePath = Path.create "c:\\temp\\test.csv"
                let exportResult = exportRemoteUpdates modelCode operatingSystemCode csvFilePath true
                return! exportResult
            }
        match testResult with
        |Ok p -> Assert.AreEqual("c:\\temp\\test.csv", p.Value)
        |Error ex -> Assert.Fail(ex.Message)
  
    [<Test>]
    [<TestCase("","WIN1","",false,"The model code '' is not valid. ModelCode cannot be null or empty.")>]
    [<TestCase("20QE","WIN1","",false,"The operating system code 'WIN1' is not valid. Valid values are: WIN2016X64|WIN2008X64|WIN2008R2X64|WIN2012X64|WIN2012R2X64|WIN10X64|VISTAX64|WIN7X64|WIN8X64|WIN81X64|WIN2016X64|WIN2008X86|WIN2008R2X64|WIN2012X64|WIN2012R2X64|WIN10X86|VISTAX86|WIN7X86|WIN8X86|WIN81X86.")>]
    [<TestCase("20QE","WIN10X64","",false,"The path '' is not valid. Path cannot be empty.")>]
    [<TestCase("LX123","WIN10X64","c:\\temp\\test.csv",true,"")>]
    [<TestCase("LX123","WIN10X64","c:\temp\test.csv",false,"The path 'c:\temp\test.csv' is not valid.")>]
    let validateExportRemoteUdateInfoParametersTest modelCodeString operatingSystemString csvFilePathString (isOk:bool) (expectedErrorMessage:string) =
        match(result
                {
                    let! modelCodeResult = ModelCode.create modelCodeString false
                    let! operatingSystemCodeResult = OperatingSystemCode.create operatingSystemString false
                    let! csvFilePathResult = Path.create csvFilePathString
                    return csvFilePathResult
                }) with
        |Ok p -> Assert.IsTrue(isOk,"")
        |Error ex -> Assert.AreEqual(expectedErrorMessage,ex.Message,"Error message was not expected")
