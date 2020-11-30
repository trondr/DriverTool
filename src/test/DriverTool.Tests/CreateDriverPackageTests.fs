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
            ExternalFiles = None
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
        
        