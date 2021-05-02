namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.ManualTests)>]
module SccmTests=
    open DriverTool.Library
    open DriverTool.Library.Sccm
    
    [<SetUp>]    
    let setup () =
        DriverTool.Library.Logging.configureConsoleLogging()

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let getAssignedSiteTest () =
        match(result{
            let expected = "T92"
            let actual = getAssignedSite()
            Assert.AreEqual(expected,actual)            
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error (ex:exn) -> Assert.Fail(ex.Message)

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let getSiteServerTest () =
        match(result{
            let expected = "teta410-cm01.teta410.local"
            let actual = getSiteServer()
            Assert.AreEqual(expected,actual)            
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error (ex:exn) -> Assert.Fail(ex.Message)

