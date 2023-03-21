namespace DriverTool.Tests

open LsupEval
open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module DriverUpdatesTests=
    open DriverTool.Library
    open DriverTool.Library.DriverUpdates
    
    [<SetUp>]    
    let setup () =
        DriverTool.Library.Logging.configureConsoleLogging()

    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let loadDriverUpdatesTest () =
        match(result{            
            let reportProgress = DriverTool.Library.Logging.reportProgressStdOut'
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath
            let manufacturer = DriverTool.Library.ManufacturerTypes.toManufacturer "LENOVO"
            let! model = DriverTool.Library.ModelCode.create "20L6" false
            let operatingSystem = OperatingSystem.getOsNameFromOsShortName "WIN10X64"
            let modelName = $"20L6 %s{operatingSystem}"
            let! operatingSystemCode = OperatingSystemCode.create "WIN10X64" false
            let osBuild = "22H2"
            let! excludeUpdateRegexPatterns = ([|"BIOS";"Firmware"|] |> DriverTool.Library.RegExp.toRegexPatterns true)
            let! actual = loadDriverUpdates reportProgress cacheFolderPath manufacturer model modelName operatingSystemCode osBuild excludeUpdateRegexPatterns
            return actual
        })with
        |Result.Ok a -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(getAccumulatedExceptionMessages ex)