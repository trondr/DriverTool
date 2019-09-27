namespace DriverTool.Tests
open NUnit.Framework
open DriverTool

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module EmbeddedResourceTest  =    
    open DriverTool.EmbeddedResouce
    open System
    open System.IO
    let logger = Common.Logging.Simple.ConsoleOutLogger("EmbeddedResourceTest",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")
    
    [<Test>]
    [<TestCase(@"c:\temp\DpInstExitCode2ExitCode_tst.exe",true,"NotUsed",TestName="extractEmbeddedResourceToFile - Expect success")>]
    [<TestCase(@"c:\temp\folderthatdonotexist\DpInstExitCode2ExitCode_tst.exe",false,"Could not find a part of the path",TestName="extractEmbeddedResourceToFile - Extract to folder that do not exist - Expect failure")>]
    let extractEmbeddedResourceInAssemblyToFileTest (destinationFilePathString:string, expectSuccess: bool, expectedErrorMessage:string) =
        let res =
            result {
                let! testPath = FileSystem.path destinationFilePathString;
                let! testResourceName = ResourceName.create "DriverTool.PackageTemplate.Drivers.DpInstExitCode2ExitCode.exe"
                let! resultPath = EmbeddedResouce.extractEmbeddedResourceInAssemblyToFile (testResourceName, testResourceName.GetType().Assembly,testPath) 
                return resultPath
            }
        match res with
        | Ok p -> 
            Assert.IsTrue(expectSuccess,sprintf "Target call succeded when failure was expected")
        | Error e -> 
            Assert.IsFalse(expectSuccess,sprintf "Target call failed when it was expected to succeded. Error: %s" e.Message)
            Assert.IsTrue(e.Message.StartsWith(expectedErrorMessage),"Error message was not expected. Actual: " + e.Message)
 
    let toStringArray (d:string) =
        d.Split([|'|'|],StringSplitOptions.None)
 
    [<Test>]
    [<TestCase("DriverTool.PackageTemplate.Install.xml",@"DriverTool.PackageTemplate.Install.xml|DriverTool.PackageTemplate.Install|DriverTool.PackageTemplate|DriverTool", TestName="resourceNameToPotentialDirectoriesTest 1")>]
    
    [<TestCase("DriverTool.PackageTemplate.Functions.Copy-Drivers.ps1",@"DriverTool.PackageTemplate.Functions.Copy-Drivers.ps1|DriverTool.PackageTemplate.Functions.Copy-Drivers|DriverTool.PackageTemplate.Functions|DriverTool.PackageTemplate|DriverTool", TestName="resourceNameToPotentialDirectoriesTest 2")>]
    
    [<TestCase("DriverTool.PackageTemplate.Functions.Util.7Zip.7za.exe",@"DriverTool.PackageTemplate.Functions.Util.7Zip.7za.exe|DriverTool.PackageTemplate.Functions.Util.7Zip.7za|DriverTool.PackageTemplate.Functions.Util.7Zip|DriverTool.PackageTemplate.Functions.Util|DriverTool.PackageTemplate.Functions|DriverTool.PackageTemplate|DriverTool", TestName="resourceNameToPotentialDirectoriesTest 3")>]

    let resourceNameToPotentialDirectoriesTest (resourceName:string, expectedDirectories:string) =
        let expectedDirectoriesArray = toStringArray expectedDirectories 
        let actual = EmbeddedResouce.resourceNameToPartialResourceNames (resourceName)
        Assert.AreEqual(expectedDirectoriesArray, actual, "Potential directories were no expected.")   
        
    [<Test>]
    [<TestCase("DriverTool.PackageTemplate.Install.xml",@"c:\temp\testpackage_1",@"c:\temp\testpackage_1\Install.xml")>]
    [<TestCase("DriverTool.PackageTemplate.Drivers.DpInstExitCode2ExitCode.exe",@"c:\temp\testpackage_2",@"c:\temp\testpackage_2\Drivers\DpInstExitCode2ExitCode.exe")>]
    let resourceNameToFileNameTest (resourceName:string, destinationFolderPathString:string,expectedFileName:string) =
         let fileNameResult =
             result{
                let! destinationFolderPath = FileSystem.path destinationFolderPathString
                let dictionary = PackageTemplate.resourceNameToDirectoryDictionary destinationFolderPath
                let fileName = EmbeddedResouce.resourceNameToFileName (resourceName, dictionary)
                return match fileName with
                        |Some fn -> fn
                        |None -> String.Empty
             }
         match fileNameResult with         
         |Ok actual -> Assert.AreEqual(expectedFileName, actual, "Resource file name was not expected.")
         |Error ex -> Assert.Fail("Test failed unexpectedly due to: " + ex.Message)


    [<Test>]
    let extractPackageTemplateTest () = 
        let destinationFolderPathString = @"c:\temp\testpackage_test" 
        if (System.IO.Directory.Exists(destinationFolderPathString)) then
                    System.IO.Directory.Delete(destinationFolderPathString,true)
                    |>ignore
        let res =
            result{
                let! destinationFolderPath = FileSystem.path destinationFolderPathString
                let! extractedFiles = PackageTemplate.extractPackageTemplate destinationFolderPath
                printfn "Extracted files: %A" (extractedFiles |> Seq.toArray)
                return destinationFolderPath
            }
        match res with
        |Ok p -> 
            Assert.IsTrue(true)
            Assert.AreEqual(15, System.IO.Directory.GetFiles(FileSystem.pathValue p,"*.*",System.IO.SearchOption.AllDirectories).Length, sprintf "Extracted file count not expected in %s. This number must be adjusted by the developer if files are added or removed from the package template folder '<solutiondirectory>\src\app\DriverTool\PackageTemplate'. %A" destinationFolderPathString (System.IO.Directory.GetFiles(destinationFolderPathString,"*.*",SearchOption.AllDirectories)))
        |Error ex -> Assert.IsTrue(false,ex.Message)

    [<Test>]
    let getPackageTemplateEmbeddedResourceNamesTest () =
        let actual = PackageTemplate.getPackageTemplateEmbeddedResourceNames()
        let actualResourceNameCount = (actual |> Seq.toList).Length
        Assert.AreEqual(11,actualResourceNameCount,"Resource name count not expected. This number must be adjusted by the developer if files are added or removed from the package template folder '<solutiondirectory>\src\app\DriverTool\PackageTemplate'.")


    [<Test>]
    let mapResourceNamesToFileNamesTest () =
        
        let destinationFolderPathResult = FileSystem.path @"c:\temp\testpackage2"
        match destinationFolderPathResult with
        |Ok destinationFolderPath ->
            let actual = EmbeddedResouce.mapResourceNamesToFileNames (destinationFolderPath,PackageTemplate.getPackageTemplateEmbeddedResourceNames(),PackageTemplate.resourceNameToDirectoryDictionary)
            let actualResourceNameToFileMapCount = (actual |> Seq.toList).Length
            Assert.AreEqual(11, actualResourceNameToFileMapCount,"Resource name vs file name count not expected. This number must be adjusted by the developer if files are added or removed from the package template folder '<solutiondirectory>\src\app\DriverTool\PackageTemplate'. If a folder is added to the package templated folder structure, the resourceNameToDirectoryDictionary function must be updated also.")
        |Error ex -> Assert.Fail(ex.Message)
    
    [<Test>]
    let getAllEmbeddedResourceNamesTest () =
        let actual = 
            EmbeddedResouce.getAllEmbeddedResourceNames
        let allResourceNames = String.concat Environment.NewLine actual                
        Assert.AreEqual(60,actual.Length,allResourceNames)


    [<Test>]
    let extractedEmbeddedResourceTest () =
        let extractAndDispose =            
            use extractedEmbeddedResource = new ExtractedEmbeddedResource("7za.exe",logger)
            match(result{
                let! filePath = extractedEmbeddedResource.FilePath
                Assert.IsTrue(System.IO.File.Exists(FileSystem.pathValue filePath),sprintf "File does not exist: '%A'" filePath)
                return filePath
                })with
            |Ok p -> p
            |Result.Error ex -> raise ex
        let filePath = extractAndDispose
        Assert.IsFalse(FileSystem.fileExists filePath,sprintf "File exists: '%A'" filePath)        
        ()