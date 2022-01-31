namespace DriverTool.Tests


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