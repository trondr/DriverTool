namespace DriverTool.Tests
open DriverTool.Util.FSharp
open NUnit.Framework

[<TestFixture>]
module OperatingSystemTests =

    [<Test>]
    [<TestCase(8,"AMD64",true,TestName="isOperatingSystemX64PlainTests - 64 bits process")>]
    [<TestCase(4,"AMD64",true,TestName="isOperatingSystemX64PlainTests - 32 bits process")>]
    [<TestCase(4,"",false,TestName="isOperatingSystemX64PlainTests - 32 bits process - 32 Bits OS")>]
    let isOperatingSystemX64PlainTests (intPtrSize:int, processorArchitecture:string, expectedIsOperatingSystemX64:bool) =
        let actual = 
            OperatingSystem.isOperatingSystemX64Plain intPtrSize processorArchitecture
        Assert.AreEqual(expectedIsOperatingSystemX64,actual)
