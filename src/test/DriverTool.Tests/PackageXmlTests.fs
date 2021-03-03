namespace DriverTool.Tests


open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module PackageXmlTests=
    open System.Runtime.InteropServices
    open DriverTool.Library.PackageXml

    [<Test>]
    [<TestCase("SomeCategory","2019-08-09","SomeCategory_2019-08-09")>]
    [<TestCase("Some/Category","2019-08-09","Some_Category_2019-08-09")>]
    [<TestCase("Some\\Category","2019-08-09","Some_Category_2019-08-09")>]
    let getPackageFolderNameTests(category,releaseDate,expectedPackageFolderName) =
        let actual = getPackageFolderName category releaseDate
        Assert.AreEqual(expectedPackageFolderName,actual)
        ()
    
