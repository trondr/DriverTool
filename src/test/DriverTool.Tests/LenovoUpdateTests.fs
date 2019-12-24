namespace DriverTool.Tests

module LenovoUpdateTests =
    open DriverTool.Tests.Init
    open DriverTool    
    open DriverTool.PackageXml
    open NUnit.Framework    
    let logger = Common.Logging.Simple.ConsoleOutLogger("LenovoUpdateTests",Common.Logging.LogLevel.All,true,true,true,"yyyy-MM-dd-HH-mm-ss-ms")
    open DriverTool.Library.F
    
    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    [<TestCase("nz3gs05w.exe","nz3gs05w.txt","nz3gs05w_2_.xml",
        "ISDAS_NZ3GS",
        "Intel® SGX Device and Software (Windows 10 Version 1709 or later) - 10 [64]",
        "2.3.100.49777"
        
        
        
        )>]
    let extractUpdateTest (installerFileName,readmeFileName,packageXmlFileName,
                            packageName,
                            packageTitle,
                            packageVersion
                            ) =
        match(result{
            let! isAdministrator = DriverTool.Requirements.assertIsAdministrator "Administrative privileges are required to run this integration test."
            use temporaryCacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! temproaryCacheFolderPath = temporaryCacheFolder.FolderPath                        
            logger.Info(sprintf "Extract from embeded resource: %s, %s, %s" installerFileName readmeFileName packageXmlFileName)
            let! installerFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (installerFileName,temproaryCacheFolderPath,installerFileName,typeof<ThisTestAssembly>.Assembly)
            let! readmeFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (readmeFileName,temproaryCacheFolderPath,readmeFileName,typeof<ThisTestAssembly>.Assembly)
            let! packageXmlFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (packageXmlFileName,temproaryCacheFolderPath,packageXmlFileName,typeof<ThisTestAssembly>.Assembly)
            logger.Info(sprintf "Construct downloaded package info...")
            let downloadedPackageInfo: DownloadedPackageInfo =
                {
                    InstallerPath = FileSystem.pathValue installerFilePath
                    ReadmePath = FileSystem.pathValue readmeFilePath
                    PackageXmlPath = FileSystem.pathValue packageXmlFilePath
                    Package =
                        {
                            Name = packageName
                            Title = packageTitle
                            Version = packageVersion
                            Installer =
                                {
                                    Url = toOptionalUri "http://someurl" installerFileName
                                    Name = installerFileName
                                    Checksum = "C872A0F1A3159C68B811F31C841153D22E304550D815EDA6464C706247EB7658"
                                    Size = 2780688L
                                    Type = PackageFileType.Installer
                                }
                            ExtractCommandLine = "nz3gs05w.exe /VERYSILENT /DIR=%PACKAGEPATH% /EXTRACT=\"YES\""
                            InstallCommandLine = "%PACKAGEPATH%\\nz3gs05w.exe /verysilent /DIR=%PACKAGEPATH%\\TMP"
                            Category = "SomeCategory"
                            Readme =
                                {
                                    Url = toOptionalUri "http://someurl" readmeFileName
                                    Name = readmeFileName
                                    Checksum = "E6A73AA8DC369C5D16B0F24EB0438FF41305E68E4D91CCB406EF9E5C5FCAC181"
                                    Size = 14275L
                                    Type = PackageFileType.Readme
                                }
                            ReleaseDate = "2019-08-15"
                            PackageXmlName = packageXmlFileName
                        }
                }
            logger.Info("Create package folder path...")
            use temporaryPackageFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! temporaryPackageFolderPath = temporaryPackageFolder.FolderPath
            let! extractedPackageInfo = PackageXml.extractInstaller (downloadedPackageInfo, temporaryPackageFolderPath)
            
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(FileSystem.pathValue temporaryPackageFolderPath,installerFileName)),sprintf "Installer '%s' is not copied to package folder." installerFileName)

            return extractedPackageInfo
        })with
        |Result.Ok v -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)

    [<Test>]
    [<Category(TestCategory.IntegrationTests)>]
    let loadPackagesXmlTest () =
        match(result{
            use temporaryFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! temporaryFolderPath = temporaryFolder.FolderPath
            let! xmlFilePath = DriverTool.EmbeddedResource.extractEmbeddedResourceByFileNameBase ("LenovoCatalog_WithError_20QG_win10.xml",temporaryFolderPath,"LenovoCatalog_WithError_20QG_win10.xml",typeof<ThisTestAssembly>.Assembly)
            let! packages = DriverTool.LenovoUpdates.loadPackagesXml xmlFilePath 
            let! downloadedPackages = DriverTool.LenovoUpdates.downloadPackageXmls temporaryFolderPath packages
            let! packageInfos = 
                (DriverTool.LenovoUpdates.parsePackageXmls downloadedPackages)
                |>toAccumulatedResult
            return packageInfos
        })with
        |Result.Ok v -> Assert.IsTrue(false,"Did not fail as expected.")
        |Result.Error ex -> Assert.AreEqual("Failed to download all package infos due to the following 1 error messages:\r\nUri 'https://download.lenovo.com/pccbbs/mobiles/n2hwe01w.txt' does not represent a xml file.",ex.Message)        


    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let getRemoteUpdatesTests () =
        match(result{
            let! cacheFolderPath = FileSystem.path @"C:\Temp\DriverToolCache"
            let! existingCacheFolderPath = DirectoryOperations.ensureDirectoryExists true cacheFolderPath
            let! model = ModelCode.create "20QG" false
            let! osCode = OperatingSystemCode.create "WIN10X64" false
            let! logDirectory = FileSystem.path @"C:\Temp\DriverToolLogs"
            let context = DriverTool.UpdatesContext.toUpdatesRetrievalContext model osCode false logDirectory [||]                                
            let! packageInfos = DriverTool.LenovoUpdates.getRemoteUpdates logger existingCacheFolderPath context
            return packageInfos        
        })with
        |Result.Ok v -> Assert.IsTrue(true)
        |Result.Error ex -> Assert.Fail(ex.Message)