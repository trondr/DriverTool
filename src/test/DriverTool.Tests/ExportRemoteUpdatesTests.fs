
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
        let modelCodeResult = ModelCode.create "LP1234" readFromLocalMachine
        let operatingSystemCodeResult = OperatingSystemCode.create "Win10" readFromLocalMachine
        let csvPathResult = Path.create "c:\\temp\\test.csv"

        let parametersResult = validateParameters (modelCodeResult, operatingSystemCodeResult, csvPathResult)
        match parametersResult with
        |Ok parameters -> 
            let (modelCode, operatingSystemCode, csvFilePath) = parameters
            let result = exportRemoteUpdates modelCode operatingSystemCode csvPathResult
            match result with
            |Ok p -> Assert.AreEqual("C:\\Temp", p.Value)
            |Error ex -> Assert.Fail(ex.Message)
        |Error ex -> Assert.Fail(ex.Message)
