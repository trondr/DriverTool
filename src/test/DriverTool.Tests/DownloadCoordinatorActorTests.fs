namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
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

    let getTestDownloadCoordinatorContext =
        let destinationFile = FileSystem.pathUnSafe @"c:\temp\dummy.txt"
        let downloadJob = getTestWebFileDownload @"c:\temp" "dummy.txt"
        let package = getTestPackage ("dummy.exe","dummy.txt","","","","")
        let downloads = [(downloadJob.Destination.DestinationFile,[package])] |>Map.ofSeq
        let downloadCoordinatorContext = {PackageDownloads=downloads}
        downloadCoordinatorContext

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ``notAllreadyDownloading DownloadJob allready exists, return false`` () =        
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let existingDestinationFile = (downloadCoordinatorContext.PackageDownloads |>Seq.head).Key
        let actual = notAllreadyDownloading downloadCoordinatorContext existingDestinationFile
        Assert.IsFalse(actual)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ``notAllreadyDownloading DownloadJob does not allready exists, return false`` () =
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let newDownloadJob = getTestWebFileDownload @"c:\temp" "dummy_new.txt"
        let actual = notAllreadyDownloading downloadCoordinatorContext newDownloadJob.Destination.DestinationFile
        Assert.IsTrue(actual)

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ``updateDownloadCoordinatorContext Download job allready exist so do not update the context`` () =
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let existingDestinationFile = (downloadCoordinatorContext.PackageDownloads |>Seq.head).Key
        let existingPackage = (downloadCoordinatorContext.PackageDownloads |>Seq.head).Value.Head
        let actual = updateDownloadCoordinatorContext downloadCoordinatorContext existingPackage existingDestinationFile
        Assert.AreEqual(downloadCoordinatorContext,actual)
        ()

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ``updateDownloadCoordinatorContext Download job and package allready exist so do not update the context`` () =
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let existingDestinationFile = (downloadCoordinatorContext.PackageDownloads |>Seq.head).Key
        let existingPackage = (downloadCoordinatorContext.PackageDownloads |>Seq.head).Value.Head
        let actual = updateDownloadCoordinatorContext downloadCoordinatorContext existingPackage existingDestinationFile
        Assert.AreEqual(downloadCoordinatorContext,actual)
        let actual2 = updateDownloadCoordinatorContext downloadCoordinatorContext existingPackage existingDestinationFile
        Assert.AreEqual(downloadCoordinatorContext,actual2)
        ()

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ``updateDownloadCoordinatorContext Download job exist and package is new update the context`` () =
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let existingDestinationFile = (downloadCoordinatorContext.PackageDownloads |>Seq.head).Key
        let newPackage = getTestPackage ("dummy_new.exe","dummy.txt","","","","")
        let actual = updateDownloadCoordinatorContext downloadCoordinatorContext newPackage existingDestinationFile
        Assert.AreNotEqual(downloadCoordinatorContext,actual)
        let actual2 = updateDownloadCoordinatorContext downloadCoordinatorContext newPackage existingDestinationFile
        Assert.AreEqual(actual,actual2)
        ()

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ``updateDownloadCoordinatorContext Download job is new so update the context`` () =
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let newDownloadJob = getTestWebFileDownload @"c:\temp" "dummt_new.txt"
        let newpackage = getTestPackage ("dummy_new.exe","dummy_new.txt","","","","")
        let actual = updateDownloadCoordinatorContext downloadCoordinatorContext newpackage newDownloadJob.Destination.DestinationFile
        Assert.AreNotEqual(downloadCoordinatorContext,actual)
        ()



       
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
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
    [<Category(TestCategory.UnitTests)>]
    let ``packageToUniqueDownloadJob only installer must be downloaded since readme file is allready downloading, return one download job`` () =
        
        let package = getTestPackage ("dummy.exe","dummy.txt","","","","")
        let destinationFolderPath = FileSystem.pathUnSafe @"c:\temp"
        let downloadCoordinatorContext = getTestDownloadCoordinatorContext
        let actual = packageToUniqueDownloadJob downloadCoordinatorContext destinationFolderPath package        
        let expectedDownloadJob2 = getTestWebFileDownload @"c:\temp" "dummy.exe"        
        Assert.AreEqual(1,Array.length actual,"Array length is not 1")
        Assert.IsTrue(actual |> Array.exists (fun d -> d = expectedDownloadJob2) )


    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let ``updateDownloadsCoordinatorContext`` () =
        let testCacheFolderPath = FileSystem.pathUnSafe @"c:\temp"
        let packages = 
            [
                (getTestPackage ("dummy1.exe","dummy1.txt","","","",""));
                (getTestPackage ("dummy2.exe","dummy2.txt","","","",""));
            ] 
            |> List.toSeq
            |> Seq.map(fun p -> 
                    let webDownloads = (packageInfoToWebFileDownloads testCacheFolderPath p)
                    (webDownloads,p)
                )
            //|> Seq.concat
            //|> List.ofSeq
            

        ()