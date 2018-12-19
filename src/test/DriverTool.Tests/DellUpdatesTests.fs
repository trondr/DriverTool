namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module DellUpdatesTests =
    open DriverTool
    open F

    [<Test>]
    let downloadAndLoadSoftwareCatalogTest () =
        match(result{
            let! actual = DriverTool.DellUpdates.downloadAndLoadSoftwareCatalog ()
            Assert.IsTrue(actual.SoftwareComponents.Length > 0,"Number of software components found in Dell software catalog is 0")
            Assert.IsNotNull(actual)
            return actual        
        }) with
        |Ok _->Assert.IsTrue(true)
        |Error ex ->Assert.Fail(ex.Message)
        
        


