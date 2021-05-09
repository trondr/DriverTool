namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.ManualTests)>]
module PackageDefintionSmsTests=
    open DriverTool.Library
    open DriverTool.Library.PackageDefinitionSms
    
    [<SetUp>]    
    let setup () =
        DriverTool.Library.Logging.configureConsoleLogging()

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let writeToFileAndReadFromFileTest () =
        match(result{
            let! filePath = FileSystem.path "c:\\temp\\Example_PackageDefinition.sms"
            let! installProgram = createSmsProgram "INSTALL" "Install.cmd > %public%\\Logs\\Install.cmd.log" "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "Install application"
            let! unInstallProgram = createSmsProgram "UNINSTALL" "Uninstall.cmd > %public%\\Logs\\Uninstall.cmd.log" "" SmsCanRunWhen.AnyUserStatus true true false (Some SmsProgramMode.Hidden) "Uninstall application"
            let! expected = createSmsPackageDefinition "Example Application" "1.0" (Some "App.ico") "trondr" "EN" false "Example Application" [|installProgram;unInstallProgram|]
            let! path = writeToFile logger filePath expected
            let! actual2 = readFromFile filePath
            Assert.AreEqual(expected,actual2)
            return actual2
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(getAccumulatedExceptionMessages ex)

