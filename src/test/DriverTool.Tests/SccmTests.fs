namespace DriverTool.Tests
open System.Management.Automation

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
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
            let! actual = getAssignedSite()
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
            let! actual = getSiteServer()
            Assert.AreEqual(expected,actual)            
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error (ex:exn) -> Assert.Fail(ex.Message)


    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let createPackageFromDefinitionTest () =
        match(result{            
            let sourceFolderPath = "\\\\TETA410-CM01\\PkgSrc$\\Packages\\Example Package\\1.0\\Scripts"
            let packageDefinitionSms = "\\\\TETA410-CM01\\PkgSrc$\\Packages\\Example Package\\1.0\\Scripts\\PackageDefinition.sms"
            let! actual = createPackageFromDefinition packageDefinitionSms sourceFolderPath           
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error (ex:exn) -> Assert.Fail(getAccumulatedExceptionMessages ex)

    open DriverTool.Library.PackageDefinitionSms

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let toNewCmProgramPSCommandTest () =
        match(result{
            let expected = "New-CMProgram -PackageId \"TST123456\" -StandardProgramName 'INSTALL' -CommandLine 'Install.cmd' -WorkingDirectory '' -UserInteraction:$false -RunMode RunWithAdministrativeRights -RunType Hidden -ProgramRunType WhetherOrNotUserIsLoggedOn -DriveMode RunWithUnc | Set-CMProgram -Comment 'This is an example program' -StandardProgram"
            let testPackageId = "TST123456"
            let! testprogram = createSmsProgram "INSTALL" "Install.cmd" "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "This is an example program"
            let actual = Sccm.toNewCmProgramPSCommand testPackageId testprogram
            Assert.AreEqual(expected,actual,"New-CMProgram powershell command was not as expected.")
            return testprogram
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error (ex:exn) -> Assert.Fail(getAccumulatedExceptionMessages ex)

//[-AddSupportedOperatingSystemPlatform <IResultObject[]>]
//[-DiskSpaceRequirement <String>]
//[-DiskSpaceUnit <DiskSpaceUnitType>]
//[-Duration <Int32>]
//[-DisableWildcardHandling]
//[-ForceWildcardHandling]
//[-WhatIf]