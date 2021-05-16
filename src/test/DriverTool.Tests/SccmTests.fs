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
            let! actual = createCustomTaskSequence "Example Task Sequence 1.0" "Example Description" "INSTALL-OFFLINE-OS" [|packageDefinitionSmsFilePath1;packageDefinitionSmsFilePath2|]
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error (ex:exn) -> Assert.Fail(getAccumulatedExceptionMessages ex)

    open DriverTool.Library.PackageXml

    let createTestPackageDefinition packageName programName manufacturerName modelName =
        result{
            let! installProgram = createSmsProgram programName "Install.cmd > %public%\\Logs\\Install.cmd.log" "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "Install application"
            let! unInstallProgram = createSmsProgram "UNINSTALL" "Uninstall.cmd > %public%\\Logs\\Uninstall.cmd.log" "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "Uninstall application"
            let manufacturerWmiQuery =
                {
                    Name = manufacturerName
                    NameSpace = "root\\WMI"
                    Query = "select where " + manufacturerName
                }
            let modelWmiQuery =
                {
                    Name = modelName
                    NameSpace = "root\\WMI"
                    Query = "select where " + modelName
                }
            let! expected = createSmsPackageDefinition packageName "1.0" (Some "App.ico") "trondr" "EN" false (sprintf "%s Comment" packageName) [|installProgram;unInstallProgram|] manufacturerWmiQuery modelWmiQuery
            return expected
        }
    let expected = """$ManufacturerGroups = @()
$ModelGroups = @()
$package = Get-CMPackage -Name 'Example Application 1' -Fast
$commandLineStep = New-CMTSStepRunCommandLine -PackageId $($Package.PackageID) -Name "Example Application 1" -CommandLine 'Install.cmd > %public%\Logs\Install.cmd.log' -SuccessCode @(0,3010) -Description "Install application"
$restartStep = New-CMTSStepReboot -Name "Restart" -RunAfterRestart "HardDisk" -NotificationMessage "A new Microsoft Windows operating system is being installed. The computer must restart to continue." -MessageTimeout 3
$ModelGroupCondition = New-CMTSStepConditionQueryWMI -Namespace "root\WMI" -Query "select where Lenovo ThinkPad X1 Yoga"
$ModelGroups += New-CMTaskSequenceGroup -Name 'Lenovo ThinkPad X1 Yoga' -Condition @($ModelGroupCondition) -Step @($commandLineStep,$restartStep)
$GroupCondition = New-CMTSStepConditionQueryWMI -Namespace "root\WMI" -Query "select where Lenovo"
$ManufacturerGroups += New-CMTaskSequenceGroup -Name 'Lenovo' -Description 'Manufacturer Lenovo' -Condition @($GroupCondition) -Step @($ModelGroups)
$ModelGroups = @()
$package = Get-CMPackage -Name 'Example Application 2' -Fast
$commandLineStep = New-CMTSStepRunCommandLine -PackageId $($Package.PackageID) -Name "Example Application 2" -CommandLine 'Install.cmd > %public%\Logs\Install.cmd.log' -SuccessCode @(0,3010) -Description "Install application"
$restartStep = New-CMTSStepReboot -Name "Restart" -RunAfterRestart "HardDisk" -NotificationMessage "A new Microsoft Windows operating system is being installed. The computer must restart to continue." -MessageTimeout 3
$ModelGroupCondition = New-CMTSStepConditionQueryWMI -Namespace "root\WMI" -Query "select where HP EliteBook 830"
$ModelGroups += New-CMTaskSequenceGroup -Name 'HP EliteBook 830' -Condition @($ModelGroupCondition) -Step @($commandLineStep,$restartStep)
$GroupCondition = New-CMTSStepConditionQueryWMI -Namespace "root\WMI" -Query "select where HP"
$ManufacturerGroups += New-CMTaskSequenceGroup -Name 'HP' -Description 'Manufacturer HP' -Condition @($GroupCondition) -Step @($ModelGroups)
$ModelGroups = @()
$package = Get-CMPackage -Name 'Example Application 3' -Fast
$commandLineStep = New-CMTSStepRunCommandLine -PackageId $($Package.PackageID) -Name "Example Application 3" -CommandLine 'Install.cmd > %public%\Logs\Install.cmd.log' -SuccessCode @(0,3010) -Description "Install application"
$restartStep = New-CMTSStepReboot -Name "Restart" -RunAfterRestart "HardDisk" -NotificationMessage "A new Microsoft Windows operating system is being installed. The computer must restart to continue." -MessageTimeout 3
$ModelGroupCondition = New-CMTSStepConditionQueryWMI -Namespace "root\WMI" -Query "select where Dell Latitude 550"
$ModelGroups += New-CMTaskSequenceGroup -Name 'Dell Latitude 550' -Condition @($ModelGroupCondition) -Step @($commandLineStep,$restartStep)
$GroupCondition = New-CMTSStepConditionQueryWMI -Namespace "root\WMI" -Query "select where Dell"
$ManufacturerGroups += New-CMTaskSequenceGroup -Name 'Dell' -Description 'Manufacturer Dell' -Condition @($GroupCondition) -Step @($ModelGroups)
$ApplyDriversGroup = New-CMTaskSequenceGroup -Name 'Apply Drivers' -Description 'Apply drivers to the offline operating system.' -Step @($ManufacturerGroups)
$taskSequence = New-CMTaskSequence -CustomTaskSequence -Name 'Example TS 1.0' -Description 'Example Custom Task Sequence'
Add-CMTaskSequenceStep -TaskSequenceName 'Example TS 1.0' -Step @($ApplyDriversGroup)"""
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let toCustomTaskSequenceScriptTest () =
        match(result{
            let programName = "INSTALL-OFFLINE-OS"
            let! packageDefinition1 = createTestPackageDefinition "Example Application 1" programName "Lenovo" "Lenovo ThinkPad X1 Yoga"
            let! packageDefinition2 = createTestPackageDefinition "Example Application 2" programName "HP" "HP EliteBook 830"
            let! packageDefinition3 = createTestPackageDefinition "Example Application 3" programName "Dell" "Dell Latitude 550"            
            let! actual = toCustomTaskSequenceScript "Example TS 1.0" "Example Custom Task Sequence" programName [|packageDefinition1;packageDefinition2;packageDefinition3|]
            Assert.AreEqual(expected,actual,"Custom task sequence PowerShell script was not as expected.")
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error (ex:exn) -> Assert.Fail(getAccumulatedExceptionMessages ex)