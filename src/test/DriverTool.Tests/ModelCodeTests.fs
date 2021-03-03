namespace DriverTool.Tests
open NUnit.Framework
open DriverTool.Library
open System

[<TestFixture>]
module ModelCodeTests  =
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ModuleCodeTest() =
        let modelCodeResult = ModelCode.create "EQ10" false
        match modelCodeResult with
        |Ok modelCode -> Assert.IsFalse((String.IsNullOrWhiteSpace(modelCode.Value)), sprintf "Model code: %s" modelCode.Value)
        |Error ex -> Assert.Fail(sprintf "Did not expect to fail. Error: %s" ex.Message)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ModuleCodeTest_Empty_ModuleCode_UseLocalIsFalse() =
        let modelCodeResult = ModelCode.create "" false
        match modelCodeResult with
        |Ok modelCode -> Assert.IsFalse((String.IsNullOrWhiteSpace(modelCode.Value)), sprintf "Model code: %s" modelCode.Value)
        |Error ex -> Assert.IsTrue(ex.Message.EndsWith("ModelCode cannot be null or empty."))

    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    let ModuleCodeTest_Empty_ModuleCode_UseLocalIsTrue() =
        let modelCodeResult = ModelCode.create "" true
        match modelCodeResult with
        |Ok modelCode -> Assert.IsFalse((String.IsNullOrWhiteSpace(modelCode.Value)), sprintf "Model code: %s" modelCode.Value)
        |Error ex -> Assert.Fail(ex.Message)



