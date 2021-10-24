namespace DriverTool.PowerCLI.Library.Tests

module ExampleTests =
    open System
    open NUnit.Framework
    open DriverTool.PowerCLI.Library    
    
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let exampleTest1 () =
        Assert.IsTrue(true,"Example Test expected to succede.")
        ()