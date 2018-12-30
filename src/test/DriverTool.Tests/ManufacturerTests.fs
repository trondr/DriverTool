namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module ManufacturerTests =
    open DriverTool.ManufacturerTypes
    open System

    [<Test>]
    [<TestCase("Dell","Acer",false,true,"Dell \"Dell\"","")>]
    [<TestCase("Lenovo","Acer",false,true,"Lenovo \"Lenovo\"","")>]
    [<TestCase("HP","Acer",false,false,"HP \"HP\"","Manufacturer 'HP' is not supported. Supported manufacturers: Dell|Lenovo.")>]
    [<TestCase("Microsoft","Acer",false,false,"Microsoft \"Microsoft\"","Manufacturer 'Microsoft' is not supported. Supported manufacturers: Dell|Lenovo.")>]
    [<TestCase("","Dell",true,true,"Dell \"Dell\"","")>]
    [<TestCase("","",true,false,"Dell \"Dell\"","Current system manufacturer was empty. Supported manufacturers: Dell|Lenovo.")>]
    let manufacturerStringToManufacturerBaseTests (manufacturerString:string,currentManufacturerString:string,defaultToLocal:bool, isSuccess:bool,expectedTypeName:string,expectedError) =
        
        let getCurrentManufacturerStub () =
            match currentManufacturerString with
            |cm when System.String.IsNullOrWhiteSpace(cm) -> Result.Error (new InvalidManufacturerException("Current system manufacturer was empty."):>Exception)
            |_ -> Result.Ok currentManufacturerString

        let actual = manufacturerStringToManufacturerBase (getCurrentManufacturerStub,manufacturerString,defaultToLocal)
        match actual with
        |Result.Ok m -> 
            Assert.IsTrue(isSuccess,"Did not expect success.")
            Assert.AreEqual(expectedTypeName,m.ToString())
        |Result.Error ex ->
            Assert.AreEqual(expectedError,ex.Message)
            Assert.IsFalse(isSuccess,"Failure not expected")