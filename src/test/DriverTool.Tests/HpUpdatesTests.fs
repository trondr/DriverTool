namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.IntegrationTests)>]
module HpUpdatesTests =
    open DriverTool
    open System
    open Init
    open DriverTool
    
    [<Test>]
    [<TestCase("WIN10X64","83B3")>]
    let downloadSccmDriverPackageTest (operatingSystemCodeString:string,modelCodeString:string) =
        match(result
                {

                    let! operatingSystemCode = (OperatingSystemCode.create operatingSystemCodeString false)
                    let! modelCode = (ModelCode.create modelCodeString false)
                    let! sccmDriverPackageInfo = HpUpdates.getSccmDriverPackageInfo (modelCode,operatingSystemCode)                
                    let cacheDirectory =   Configuration.getDownloadCacheDirectoryPath
                                        
                    let! actual = HpUpdates.downloadSccmPackage (cacheDirectory,sccmDriverPackageInfo)
                    Assert.IsFalse(String.IsNullOrWhiteSpace(actual.InstallerPath), "InstallerPath is empty")
                    
                    return actual
                }) with
         |Ok _ -> Assert.IsTrue(true)
         |Error e -> Assert.Fail(e.Message)
