namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module ManufacturerTests =    
    open DriverTool.Library.ManufacturerTypes
    open System
    open DriverTool.Library.F

    [<Test>]
    [<TestCase("Dell","Acer",false,true,"Dell \"Dell\"","")>]
    [<TestCase("Dell Inc.","Acer",false,true,"Dell \"Dell\"","")>]

    [<TestCase("Lenovo","Acer",false,true,"Lenovo \"Lenovo\"","")>]
    [<TestCase("Lenovo Inc.","Acer",false,true,"Lenovo \"Lenovo\"","")>]


    [<TestCase("HP","Acer",false,true,"HP \"HP\"","")>]
    [<TestCase("Microsoft","Acer",false,false,"Microsoft \"Microsoft\"","Manufacturer 'Microsoft' is not supported. Supported manufacturers: Dell|Lenovo|HP.")>]
    [<TestCase("","Dell",true,true,"Dell \"Dell\"","")>]
    [<TestCase("","",true,false,"Dell \"Dell\"","Current system manufacturer was empty. Supported manufacturers: Dell|Lenovo|HP.")>]
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

    
    [<Test>]
    [<TestCase("Dell Inc.",true,true,"No error",TestName = "Dell Supported.")>]
    [<TestCase("Dell Inc.",false,false,"Failed to get Manufacturer from WMI.",TestName = "Dell Wmi failed.")>]
    [<TestCase("LENOVO",true,true,"No error",TestName = "Lenovo Supported.")>]
    [<TestCase("LENOVO",false,false,"Failed to get Manufacturer from WMI.",TestName = "Lenovo Wmi failed.")>]
    [<TestCase("HP",true,true,"No error",TestName = "HP Supported.")>]
    [<TestCase("HP",false,false,"Failed to get Manufacturer from WMI.",TestName = "HP Wmi failed.")>]
    [<TestCase("Acer Inc.",true,false,"Manufacturer 'Acer Inc.' is not supported. Supported manufacturers: Dell|Lenovo|HP.",TestName = "Not supported.")>]
    let getManufacturerForCurrentSystemBaseTest (wmiManufacturer:string,wmiSucceded:bool, expectedIsSupported:bool, expectedErrorMessage:string) =
        match (result{
            
                    let wmiManufacturerFuncStub () =
                        match wmiSucceded with
                        |true -> Result.Ok wmiManufacturer
                        |false-> Result.Error (new Exception("Failed to get Manufacturer from WMI."))

                    let! actual = DriverTool.Library.ManufacturerTypes.manufacturerStringToManufacturerBase ( wmiManufacturerFuncStub,wmiManufacturer,true)
            return actual
        }) with
        |Ok _-> Assert.IsTrue(true)
        |Error ex ->
            match expectedIsSupported with
            |true -> Assert.Fail(ex.Message)
            |false -> 
                Assert.AreEqual(expectedErrorMessage,ex.Message)