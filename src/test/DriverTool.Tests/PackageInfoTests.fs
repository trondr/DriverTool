﻿namespace DriverTool.Tests
open NUnit.Framework
open DriverTool.Library.PackageXml

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module PackageInfoTests  =
    open System
    open DriverTool.Library

    [<Test>]
    let packageInfosToDownloadJobs_Duplicate_Readme() =        
        let packageInfos =
            seq{
                
                yield { 
                    Name = "Package1Name";
                    Title = "Package1Title";
                    Version = "1.0.0.0";
                    Installer={Url=toOptionalUri "http://some.company.com/XXXXX.exe" "";Name="XXXXX.exe";Checksum="XXXXXXXXXXXXX";Size=395L;Type=Installer};
                    Readme={Url=toOptionalUri "http://some.company.com/XXXXX.txt" "";Name="XXXXX.txt";Checksum="";Size=24L;Type=Readme};                
                    ExtractCommandLine = "";InstallCommandLine = "";Category = "";ReleaseDate = DateTime.Now.ToString();PackageXmlName="xxx";ExternalFiles = None}
                yield { 
                    Name = "Package2Name";
                    Title = "Package2Title";Version = "1.0.0.0";                
                    Installer={Url=toOptionalUri "http://some.company.com/YYYYY.exe" "";Name="YYYYY.exe";Checksum="XXXXXXXXXXXXX";Size=395L;Type=Installer};
                    Readme={Url=toOptionalUri "http://some.company.com/YYYYY.txt" "";Name="XXXXX.txt";Checksum="";Size=24L;Type=Readme};                                
                    ExtractCommandLine = "";InstallCommandLine = "";Category = "";ReleaseDate = DateTime.Now.ToString();PackageXmlName="yyy";ExternalFiles = None}

            }
        
        let actual = 
            packageInfosToDownloadJobs (FileSystem.pathUnSafe @"c:\temp") packageInfos
            |>Seq.toArray
        Assert.AreEqual(3,actual.Length,"Number of download jobs was not expected")

    [<Test>]
    let packageInfosToDownloadJobs_Unique_Readme() =        
        let packageInfos =
            seq{
                yield { Name = "Package1Name";Title = "Package1Title";Version = "1.0.0.0";Installer={Url=toOptionalUri "http://some.company.com/XXXXX.exe" "";Name="XXXXX.exe";Checksum="XXXXXXXXXXXXX";Size=395L;Type=Installer};Readme={Url=toOptionalUri "http://some.company.com/XXXXX.txt" "";Name="XXXXX.txt";Checksum="";Size=24L;Type=Readme};ExtractCommandLine = "";InstallCommandLine = "";Category = "";ReleaseDate = DateTime.Now.ToString();PackageXmlName="xxx";ExternalFiles = None}
                yield { Name = "Package2Name";Title = "Package2Title";Version = "1.0.0.0";Installer={Url=toOptionalUri "http://some.company.com/YYYYY.exe" "";Name="YYYYY.exe";Checksum="XXXXXXXXXXXXX";Size=395L;Type=Installer};
                    Readme={Url=toOptionalUri "http://some.company.com/YYYYY.txt" "";Name="YYYYY.txt";Checksum="";Size=24L;Type=Readme};ExtractCommandLine = "";InstallCommandLine = "";Category = "";ReleaseDate = DateTime.Now.ToString();PackageXmlName="yyy";ExternalFiles = None}
            }
        
        let actual = 
            packageInfosToDownloadJobs (FileSystem.pathUnSafe @"c:\temp") packageInfos
            |>Seq.toArray
        Assert.AreEqual(4,actual.Length,"Number of download jobs was not expected")
        