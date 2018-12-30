namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module ManufacturerTests =
    open DriverTool.ManufacturerTypes

    [<Test>]
    [<TestCase("Dell",true,"Dell \"Dell\"")>]
    [<TestCase("Lenovo",true,"Lenovo \"Lenovo\"")>]
    [<TestCase("HP",false,"HP \"HP\"")>]
    [<TestCase("Microsoft",false,"Microsoft \"Microsoft\"")>]
    let manufacturerStringToManufacturerTest (manufacturerString:string, isSuccess:bool,expectedTypeName:string) =
        
        let actual = manufacturerStringToManufacturer manufacturerString
        match actual with
        |Result.Ok m -> 
            Assert.IsTrue(isSuccess)
            Assert.AreEqual(expectedTypeName,m.ToString())
        |Result.Error ex ->
            Assert.AreEqual(sprintf "Manufacturer '%s' is not supported. Supported manufacturers: Dell|Lenovo." manufacturerString,ex.Message)
            Assert.IsFalse(isSuccess,"Failure not expected")
