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
    let writeToFileTest () =
        match(result{
            let! filePath = FileSystem.path "c:\\temp\\Example_PackageDefinition.sms"
            let! installProgram = createSmsProgram "INSTALL" "Install.cmd > %public%\\Logs\\Install.cmd.log" "" SmsCanRunWhen.AnyUserStatus true true SmsProgramMode.Hidden "Install application"
            let! unInstallProgram = createSmsProgram "UNINSTALL" "Uninstall.cmd > %public%\\Logs\\Uninstall.cmd.log" "" SmsCanRunWhen.AnyUserStatus true true SmsProgramMode.Hidden "Uninstall application"
            let! packageDefinition = createSmsPackageDefinition "Example Application" "1.0" (Some "App.ico") "trondr" "EN" "Example Application" [|installProgram;unInstallProgram|]
            let! actual = writeToFile logger filePath packageDefinition 
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

