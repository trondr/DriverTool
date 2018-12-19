namespace DriverTool.Tests
open NUnit.Framework
open DriverTool.PackageXml

[<TestFixture>]
module PackageInfoTests  =
    open System

    [<Test>]
    let packageInfosToDownloadJobs_Duplicate_Readme() =        
        let packageInfos =
            seq{
                yield { Name = "Package1Name";Title = "Package1Title";Version = "1.0.0.0";InstallerName = "XXXXX.exe";InstallerCrc = "XXXXXXXXXXXXX";InstallerSize = 395L;BaseUrl = "http://some.company.com";ReadmeName = "XXXXX.txt";ReadmeCrc = "";ReadmeSize = 24L;ExtractCommandLine = "";InstallCommandLine = "";Category = "";ReleaseDate = DateTime.Now.ToString();PackageXmlName="xxx"}
                yield { Name = "Package2Name";Title = "Package2Title";Version = "1.0.0.0";InstallerName = "YYYYY.exe";InstallerCrc = "XXXXXXXXXXXXX";InstallerSize = 395L;BaseUrl = "http://some.company.com";ReadmeName = "XXXXX.txt";ReadmeCrc = "";ReadmeSize = 24L;ExtractCommandLine = "";InstallCommandLine = "";Category = "";ReleaseDate = DateTime.Now.ToString();PackageXmlName="yyy"}
            }
        
        let actual = 
            packageInfosToDownloadJobs @"c:\temp" packageInfos
            |>Seq.toArray
        Assert.AreEqual(3,actual.Length,"Number of download jobs was not expected")

    [<Test>]
    let packageInfosToDownloadJobs_Unique_Readme() =        
        let packageInfos =
            seq{
                yield { Name = "Package1Name";Title = "Package1Title";Version = "1.0.0.0";InstallerName = "XXXXX.exe";InstallerCrc = "XXXXXXXXXXXXX";InstallerSize = 395L;BaseUrl = "http://some.company.com";ReadmeName = "XXXXX.txt";ReadmeCrc = "";ReadmeSize = 24L;ExtractCommandLine = "";InstallCommandLine = "";Category = "";ReleaseDate = DateTime.Now.ToString();PackageXmlName="xxx"}
                yield { Name = "Package2Name";Title = "Package2Title";Version = "1.0.0.0";InstallerName = "YYYYY.exe";InstallerCrc = "XXXXXXXXXXXXX";InstallerSize = 395L;BaseUrl = "http://some.company.com";ReadmeName = "YYYYY.txt";ReadmeCrc = "";ReadmeSize = 24L;ExtractCommandLine = "";InstallCommandLine = "";Category = "";ReleaseDate = DateTime.Now.ToString();PackageXmlName="yyy"}
            }
        
        let actual = 
            packageInfosToDownloadJobs @"c:\temp" packageInfos
            |>Seq.toArray
        Assert.AreEqual(4,actual.Length,"Number of download jobs was not expected")