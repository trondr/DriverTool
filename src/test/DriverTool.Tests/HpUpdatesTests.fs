namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module HpUpdatesTests =
    open DriverTool
    open System
    open Init
    open DriverTool
    open DriverTool
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
         |Error e -> Assert.Fail(String.Format("{0}", e.Message))

    [<Test>]
    [<TestCase("WIN10X64","83B3")>]
    let extractSccmDriverPackageTest (operatingSystemCodeString:string,modelCodeString:string) =
        match(result
                {

                    let! operatingSystemCode = (OperatingSystemCode.create operatingSystemCodeString false)
                    let! modelCode = (ModelCode.create modelCodeString false)
                    let! sccmDriverPackageInfo = HpUpdates.getSccmDriverPackageInfo (modelCode,operatingSystemCode)                
                    let cacheDirectory =   Configuration.getDownloadCacheDirectoryPath             
                    let! downloadedSccmPackageInfo = HpUpdates.downloadSccmPackage (cacheDirectory,sccmDriverPackageInfo)
                    let! destinationFolderPath = PathOperations.combine2Paths (PathOperations.getTempPath,"005 Sccm Package Test")
                    Assert.IsTrue((FileSystem.pathValue destinationFolderPath).EndsWith("\\005 Sccm Package Test"))
                    let deletedDestinationDirectory = DirectoryOperations.deleteDirectory true, FileSystem.pathValue  destinationFolderPath
                    let! existingDestinationPath = DirectoryOperations.ensureDirectoryExistsAndIsEmpty (destinationFolderPath,true)
                    let! actual = HpUpdates.extractSccmPackage (downloadedSccmPackageInfo, existingDestinationPath)
                    Assert.IsFalse(String.IsNullOrWhiteSpace(FileSystem.pathValue  actual), "Destination path is empty")
                    
                    return actual
                }) with
         |Ok _ -> Assert.IsTrue(true)
         |Error e -> Assert.Fail(String.Format("{0}", e.Message))
