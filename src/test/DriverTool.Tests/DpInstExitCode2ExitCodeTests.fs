namespace DriverTool

open NUnit.Framework 

[<TestFixture>]
module DpInstExitCode2ExitCodeTests =
    open System
    open DriverTool

    [<Test>]
    [<TestCase(0x10u,16u,0u,0u,false,false,0u)>]
    let DpInstExitCode2ExitCodeTests (dpInstExitCode:uint, expectedInstalledCount:uint, expectedCouldNotBeInstalledCount:uint, expectedCopiedToDriverStoreCount:uint,expectedCouldNotBeInstalled:bool,expectedRebootNeeded:bool,expectedExitCode:uint) =
        let actual = DpInstExitCode.toDpInstExitCodeInfo dpInstExitCode
        Assert.AreEqual(expectedInstalledCount,actual.InstalledCount,nameof actual.InstalledCount);
        Assert.AreEqual(expectedCouldNotBeInstalledCount, actual.CouldNotBeInstalledCount, nameof actual.CouldNotBeInstalledCount);
        Assert.AreEqual(expectedCopiedToDriverStoreCount, actual.CopiedToDriverStoreCount, nameof actual.CopiedToDriverStoreCount);
        Assert.AreEqual(expectedCouldNotBeInstalled, actual.CouldNotBeInstalled, nameof actual.CouldNotBeInstalled);
        Assert.AreEqual(expectedRebootNeeded, actual.RebootNeeded, nameof actual.RebootNeeded);
        Assert.AreEqual(expectedExitCode, actual.ExitCode, nameof actual.ExitCode);


    [<Test>]
    [<TestCase(0x10u, 0u)>]
    [<TestCase(0x1010u, 0u)>]
    [<TestCase(0x101010u, 0u)>]
    [<TestCase(0x40101010u, 3010u)>]
    [<TestCase(0x80101010u, 1u)>]
    let toExitCodeTest (dpInstExitCode:uint,expectedExitCode:uint) =        
        let actual = DriverTool.DpInstExitCode.toDpInstExitCodeInfo dpInstExitCode
        Assert.AreEqual(expectedExitCode,actual.ExitCode,"Exit code was not expected");