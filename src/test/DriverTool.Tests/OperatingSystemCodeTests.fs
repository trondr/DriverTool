namespace DriverTool.Tests
open NUnit.Framework
open DriverTool
open System

[<TestFixture>]
module OperatingSystemCodeTests  =
    [<Test>]
    let OperatingSystemCodeTest() =
        let operatingSystemCodeResult = OperatingSystemCode.create "WIN10X64" false
        match operatingSystemCodeResult with
        |Ok operatingSystemCode -> Assert.IsFalse((String.IsNullOrWhiteSpace(operatingSystemCode.Value)), sprintf "Operating system code: %s" operatingSystemCode.Value)
        |Error ex -> Assert.Fail(sprintf "Did not expect to fail. Error: %s" ex.Message)
    
    [<Test>]
    let OperatingSystemCodeTest_Invalid_OperatingSystemCode() =
        let operatingSystemCodeResult = OperatingSystemCode.create "Win1" false
        match operatingSystemCodeResult with
        |Ok os -> Assert.Fail(sprintf "Did expect to fail but did not fail.")
        |Error ex -> Assert.IsTrue(ex.Message.StartsWith("The operating system code 'Win1' is not valid."))

    [<Test>]
    let OperatingSystemCode_Empty_OperatingSystemCode_UseLocalIsFalse() =
        let operatingSystemCodeResult = OperatingSystemCode.create "" false
        match operatingSystemCodeResult with
        |Ok operatingSystemCode -> Assert.IsFalse((String.IsNullOrWhiteSpace(operatingSystemCode.Value)), sprintf "Operating system code: %s" operatingSystemCode.Value)
        |Error ex -> Assert.IsTrue(ex.Message.EndsWith("OperatingSystemCode cannot be null or empty."))

    [<Test>]
    let OperatingSystemCode_Empty_OperatingSystemCode_UseLocalIsTrue() =
        let operatingSystemCodeResult = OperatingSystemCode.create "" true
        match operatingSystemCodeResult with
        |Ok operatingSystemCode -> Assert.IsFalse((String.IsNullOrWhiteSpace(operatingSystemCode.Value)), sprintf "Operating system code: %s" operatingSystemCode.Value)
        |Error ex -> Assert.Fail(ex.Message)

    [<Test>]
    let OperatingSytemCode_DifferentCase_Win10X64 () =
        let operatingSystemCodeResult = OperatingSystemCode.create "Win10X64" true
        match operatingSystemCodeResult with
        |Ok os -> Assert.Fail(os.Value)
        |Error ex -> Assert.IsTrue(true,"Expected to fail with: " + ex.Message)        
        

