namespace DriverTool.Tests
open NUnit.Framework
open DriverTool

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module EnvironmentTests =
    open DriverTool
    
    [<Test>]
    [<TestCase(@"%Public%\Logs",@"%PUBLIC%\Logs")>]
    let unExpandEnironmentVariablesTests (unexpandedText:string, expectedUnExpandedText:string) =
        let expandedText = 
            Environment.expandEnvironmentVariables unexpandedText
        let actual = Environment.unExpandEnironmentVariables expandedText
        Assert.AreEqual(expectedUnExpandedText, actual)
       