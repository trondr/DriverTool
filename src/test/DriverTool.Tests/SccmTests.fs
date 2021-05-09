namespace DriverTool.Tests
open System.Management.Automation

open NUnit.Framework

[<TestFixture>]
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
            let! sourceFolderPath = FileSystem.path "\\\\TETA410-CM01\\PkgSrc$\\Packages\\Example Package\\1.0\\Scripts"
            let! packageDefinitionSmsFilePath = FileSystem.path "\\\\TETA410-CM01\\PkgSrc$\\Packages\\Example Package\\1.0\\Scripts\\PackageDefinition.sms"
            let! packageDefinition = PackageDefinitionSms.readFromFile packageDefinitionSmsFilePath
            let! actual = createPackageFromDefinition sourceFolderPath packageDefinition
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error (ex:exn) -> Assert.Fail(getAccumulatedExceptionMessages ex)

    open DriverTool.Library.PackageDefinitionSms

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let toNewCmProgramPSCommandTest () =
        match(result{
            let expected = "New-CMProgram -PackageId \"TST123456\" -StandardProgramName 'INSTALL' -CommandLine 'Install.cmd' -UserInteraction:$false -RunMode RunWithAdministrativeRights -RunType Hidden -ProgramRunType WhetherOrNotUserIsLoggedOn -DriveMode RunWithUnc | Set-CMProgram -Comment 'This is an example program' -StandardProgram"
            let testPackageId = "TST123456"
            let! testprogram = createSmsProgram "INSTALL" "Install.cmd" "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "This is an example program"
            let actual = Sccm.toNewCmProgramPSCommand testPackageId testprogram
            Assert.AreEqual(expected,actual,"New-CMProgram powershell command was not as expected.")
            return testprogram
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error (ex:exn) -> Assert.Fail(getAccumulatedExceptionMessages ex)

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let createCustomTaskSequenceTest () =
        match(result{                        
            let! packageDefinitionSmsFilePath1 = FileSystem.path "\\\\TETA410-CM01\\PkgSrc$\\Packages\\Example Package\\1.0\\Scripts\\PackageDefinition.sms"
            let! packageDefinitionSmsFilePath2 = FileSystem.path "\\\\TETA410-CM01\\PkgSrc$\\Packages\\Example Package\\2.0\\Scripts\\PackageDefinition.sms"
            let packageDefinitionSmsFilePaths = [|packageDefinitionSmsFilePath1;packageDefinitionSmsFilePath2|]
            //let! cmpackages = 
            //    packageDefinitionSmsFilePaths
            //    |>Array.map(fun fp ->
            //        result{
            //            let! packageDefinition = PackageDefinitionSms.readFromFile fp
            //            let! sourceFolderPath = FileOperations.getParentPath fp
            //            let! actual = createPackageFromDefinition sourceFolderPath packageDefinition
            //            return actual
            //        }
            //    )
            //    |>toAccumulatedResult            
            let! actual = createCustomTaskSequence "Example Task Sequence 1.0" "Example Description" [|packageDefinitionSmsFilePath1;packageDefinitionSmsFilePath2|]
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error (ex:exn) -> Assert.Fail(getAccumulatedExceptionMessages ex)