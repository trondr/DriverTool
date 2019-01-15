namespace DriverTool.Tests
open NUnit.Framework
open System
open DriverTool

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module WmiHelperTests  =
    open DriverTool

    [<Test>]
    let WmiHelperTest_Success() =
        let actualResult = WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "Name"
        let expected = System.Environment.GetEnvironmentVariable("COMPUTERNAME")
        match actualResult with
        |Ok actual -> Assert.AreEqual(expected,actual,"Computer name not expected.")
        |Error e -> Assert.Fail(String.Format("{0}", e.Message))
        
    [<Test>]
    let WmiHelperTest_InvalidClassName() =
        let actualResult = WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem2" "Name"        
        match actualResult with
        |Ok actual -> Assert.Fail("This test did not fail as expected due to invalid class name.")
        |Error e -> Assert.AreEqual("Not found ",String.Format("{0}", e.Message))
    
    [<Test>]
    let WmiHelperTest_InvalidPropertyName() =
        let actualResult = WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "Name2"
        let expected = System.Environment.GetEnvironmentVariable("COMPUTERNAME")
        match actualResult with
        |Ok actual -> Assert.Fail("This test did not fail as expected due to invalid property name.")
        |Error e -> Assert.AreEqual("Not found ",String.Format("{0}", e.Message))

    [<Test>]
    let WmiHelper2Test_Success() =
        let actualResult = WmiHelper.getWmiProperty "root\WMI" "MS_SystemInformation" "BaseBoardProduct"
        match actualResult with
        |Ok actual -> Assert.IsFalse(System.String.IsNullOrWhiteSpace(actual),"Is null")
        |Error e -> Assert.Fail(String.Format("{0}", e.Message))