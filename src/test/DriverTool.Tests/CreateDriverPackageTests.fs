namespace DriverTool.Tests

open NUnit.Framework
open DriverTool.Library.PackageXml
open DriverTool.Library.Web
open DriverTool.DownloadActor
open DriverTool
open DriverTool.Library.F
open DriverTool.Library
open DriverTool.Library.ManufacturerTypes
open DriverTool.Packaging
open DriverTool.Library.WebDownload
open DriverTool.Library.Messages

[<TestFixture>]
module CreateDriverPackageTests =
    open System
    
    let defaulPackageInfo = 
        {   
            Name="";
            Title="";
            Version="";
            Installer = 
                {
                    Url = toOptionalUri "http://dummy" ""
                    Name = ""
                    Checksum = ""
                    Size = 0L
                    Type = Installer
                }            
            ExtractCommandLine="";
            InstallCommandLine="";
            Category="";
            Readme =
                {
                    Url = toOptionalUri "http://dummy" ""
                    Name = ""
                    Checksum = ""
                    Size = 0L
                    Type = Readme
                }            
            ReleaseDate="";
            PackageXmlName=""
        }
    let defaultDownloadInfo =
        {
            SourceUri=new Uri("http://some.test.server.com/test.txt");
            SourceChecksum="";
            SourceFileSize=0L;            
            DestinationFile=FileSystem.pathUnSafe @"c:\temp\test.txt";
        }

    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    let packageInfosToDownloadedPackageInfosTests ()=
        
        let installerName1="x12345.exe" 
        let installerName2="x54321.exe" 
        let installerName3="xabcdef.exe" 
        let installerName4="xabcdef.exe"
        let installerName5="somefile_with_invalid_signature_xghijkl.exe"
        
        let packageInfos : array<PackageInfo> = [|
                    {defaulPackageInfo with Installer={defaulPackageInfo.Installer with Name=installerName1}}
                    {defaulPackageInfo with Installer={defaulPackageInfo.Installer with Name=installerName2}}
                    {defaulPackageInfo with Installer={defaulPackageInfo.Installer with Name=installerName3}}
                    {defaulPackageInfo with Installer={defaulPackageInfo.Installer with Name=installerName4}}
                    {defaulPackageInfo with Installer={defaulPackageInfo.Installer with Name=installerName5}}
                    |]

        let downloadInfos = 
            [|
                {defaultDownloadInfo with DestinationFile = FileSystem.pathUnSafe (@"c:\temp\" + installerName1)}
                {defaultDownloadInfo with DestinationFile = FileSystem.pathUnSafe (@"c:\temp\" + installerName2)}
                {defaultDownloadInfo with DestinationFile = FileSystem.pathUnSafe (@"c:\temp\" + installerName3)}
                {defaultDownloadInfo with DestinationFile = FileSystem.pathUnSafe (@"c:\temp\" + installerName4)}
            |]
        ()
        let expectedCount = 4
        let actual = 
            packageInfosToDownloadedPackageInfos (FileSystem.pathUnSafe @"c:\temp\test") packageInfos downloadInfos
            |>Seq.toArray
        let actualCount =
            actual
            |>Seq.toArray
            |>Array.length
        Assert.AreEqual(expectedCount, actualCount, "Count of downloaded package infos")


    [<Test>]
    [<Category(TestCategory.ManualTests)>]
    let downloadUpdateTest () =
        match(result{
            let! webFileSource = toWebFileSource "http://ftp.hp.com/pub/softpaq/sp81501-82000/sp81886.exe" "ec6c692772662540c3d4bc6156ae33a37dd2ed06" 4092824L 
            let webFileDestination = {DestinationFile = (FileSystem.pathUnSafe @"C:\Temp\DriverToolCache\sp81886.exe")}
            return {Source=webFileSource;Destination=webFileDestination}            
        })with
        |Result.Ok wfd -> 
            Assert.IsTrue(true)
            let expected = CreateDriverPackageMessage.DownloadFinished (Some wfd,wfd)
            let actual = DownloadActor.downloadWebFile false wfd
            Assert.AreEqual(expected,actual)
        |Result.Error ex -> Assert.Fail(ex.Message)
    
    [<Test>]
    [<Category(TestCategory.UnitTests)>]
    [<TestCase(false,"setup.exe /s", "Dell", @"c:\temp",true)>]
    [<TestCase(false,"setup.exe /s", "HP", @"c:\temp",true)>]
    [<TestCase(false,"setup.exe /s", "Lenovo", @"c:\temp",true)>]
    let createInstallScriptFileContentTests (packageIsUsingDpInst:bool, installCommandLine:string, manufacturerString:string, logDirectoryString:string, isSuccess:bool) =
        match(result{
            let! manufacturer = manufacturerStringToManufacturer (manufacturerString,false)
            let! logDirectory = FileSystem.path logDirectoryString
            let actual = createInstallScriptFileContent (packageIsUsingDpInst, installCommandLine,manufacturer, logDirectory)            
            Assert.IsTrue(actual.Contains("IF NOT EXIST \"c:\\temp\" md \"c:\\temp\""))
            return actual
        })with
        |Result.Ok _ -> 
            Assert.IsTrue(isSuccess,"Expected fail, but succeeded")
        |Result.Error ex -> 
            Assert.IsFalse(isSuccess,sprintf "Expected success, but failed. %A" ex)
        
        