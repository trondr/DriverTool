
namespace DriverTool.Tests
open System
open NUnit.Framework
open DriverTool
open DriverTool.ExportRemoteUpdates
open FileOperations

[<TestFixture>]
module ExportRemoteUpdatesTests =
    [<Test>]
    let exportRemoteUpdatesTest() =
        let readFromLocalMachine = false
        let model = Model.ModelCodeResult (ModelCode.create "LP1234" readFromLocalMachine)
        let operatingSystem = OperatingSystem.OperatingSystemCodeResult (OperatingSystemCode.create "Win10" readFromLocalMachine)
        let result = exportRemoteUpdates model operatingSystem
        match result with
        |Ok p -> Assert.AreEqual("C:\\Temp", p.Value)
        |Error ex -> Assert.Fail(ex.Message)
