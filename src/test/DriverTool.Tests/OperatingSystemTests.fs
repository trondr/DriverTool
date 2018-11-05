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

