namespace DriverTool.Tests
open DriverTool.Util.FSharp
open NUnit.Framework
open System

[<TestFixture>]
module OperatingSystemTests =

    [<Test>]
    [<TestCase(8,"AMD64",true,TestName="isOperatingSystemX64PlainTests - 64 bits process")>]
    [<TestCase(4,"AMD64",true,TestName="isOperatingSystemX64PlainTests - 32 bits process")>]
    [<TestCase(4,"",false,TestName="isOperatingSystemX64PlainTests - 32 bits process - 32 Bits OS")>]
    let isOperatingSystemX64PlainTests (intPtrSize:int, processorArchitecture:string, expectedIsOperatingSystemX64:bool) =
        let actual = 
            OperatingSystem.isOperatingSystemX64Base intPtrSize processorArchitecture
        Assert.AreEqual(expectedIsOperatingSystemX64,actual)
   
    [<Test>]
    [<TestCase(6,0,true,true,"WIN2008X64")>]    
    [<TestCase(6,0,false,true,"WIN2008X86")>]
    [<TestCase(6,0,true,false,"VISTAX64")>]    
    [<TestCase(6,0,false,false,"VISTAX86")>]
    
    [<TestCase(6,1,true,true, "WIN2008R2X64")>]    
    [<TestCase(6,1,false,true,"WIN2008R2X64")>]
    [<TestCase(6,1,true,false, "WIN7X64")>]    
    [<TestCase(6,1,false,false,"WIN7X86")>]

    [<TestCase(6,2,true,true, "WIN2012X64")>]    
    [<TestCase(6,2,false,true,"WIN2012X64")>]
    [<TestCase(6,2,true,false, "WIN8X64")>]    
    [<TestCase(6,2,false,false,"WIN8X86")>]

    [<TestCase(6,3,true,true, "WIN2012R2X64")>]    
    [<TestCase(6,3,false,true,"WIN2012R2X64")>]
    [<TestCase(6,3,true,false, "WIN81X64")>]    
    [<TestCase(6,3,false,false,"WIN81X86")>]

    [<TestCase(10,0,true,true, "WIN2016X64")>]    
    [<TestCase(10,0,false,true,"WIN2016X64")>]
    [<TestCase(10,0,true,false, "WIN10X64")>]    
    [<TestCase(10,0,false,false,"WIN10X86")>]
    let getOsShortNameBaseTests (osMajorVersion:int, osMinorVersion:int, isX64: bool, isServer:bool,expectedOsShortName:string) =
        let actual = 
            OperatingSystem.getOsShortNameBase (osMajorVersion,osMinorVersion,isX64,isServer)
        Assert.AreEqual(expectedOsShortName,actual)

    [<Test>]
    [<TestCase(1u,false)>]
    [<TestCase(2u,false)>]
    [<TestCase(3u,false)>]
    [<TestCase(4u,false)>]
    [<TestCase(5u,false)>]
    [<TestCase(6u,false)>]
    [<TestCase(7u,true)>]
    [<TestCase(8u,true)>]
    [<TestCase(9u,true)>]
    [<TestCase(10u,true)>]
    [<TestCase(11u,false)>]
    [<TestCase(12u,true)>]
    [<TestCase(13u,true)>]
    [<TestCase(14u,true)>]
    [<TestCase(15u,true)>]
    [<TestCase(16u,false)>]
    [<TestCase(17u,true)>]
    [<TestCase(18u,true)>]
    [<TestCase(19u,true)>]
    [<TestCase(20u,true)>]
    [<TestCase(21u,true)>]
    [<TestCase(22u,true)>]
    [<TestCase(23u,true)>]
    [<TestCase(24u,true)>]
    [<TestCase(25u,true)>]
    [<TestCase(29u,true)>]
    [<TestCase(39u,true)>]
    [<TestCase(40u,true)>]
    [<TestCase(41u,true)>]
    [<TestCase(42u,true)>]
    let isServerTests (sku:UInt32, expected:bool) =
        let actual = OperatingSystem.isServerBase (sku)
        Assert.AreEqual(expected,actual)
        let actualCurrent = OperatingSystem.isServer
        Assert.AreEqual(false,actualCurrent)
        