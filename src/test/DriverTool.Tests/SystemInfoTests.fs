namespace DriverTool.Tests

open NUnit.Framework

[<TestFixture>]
module SystemInfoTests =
    open DriverTool
    open System
    open DriverTool.SystemInfo
    
    [<Test>]
    [<TestCase("Dell Inc.",true,true,"No error",TestName = "Dell Supported.")>]
    [<TestCase("Dell Inc.",false,false,"Failed to get Manufacturer from WMI.",TestName = "Dell Wmi failed.")>]
    [<TestCase("LENOVO",true,true,"No error",TestName = "Lenovo Supported.")>]
    [<TestCase("LENOVO",false,false,"Failed to get Manufacturer from WMI.",TestName = "Lenovo Wmi failed.")>]
    [<TestCase("Acer Inc.",true,false,"Manufacturer 'Acer Inc.' is not supported. Supported manufacturers: [Dell|Lenovo]",TestName = "Not supported.")>]
    let getManufacturerForCurrentSystemBaseTest (wmiManufacturer:string,wmiSucceded:bool, expectedIsSupported:bool, expectedErrorMessage:string) =
        match (result{
            
                    let wmiManufacturerFuncStub () =
                        match wmiSucceded with
                        |true -> Result.Ok wmiManufacturer
                        |false-> Result.Error (new Exception("Failed to get Manufacturer from WMI."))

                    let! actual = DriverTool.SystemInfo.getManufacturerForCurrentSystemBase wmiManufacturerFuncStub
            return actual
        }) with
        |Ok _-> Assert.IsTrue(true)
        |Error ex ->
            match expectedIsSupported with
            |true -> Assert.Fail(ex.Message)
            |false -> 
                Assert.AreEqual(expectedErrorMessage,ex.Message)