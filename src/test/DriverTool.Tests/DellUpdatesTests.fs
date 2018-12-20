namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module DellUpdatesTests =
    open DriverTool
    open F
    open System

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
        
        
    [<Test>]
    [<TestCase("07A7","WIN10X64")>]
    let getUpdatesTest (modelCode,operatingSystemCode) =
        match(result{
            //let! actual = getUpdates (modelCode, operatingSystemCode)
            let! result = Result.Ok 1
            return! Result.Error (new Exception("Not implemented"))
        }) with
        |Ok _->Assert.IsTrue(true)
        |Error ex ->Assert.Fail(ex.Message)

