namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module EnvironmentTests =
    open DriverTool
    open DriverTool.Library.Environment
    
    [<Test>]
    [<TestCase(@"%Public%\Logs",@"%PUBLIC%\Logs")>]
    let unExpandEnironmentVariablesTests (unexpandedText:string, expectedUnExpandedText:string) =
        let expandedText = 
            expandEnvironmentVariables unexpandedText
        let actual = unExpandEnironmentVariables expandedText
        Assert.AreEqual(expectedUnExpandedText, actual)
       