namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module DownloadCoordinatorActorTests =
    open DriverTool.DownloadCoordinatorActor
    open DriverTool.Library.Web
    open DriverTool.Library.PackageXml
    open DriverTool.Library
    open DriverTool.Library.Messages
    open DriverTool.Library.WebDownload
    open DriverTool.Library.F

    let getTestWebFileDownload destinationFolder fileName =
        let destinationFile = FileSystem.pathUnSafe (System.IO.Path.Combine(destinationFolder, fileName))        
        let webFileDownload = resultToValueUnsafe (toWebFileDownload (sprintf "http://dummy/%s" fileName) "some checksum" 0L destinationFile)
        webFileDownload

    let getTestDownloadCoordinatorContext =
        let destinationFile = FileSystem.pathUnSafe @"c:\temp\dummy.txt"
        let downloadJob = getTestWebFileDownload @"c:\temp" "dummy.txt"
        let downloads = [(downloadJob.Destination.DestinationFile,downloadJob)] |>Map.ofSeq
        let downloadCoordinatorContext = {Downloads=downloads}
        downloadCoordinatorContext

    [<Test>]
    let ``notAllreadyDownloading DownloadJob allready exists, return false`` () =        
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let existingDownloadJob = (downloadCoordinatorContext.Downloads |>Seq.head).Value
        let actual = notAllreadyDownloading downloadCoordinatorContext existingDownloadJob
        Assert.IsFalse(actual)

    [<Test>]
    let ``notAllreadyDownloading DownloadJob does not allready exists, return false`` () =
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let newDownloadJob = getTestWebFileDownload @"c:\temp" "dummy_new.txt"
        let actual = notAllreadyDownloading downloadCoordinatorContext newDownloadJob
        Assert.IsTrue(actual)

    [<Test>]
    let ``updateDownloadCoordinatorContext Download job allready exist so do not update the context`` () =
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let existingDownloadJob = (downloadCoordinatorContext.Downloads |>Seq.head).Value
        let actual = updateDownloadCoordinatorContext downloadCoordinatorContext existingDownloadJob
        Assert.AreEqual(downloadCoordinatorContext,actual)
        ()

    [<Test>]
    let ``updateDownloadCoordinatorContext Download job is new so update the context`` () =
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let newDownloadJob = getTestWebFileDownload @"c:\temp" "dummt_new.txt"
        let actual = updateDownloadCoordinatorContext downloadCoordinatorContext newDownloadJob
        Assert.AreNotEqual(downloadCoordinatorContext,actual)
        ()

    let getTestPackage (installerFileName,readmeFileName,packageXmlFileName,packageName,packageTitle,packageVersion) =
        {
            Name = packageName
            Title = packageTitle
            Version = packageVersion
            Installer =
                {
                    Url = toOptionalUri "http://dummy" installerFileName
                    Name = installerFileName
                    Checksum = "some checksum"
                    Size = 0L
                    Type = PackageFileType.Installer
                }
            ExtractCommandLine = "nz3gs05w.exe /VERYSILENT /DIR=%PACKAGEPATH% /EXTRACT=\"YES\""
            InstallCommandLine = "%PACKAGEPATH%\\nz3gs05w.exe /verysilent /DIR=%PACKAGEPATH%\\TMP"
            Category = "SomeCategory"
            Readme =
                {
                    Url = toOptionalUri "http://dummy" readmeFileName
                    Name = readmeFileName
                    Checksum = "some checksum"
                    Size = 0L
                    Type = PackageFileType.Readme
                }
            ReleaseDate = "2019-08-15"
            PackageXmlName = packageXmlFileName
        }
    
    [<Test>]
    let ``packageToUniqueDownloadJob both installer and readme must be downloaded, return two download jobs`` () =
        
        let package = getTestPackage ("dummy2.exe","dummy2.txt","","","","")
        let destinationFolderPath = FileSystem.pathUnSafe @"c:\temp"
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let actual = packageToUniqueDownloadJob downloadCoordinatorContext destinationFolderPath package
        let expectedDownloadJob1 = getTestWebFileDownload @"c:\temp" "dummy2.txt"
        let expectedDownloadJob2 = getTestWebFileDownload @"c:\temp" "dummy2.exe"
        Assert.AreEqual(2,Array.length actual,"Array length is not 2")
        Assert.IsTrue(actual |> Array.exists (fun d -> d = expectedDownloadJob1) )
        Assert.IsTrue(actual |> Array.exists (fun d -> d = expectedDownloadJob2) )

    [<Test>]
    let ``packageToUniqueDownloadJob only installer must be downloaded since readme file is allready downloading, return one download job`` () =
        
        let package = getTestPackage ("dummy.exe","dummy.txt","","","","")
        let destinationFolderPath = FileSystem.pathUnSafe @"c:\temp"
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let actual = packageToUniqueDownloadJob downloadCoordinatorContext destinationFolderPath package        
        let expectedDownloadJob2 = getTestWebFileDownload @"c:\temp" "dummy.exe"        
        Assert.AreEqual(1,Array.length actual,"Array length is not 1")
        Assert.IsTrue(actual |> Array.exists (fun d -> d = expectedDownloadJob2) )

