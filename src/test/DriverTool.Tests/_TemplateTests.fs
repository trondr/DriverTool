namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module _TemplateTests=
    open DriverTool.Library
    
    [<SetUp>]    
    let setup () =
        DriverTool.Library.Logging.configureConsoleLogging()

    //[<Test>]
    [<Category(TestCategory.UnitTests)>]
    let someTest () =
        match(result{
            let! actual = Result.Error (toException "Not Implemented" None)
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(toExceptionMessages ex)

