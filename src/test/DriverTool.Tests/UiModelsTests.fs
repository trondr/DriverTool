﻿namespace DriverTool.Tests

open NUnit.Framework
open DriverTool.Library
open DriverTool.Library.CmUi.UiModels
open DriverTool.Library.Logging

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module UiModelsTests =

    [<SetUp>]    
    let setup () =
        DriverTool.Library.Logging.configureConsoleLogging()

    [<Test>]
    let loadSccmPackagesTest () =
        match(result{
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath
            let! sccmPackages = loadSccmPackages(cacheFolderPath)
            return sccmPackages
        })with
        |Result.Ok ps -> Assert.IsTrue(ps.Length > 0, "Number of returned Sccm Packages are not greater than 0")
        |Result.Error ex -> Assert.Fail(ex.Message)        
