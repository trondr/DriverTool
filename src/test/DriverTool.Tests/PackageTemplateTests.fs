﻿namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.IntegrationTests)>]
module PackageTemplateTests =    
    open DriverTool.Library.F
    open DriverTool.Library
    
    let expectedPackageTemplateFiles =
        [|
            @"Install.cmd"
            @"Install.xml"
            @"UnInstall.cmd"
            @"_Compress.cmd"
            @"_Decompress.cmd"
            @"Drivers\DpInstExitCode2ExitCode.exe"
            @"Drivers\DpInstExitCode2ExitCode.exe.config"
            @"Drivers\Drivers-README.txt"
            @"Drivers\DriverTool.DupExitCode2ExitCode.exe"
            @"Drivers\DriverTool.DupExitCode2ExitCode.exe.config"
            @"Drivers\FSharp.Core.dll"
            @"DriverTool\DriverTool.exe"
            @"DriverTool\DriverTool.exe.config"
            @"DriverTool\DriverTool.Library.dll"
            @"DriverTool\FSharp.Core.dll"
            @"DriverTool\Common.Logging.dll"
        |]
    
    [<Test>]
    let extractPackageTemplateTest () =
        let getFileCount (destintionFolderPath:FileSystem.Path) =
            System.IO.Directory.GetFiles(FileSystem.pathValue destintionFolderPath,"*.*",System.IO.SearchOption.AllDirectories).Length
        
        result{
            let! destinationFolderPath = FileSystem.path (System.IO.Path.Combine(System.IO.Path.GetTempPath(),"extractPackageTemplateTest"))
            DriverTool.Library.DirectoryOperations.deleteDirectory true destinationFolderPath|>ignore
            let! existingDestinationFolderPath = DirectoryOperations.ensureDirectoryExists true destinationFolderPath
            let! extractedFiles = DriverTool.Library.PackageTemplate.extractPackageTemplate existingDestinationFolderPath
            let expectedFileCount = expectedPackageTemplateFiles.Length
            let actualFileCount = getFileCount existingDestinationFolderPath
            
            expectedPackageTemplateFiles
            |>Seq.map (fun f -> 
                         let file = System.IO.Path.Combine(FileSystem.pathValue existingDestinationFolderPath,f)
                         Assert.IsTrue(System.IO.File.Exists(file),"Extracted file does not exist:" + file)
                         
                        )
            |>Seq.toArray |> ignore

            
            Assert.AreEqual(expectedFileCount,actualFileCount,"Number of files in extracted package template is not expected")
            return extractedFiles
        } |> ignore

        
         