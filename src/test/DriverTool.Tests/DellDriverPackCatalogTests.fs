namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module DellDriverPackCatalogTests =
    open DriverTool.Library
    open DriverTool.Library.DellDriverPackCatalog
    open DriverTool.Tests.Init

    [<SetUp>]    
    let setup () =
        DriverTool.Library.Logging.configureConsoleLogging()

    [<Test>]
    [<Category(TestCategory.UnitTests)>]    
    let loadCatalogTest () =
        match(result{
            use temporaryCacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! temproaryCacheFolderPath = temporaryCacheFolder.FolderPath 
            let driverPackCatalogXmlFileName = "DriverPackCatalog.xml"
            logger.Info(sprintf "Extract from embeded resource: %s"  driverPackCatalogXmlFileName)
            let! catalogPath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (driverPackCatalogXmlFileName,temproaryCacheFolderPath,driverPackCatalogXmlFileName,typeof<ThisTestAssembly>.Assembly)
            let! actual = DriverTool.Library.DellDriverPackCatalog.loadCatalog catalogPath //Result.Error (toException "Not imlemented" None)
            Assert.IsTrue(actual.Length > 0,"Did not load any DriverPackage's")            
            actual |> Array.map(fun p -> 
                logger.Info(sprintf "Checking DriverPackage: %A" p)
                Assert.IsFalse(System.String.IsNullOrWhiteSpace(p.Name),"DriverPackage.Name is null or empty")
                Assert.IsFalse(p.Name.Contains("DisplayName"),"DriverPackage.Name contains 'DisplayName'")

                Assert.IsTrue(p.Models.Length >= 0,sprintf "No supported models for model '%A'" p)

                Assert.IsTrue(p.OperatinSystems.Length > 0,sprintf "No supported operatingsystems for model '%A'" p)

                Assert.IsTrue(p.Installer.Size > 0L, "Size of installer is not greater than 0.")

                Assert.IsTrue(p.PackageType = "winpe" || p.PackageType = "win")
            ) |> ignore
            
            
            return actual
        }) with
        |Result.Ok a -> Assert.IsTrue(a.Length > 0)
        |Result.Error ex -> Assert.Fail(ex.Message)
        

