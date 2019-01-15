namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module BitLockerOperationsTest =
    open DriverTool

    [<Test>]
    let isBitLockerEnabledTest () =
        if(not (Requirements.isAdministrator())) then
            Assert.Inconclusive("This test must be run with admin privileges.")
        else
            let actual = DriverTool.BitLockerOperations.isBitLockerEnabled()
            Assert.IsNotNull(actual,"Was null")
            ()

