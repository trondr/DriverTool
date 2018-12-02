namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module PackageTemplateTests =
    open DriverTool
    
    let expectedPackageTemplateFiles =
        [|
            @"Install.cmd"
            @"Install.config.xml"
            @"Install.ps1"
            @"Install.xml"
            @"Drivers\DpInstExitCode2ExitCode.exe"
            @"Drivers\DpInstExitCode2ExitCode.exe.config"
            @"Drivers\Drivers-README.txt"
            @"Drivers_Example\DpInstExitCode2ExitCode.exe"
            @"Drivers_Example\DpInstExitCode2ExitCode.exe.config"
            @"Drivers_Example\Drivers-README.txt"
            @"Drivers_Example\020_Audio_Realtek_Audio_Driver_10_1_3_2017_08_23\DT-Install-Package.cmd"
            @"Drivers_Example\020_Audio_Realtek_Audio_Driver_10_1_3_2017_08_23\DT-UnInstall-Package.cmd"
            @"Drivers_Example\040_Camera_and_Card_Reader_Re_10_64_1_2_2018_03_29\DT-Install-Package.cmd"
            @"Drivers_Example\040_Camera_and_Card_Reader_Re_10_64_1_2_2018_03_29\DT-UnInstall-Package.cmd"
            @"Functions\Assert-DirectoryExists.ps1"
            @"Functions\Assert-DirectoryNotExists.ps1"
            @"Functions\Assert-FileExists.ps1"
            @"Functions\Assert-FileNotExists.ps1"
            @"Functions\Assert-IsAdministrator.ps1"
            @"Functions\Assert-IsSupported.ps1"
            @"Functions\Clear-Cache.ps1"
            @"Functions\Compress-Drivers.ps1"
            @"Functions\Compress-Folder.ps1"
            @"Functions\Copy-Drivers.ps1"
            @"Functions\Expand-Drivers.ps1"
            @"Functions\Expand-Folder.ps1"
            @"Functions\Get-7ZipExe.ps1"
            @"Functions\Get-ApplicationName.ps1"
            @"Functions\Get-ApplicationRegistryPath.ps1"
            @"Functions\Get-Cache.ps1"
            @"Functions\Get-CachedValue.ps1"
            @"Functions\Get-ChangeExe.ps1"
            @"Functions\Get-CmdExe.ps1"
            @"Functions\Get-Company.ps1"
            @"Functions\Get-ConsoleLogLine.ps1"
            @"Functions\Get-DestinationDriverToolResumeBitLockerProtectionCmd.ps1"
            @"Functions\Get-DriversFolder.ps1"
            @"Functions\Get-DriversZipFile.ps1"
            @"Functions\Get-DriverToolProgramDataFolder.ps1"
            @"Functions\Get-DriverToolResumeBitLockerProtectionCmdFileName.ps1"
            @"Functions\Get-DriverToolResumeBitLockerProtectionScheduledTaskName.ps1"
            @"Functions\Get-DriverToolResumeBitLockerProtectionScheduledTaskXml.ps1"
            @"Functions\Get-FunctionsFolder.ps1"
            @"Functions\Get-FunctionsUtilFolder.ps1"
            @"Functions\Get-InstallPackageScriptName.ps1"
            @"Functions\Get-InstallProperty.ps1"
            @"Functions\Get-InstallXml.ps1"
            @"Functions\Get-LocalDriversFolder.ps1"
            @"Functions\Get-LogDirectory.ps1"
            @"Functions\Get-LogFile.ps1"
            @"Functions\Get-LogFileLogLine.ps1"
            @"Functions\Get-LogFileName.ps1"
            @"Functions\Get-ManageBdeExe.ps1"
            @"Functions\Get-OsShortName.ps1"
            @"Functions\Get-PackageFolderName.ps1"
            @"Functions\Get-RobocopyExe.ps1"
            @"Functions\Get-RobocopyResult.ps1"
            @"Functions\Get-SchTasksExe.ps1"
            @"Functions\Get-ScriptFolder.ps1"
            @"Functions\Get-SourceDriverToolResumeBitLockerProtectionCmd.ps1"
            @"Functions\Get-SystemFolder.ps1"
            @"Functions\Get-UnInstallPackageScriptName.ps1"
            @"Functions\Import-DriverToolUtilLibrary.ps1"
            @"Functions\Import-IniFileParserLibrary.ps1"
            @"Functions\Import-Library.ps1"
            @"Functions\Import-Log4NetLibrary.ps1"
            @"Functions\Initialize-Logging.ps1"
            @"Functions\Install-Drivers.ps1"
            @"Functions\Install-DriverToolResumeBitLockerProtectionCmd.ps1"
            @"Functions\Invoke-ChangeUserExecute.ps1"
            @"Functions\Invoke-ChangeUserInstall.ps1"
            @"Functions\Invoke-InstallAction.ps1"
            @"Functions\Invoke-Robocopy.ps1"
            @"Functions\Read-IniValue.ps1"
            @"Functions\Register-Application.ps1"
            @"Functions\Remove-DriverToolProgramDataFolder.ps1"
            @"Functions\Reset-ConfigFlags.ps1"
            @"Functions\Resume-BitLockerProtection.ps1"
            @"Functions\Show-Cache.ps1"
            @"Functions\Start-ConsoleProcess.ps1"
            @"Functions\Suspend-BitLockerProtection.ps1"
            @"Functions\Test-IsAdministrator.ps1"
            @"Functions\Test-IsBitLockerProtected.ps1"
            @"Functions\Test-IsPathLocal.ps1"
            @"Functions\Test-IsTerminalServer.ps1"
            @"Functions\Trace-FunctionCall.ps1"
            @"Functions\UnInstall-Drivers.ps1"
            @"Functions\UnInstall-DriverToolResumeBitLockerProtectionCmd.ps1"
            @"Functions\Unregister-Application.ps1"
            @"Functions\Use-Culture.ps1"
            @"Functions\Use-Object.ps1"            
            @"Functions\Write-IniValue.ps1"
            @"Functions\Write-Log.ps1"
            @"Functions\Util\7Zip\7zip.chm"
            @"Functions\Util\7Zip\7za.exe"
            @"Functions\Util\7Zip\License.txt"
            @"Functions\Util\7Zip\readme.txt"
            @"Functions\Util\BitLocker\DriverTool Disable BitLocker Protection.cmd"
            @"Functions\Util\BitLocker\DriverTool Resume BitLocker Protection.cmd"
            @"Functions\Util\BitLocker\DriverTool Resume BitLocker Protection.xml"
            @"Functions\Util\DriverTool.Util\DriverTool.Util.dll"
            @"Functions\Util\DriverTool.Util\DriverTool.Util.pdb"
            @"Functions\Util\DriverTool.Util\FSharp.Core.dll"
            @"Functions\Util\DriverTool.Util\System.ValueTuple.dll"
            @"Functions\Util\INIFileParser\INIFileParser.dll"
            @"Functions\Util\Log4Net\log4net.dll"
            @"Functions\Util\Log4Net\Log4NetCMTrace.dll"
            @"Functions\Util\Log4Net\Log4NetCMTrace.dll.config"
        |]
    
    [<Test>]    
    let extractPackageTemplateTest () =
        let getFileCount (destintionFolderPath:Path) =
            System.IO.Directory.GetFiles(destintionFolderPath.Value,"*.*",System.IO.SearchOption.AllDirectories).Length
        
        result{
            let! destinationFolderPath = Path.create (System.IO.Path.Combine(System.IO.Path.GetTempPath(),"extractPackageTemplateTest"))
            DriverTool.DirectoryOperations.deleteDirectory true destinationFolderPath
            let! existingDestinationFolderPath = DirectoryOperations.ensureDirectoryExists (destinationFolderPath, true)
            let! extractedFiles = DriverTool.CreateDriverPackage.extractPackageTemplate existingDestinationFolderPath
            let expectedFileCount = expectedPackageTemplateFiles.Length
            let actualFileCount = getFileCount existingDestinationFolderPath
            
            expectedPackageTemplateFiles
            |>Seq.map (fun f -> 
                         let file = System.IO.Path.Combine(existingDestinationFolderPath.Value,f)
                         Assert.IsTrue(System.IO.File.Exists(file),"Extracted file does not exist:" + file)
                         
                        )
            |>Seq.toArray |> ignore

            
            Assert.AreEqual(expectedFileCount,actualFileCount,"Number of files in extracted package template is not expected")
            return extractedFiles
        } |> ignore

        
         