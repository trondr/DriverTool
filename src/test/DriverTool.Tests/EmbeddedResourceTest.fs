namespace DriverTool.Tests
open NUnit.Framework
open DriverTool

[<TestFixture>]
module EmbeddedResourceTest  =    
    open DriverTool.EmbeddedResouce
    open System

    [<Test>]
    [<TestCase(@"c:\temp\DpInstExitCode2ExitCode_tst.exe",true,"NotUsed",TestName="extractEmbeddedResourceToFile - Expect success")>]
    [<TestCase(@"c:\temp\folderthatdonotexist\DpInstExitCode2ExitCode_tst.exe",false,"Could not find a part of the path",TestName="extractEmbeddedResourceToFile - Extract to folder that do not exist - Expect failure")>]
    let extractEmbeddedResourceToFile (destinationFilePathString:string, expectSuccess: bool, expectedErrorMessage:string) =
        let res =
            result {
                let! testPath = Path.create destinationFilePathString;
                let! testResourceName = ResourceName.create "DriverTool.PackageTemplate.Drivers.DpInstExitCode2ExitCode.exe"
                let! resultPath = EmbeddedResouce.extractEmbeddedResourceToFile (testResourceName, testResourceName.GetType().Assembly,testPath) 
                return resultPath
            }
        match res with
        | Ok p -> 
            Assert.IsTrue(expectSuccess,System.String.Format("Target call succeded when failure was expected"))
        | Error e -> 
            Assert.IsFalse(expectSuccess,System.String.Format("Target call failed when it was expected to succeded. Error: {0}",e.Message))
            Assert.IsTrue(e.Message.StartsWith(expectedErrorMessage),"Error message was not expected. Actual: " + e.Message)
 
    let toStringArray (d:string) =
        d.Split("|",StringSplitOptions.None)
 
    [<Test>]
    [<TestCase("DriverTool.PackageTemplate.Install.xml",@"DriverTool.PackageTemplate.Install.xml|DriverTool.PackageTemplate.Install|DriverTool.PackageTemplate|DriverTool", TestName="resourceNameToPotentialDirectoriesTest 1")>]
    
    [<TestCase("DriverTool.PackageTemplate.Functions.Copy-Drivers.ps1",@"DriverTool.PackageTemplate.Functions.Copy-Drivers.ps1|DriverTool.PackageTemplate.Functions.Copy-Drivers|DriverTool.PackageTemplate.Functions|DriverTool.PackageTemplate|DriverTool", TestName="resourceNameToPotentialDirectoriesTest 2")>]
    
    [<TestCase("DriverTool.PackageTemplate.Functions.Util.7Zip.7za.exe",@"DriverTool.PackageTemplate.Functions.Util.7Zip.7za.exe|DriverTool.PackageTemplate.Functions.Util.7Zip.7za|DriverTool.PackageTemplate.Functions.Util.7Zip|DriverTool.PackageTemplate.Functions.Util|DriverTool.PackageTemplate.Functions|DriverTool.PackageTemplate|DriverTool", TestName="resourceNameToPotentialDirectoriesTest 3")>]

    let resourceNameToPotentialDirectoriesTest (resourceName:string, expectedDirectories:string) =
        let expectedDirectoriesArray = toStringArray expectedDirectories 
        let actual = CreateDriverPackage.resourceNameToPartialResourceNames (resourceName)
        Assert.AreEqual(expectedDirectoriesArray, actual, "Potential directories were no expected.")   
        
    [<Test>]
    [<TestCase("DriverTool.PackageTemplate.Install.xml",@"c:\temp\testpackage",@"c:\temp\testpackage\Install.xml")>]
    [<TestCase("DriverTool.PackageTemplate.Functions.Copy-Drivers.ps1",@"c:\temp\testpackage",@"c:\temp\testpackage\Functions\Copy-Drivers.ps1")>]
    [<TestCase("DriverTool.PackageTemplate.Functions.Util.7Zip.7za.exe",@"c:\temp\testpackage",@"c:\temp\testpackage\Functions\Util\7Zip\7za.exe")>]
    let resourceNameToFileNameTest (resourceName:string, destinationFolderPathString:string,expectedFileName:string) =
         let fileNameResult =
             result{
                let! destinationFolderPath = Path.create destinationFolderPathString
                let dictionary = CreateDriverPackage.resourceNameToDirectoryDictionary destinationFolderPath
                let fileName = CreateDriverPackage.resourceNameToFileName (resourceName, dictionary)
                return match fileName with
                    |Some fn -> fn
                    |None -> String.Empty
             }
         match fileNameResult with         
         |Ok actual -> Assert.AreEqual(expectedFileName, actual, "Resource file name was not expected.")
         |Error ex -> Assert.Fail("Test failed unexpectedly due to: " + ex.Message)


    [<Test>]
    let extractPackageTemplateTest () = 
        let res =
            result{
                let! destinationFolderPath = Path.create @"c:\temp\testpackage"
                System.IO.Directory.Delete(destinationFolderPath.Value,true)
                CreateDriverPackage.extractPackageTemplate destinationFolderPath |> ignore
                return destinationFolderPath
            }
        match res with
        |Ok v -> Assert.IsTrue(true)
        |Error ex -> Assert.IsTrue(false,ex.Message)
        