namespace DriverTool.Tests
open NUnit.Framework
open DriverTool
open System

[<TestFixture>]
module OperatingSystemCodeTests  =
    [<Test>]
    let OperatingSystemCodeTest() =
        let operatingSystemCodeResult = OperatingSystemCode.create "Win10" false
        match operatingSystemCodeResult with
        |Ok operatingSystemCode -> Assert.IsFalse((String.IsNullOrWhiteSpace(operatingSystemCode.Value)), sprintf "Operating system code: %s" operatingSystemCode.Value)
        |Error ex -> Assert.Fail(sprintf "Did not expect to fail. Error: %s" ex.Message)

    [<Test>]
    let OperatingSystemCode_Empty_OperatingSystemCode_UseLocalIsFalse() =
        let operatingSystemCodeResult = ModelCode.create "" false
        match operatingSystemCodeResult with
        |Ok operatingSystemCode -> Assert.IsFalse((String.IsNullOrWhiteSpace(operatingSystemCode.Value)), sprintf "Operating system code: %s" operatingSystemCode.Value)
        |Error ex -> Assert.IsTrue(ex.Message.StartsWith("Value cannot be null."))

    [<Test>]
    let OperatingSystemCode_Empty_OperatingSystemCode_UseLocalIsTrue() =
        let operatingSystemCodeResult = OperatingSystemCode.create "" true
        match operatingSystemCodeResult with
        |Ok operatingSystemCode -> Assert.IsFalse((String.IsNullOrWhiteSpace(operatingSystemCode.Value)), sprintf "Operating system code: %s" operatingSystemCode.Value)
        |Error ex -> Assert.Fail(ex.Message)



