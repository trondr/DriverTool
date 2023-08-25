namespace DriverTool.Tests


open System
open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module PackageXmlTests=
    open System.Runtime.InteropServices
    open DriverTool.Library.PackageXml
    open DriverTool.Library.DriverPack

    [<Test>]
    [<TestCase("SomeCategory","2019-08-09","SomeCategory_2019-08-09")>]
    [<TestCase("Some/Category","2019-08-09","Some_Category_2019-08-09")>]
    [<TestCase("Some\\Category","2019-08-09","Some_Category_2019-08-09")>]
    [<TestCase("Motherboard Devices core chipset onboard video PCIe switches","2019-08-09","Motherboard Devices_2019-08-09")>]
    [<TestCase("Motherboard Devices core chipset onboard video PCIe","2019-08-09","Motherboard Devices_2019-08-09")>]
    [<TestCase("Motherboard Devices core chipset onboard video","2019-08-09","Motherboard Devices_2019-08-09")>]
    [<TestCase("Motherboard Devices core chipset onboard","2019-08-09","Motherboard Devices_2019-08-09")>]
    [<TestCase("Motherboard Devices core chipset","2019-08-09","Motherboard Devices_2019-08-09")>]
    [<TestCase("Motherboard Devices core","2019-08-09","Motherboard Devices_2019-08-09")>]
    [<TestCase("Motherboard Devices","2019-08-09","Motherboard Devices_2019-08-09")>]
    [<TestCase("Motherboard","2019-08-09","Motherboard_2019-08-09")>]
    let getPackageFolderNameTests(category,releaseDate,expectedPackageFolderName) =
        let actual = getPackageFolderName category releaseDate
        Assert.AreEqual(expectedPackageFolderName,actual)
        ()
    

    [<Test>]
    let toModelCodesWqlQueryTest() =
        let name = "ThinkPad X1 Yoga 4th Gen Type 20QF 20QG"
        let manufacturer = DriverTool.Library.ManufacturerTypes.toManufacturer "Lenovo"
        let expected = {
                Name = name
                NameSpace = "root\\cimv2"
                Query = "select Model from Win32_ComputerSystem where (Model like '20QF%') or (Model like '20QG%')"
            }
        let actual = toModelCodesWqlQuery name  manufacturer [|"20QF";"20QG"|]
        Assert.AreEqual(expected,actual)
        ()

    [<Test>]
    let toManufacturerWqlQueryTest() =
        let name = "Lenovo"
        let manufacturer = DriverTool.Library.ManufacturerTypes.toManufacturer "Lenovo"
        let expected = {
                Name = name
                NameSpace = "root\\cimv2"
                Query = "select Manufacturer from Win32_ComputerSystem where (Manufacturer like '%Lenovo%')"
            }
        let actual = toManufacturerWqlQuery manufacturer
        Assert.AreEqual(expected,actual)
        ()

    [<Test>]
    [<TestCase("Motherboard Devices core chipset onboard video PCIe switches","Motherboard Devices")>]
    [<TestCase("Motherboard Devices core chipset onboard video PCIe","Motherboard Devices")>]
    [<TestCase("Motherboard Devices core chipset onboard video","Motherboard Devices")>]
    [<TestCase("Motherboard Devices core chipset onboard","Motherboard Devices")>]
    [<TestCase("Motherboard Devices core chipset","Motherboard Devices")>]
    [<TestCase("Motherboard Devices core","Motherboard Devices")>]
    [<TestCase("Motherboard Devices","Motherboard Devices")>]
    [<TestCase("Motherboard","Motherboard")>]
    [<TestCase("Software and Utilities","Software Utilities")>]
    let truncateCategoryTests (category,expected) =        
        let actual = truncateCategory category
        Assert.AreEqual(expected,actual)
        ()
        
    
    [<Test>]
    let getLatestPackageInfosTest() =
        let packageInfo1 = {PackageInfo.Default with Name="Test";Version="1.0.1" }
        let packageInfo2 = {PackageInfo.Default with Name="Test";Version="1.0.2" }
        let packageInfo3 = {PackageInfo.Default with Name="Test2";Version="1.0.3" }
        let packageInfo4 = {PackageInfo.Default with Name="Test2";Version="1.0.2" }
        let packageInfo5 = {PackageInfo.Default with Name="Test2";Version="1.0.1" }
        
        let packageInfos = [|packageInfo1;packageInfo2;packageInfo3;packageInfo4;packageInfo5|]
        let expectedPackageInfos = [|packageInfo2;packageInfo3|]
        let actualPackageInfos = packageInfos |> getLatestPackageInfos
        Assert.AreEqual(2,actualPackageInfos.Length,"Length is not expected")
        Assert.AreEqual(expectedPackageInfos.[0],actualPackageInfos.[0])
        Assert.AreEqual(expectedPackageInfos.[1],actualPackageInfos.[1])
        ()
      
    let expected_nz3gs05w_PackageInfo = {
        Name = "ISDAS_NZ3GS"
        Title = "Intel® SGX Device and Software (Windows 10 Version 1709 or later) - 10 [64]"
        Version = "2.3.100.49777"
        Installer =
           { Url = None
             Name = "nz3gs05w.exe"
             Checksum =
              "C872A0F1A3159C68B811F31C841153D22E304550D815EDA6464C706247EB7658"
             Size = 2780688L
             Type = Installer }
        ExtractCommandLine = "nz3gs05w.exe /VERYSILENT /DIR=%PACKAGEPATH% /EXTRACT=\"YES\""
        InstallCommandLine = "%PACKAGEPATH%\\nz3gs05w.exe /verysilent /DIR=%PACKAGEPATH%\\TMP"
        Category = ""
        Readme =
           { Url = None
             Name = "nz3gs05w.txt"
             Checksum =
              "E6A73AA8DC369C5D16B0F24EB0438FF41305E68E4D91CCB406EF9E5C5FCAC181"
             Size = 14275L
             Type = Readme }
        ReleaseDate = "2019-08-15"
        PackageXmlName = "nz3gs05w_2_.xml"
        ExternalFiles =
           Some
             [|{ Url = None
                 Name = "getw10ver6.exe"
                 Checksum =
                  "D983A6376977C6B578D05232FEE0BDFA2C66538FDB2B35CA41694A900A2DEB6A"
                 Size = 159048L
                 Type = External }|] }

      
    open DriverTool.Library.F
    open DriverTool.Library
    [<Test>]
    [<TestCase("nz3gs05w_2_.xml",true,false,"")>]
    [<TestCase("nz3gs05w_2_Empty_Corrupt.xml",false,false,"Root element is missing.")>]
    [<TestCase("nz3gs05w_2_Missing_Installer_Element.xml",false,false,"Missing installer element.")>]
    [<TestCase("nz3gs05w_2_.xml",false,true,"Could not find file")>]
    let getPackageInfoUnsafeTests (fileName:string,success:bool,removeFileBeforeTest:bool,expectedErrorMessage:string) =
        match (result{
            use cacheFolder = new DirectoryOperations.TemporaryFolder(logger)
            let! cacheFolderPath = cacheFolder.FolderPath                        
            let! destinationFilePath = EmbeddedResource.extractEmbeddedResourceByFileNameBase (fileName,cacheFolderPath,fileName,System.Reflection.Assembly.GetExecutingAssembly())
            let! existingDestinationFilePath = FileSystem.existingFilePath destinationFilePath
            let! adjustedDestinationFilePath =
                if(removeFileBeforeTest) then
                    FileOperations.deleteFile existingDestinationFilePath
                else
                    Result.Ok existingDestinationFilePath                
            let downloadedPackageXmlInfo = {Location=""; Category="";FilePath=adjustedDestinationFilePath;BaseUrl="";CheckSum=""}            
            let! actual = getPackageInfoSafe downloadedPackageXmlInfo
            return actual
        }) with        
        |Result.Ok a ->
            Assert.IsTrue(success)
            Assert.AreEqual(expected_nz3gs05w_PackageInfo,a,"Loaded package info is not expected.")
            printfn $"%A{a}"
            ()
        |Result.Error e ->
            Assert.IsFalse(success,$"Test was expected to succeed. Exception: %s{e.ToString()}")
            Assert.IsTrue(e.Message.Contains(expectedErrorMessage),$"Expected error message: %s{expectedErrorMessage}. Actual error message %s{e.Message}")
        ()
