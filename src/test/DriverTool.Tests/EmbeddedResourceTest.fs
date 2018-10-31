namespace DriverTool.Tests
open NUnit.Framework
open DriverTool

[<TestFixture>]
module EmbeddedResourceTest  =    
    open DriverTool.EmbeddedResouce

    [<Test>]
    let extractEmbeddedResourceToFile () =
        let res =
            result {
                let! testPath = Path.create @"c:\temp\DpInstExitCode2ExitCode_tst.exe";
                let! testResourceName = ResourceName.create "DriverTool.PackageTemplate.Drivers.DpInstExitCode2ExitCode.exe"
                let! resultPath = EmbeddedResouce.extractEmbeddedResourceToFile (testResourceName, testResourceName.GetType().Assembly,testPath) 
                return resultPath
            }
        match res with
        | Ok p -> Assert.IsTrue(true,System.String.Format("Success was expected"))
        | Error e -> Assert.IsTrue(false,System.String.Format("Error: {0}",e.Message))
        