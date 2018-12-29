namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module BitLockerOperationsTest =

    [<Test>]
    let isBitLockerEnabledTest () =
        let actual = DriverTool.BitLockerOperations.isBitLockerEnabled()
        Assert.IsNotNull(actual,"Was null")
        ()

