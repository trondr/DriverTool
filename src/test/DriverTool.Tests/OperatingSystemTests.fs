namespace DriverTool.Tests
open NUnit.Framework
open System
open DriverTool.Library

[<TestFixture>]
[<Category(TestCategory.UnitTests)>]
module OperatingSystemTests =

    [<Test>]
    [<TestCase(8,"AMD64",true,TestName="isOperatingSystemX64BaseTests - 64 bits process")>]
    [<TestCase(4,"AMD64",true,TestName="isOperatingSystemX64BaseTests - 32 bits process")>]
    [<TestCase(4,"",false,TestName="isOperatingSystemX64BaseTests - 32 bits process - 32 Bits OS")>]
    let isOperatingSystemX64BaseTests (intPtrSize:int, processorArchitecture:string, expectedIsOperatingSystemX64:bool) =
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
    [<TestCase(0u,false,"An unknown product")>]
    [<TestCase(1u,false,"Ultimate")>]
    [<TestCase(2u,false,"Home Basic")>]
    [<TestCase(3u,false,"Home Premium")>]
    [<TestCase(4u,false,"Windows 10 Enterprise")>]
    [<TestCase(5u,false,"Home Basic N")>]
    [<TestCase(6u,false,"Business")>]
    [<TestCase(7u,true,"Server Standard (full installation. For Server Core installations of Windows Server 2012 and later, use the method Determining whether Server Core is running.)")>]
    [<TestCase(8u,true,"Server Datacenter (full installation. For Server Core installations of Windows Server 2012 and later, use the method Determining whether Server Core is running.)")>]
    [<TestCase(9u,true,"Windows Small Business Server")>]
    [<TestCase(10u,true,"Server Enterprise (full installation)")>]
    [<TestCase(11u,false,"Starter")>]
    [<TestCase(12u,true,"Server Datacenter (core installation, Windows Server 2008 R2 and earlier)")>]
    [<TestCase(13u,true,"Server Standard (core installation, Windows Server 2008 R2 and earlier)")>]
    [<TestCase(14u,true,"Server Enterprise (core installation)")>]
    [<TestCase(15u,true,"Server Enterprise for Itanium-based Systems")>]
    [<TestCase(16u,false,"Business N")>]
    [<TestCase(17u,true,"Web Server (full installation)")>]
    [<TestCase(18u,true,"HPC Edition")>]
    [<TestCase(19u,true,"Windows Storage Server 2008 R2 Essentials")>]
    [<TestCase(20u,true,"Storage Server Express")>]
    [<TestCase(21u,true,"Storage Server Standard")>]
    [<TestCase(22u,true,"Storage Server Workgroup")>]
    [<TestCase(23u,true,"Storage Server Enterprise")>]
    [<TestCase(24u,true,"Windows Server 2008 for Windows Essential Server Solutions")>]
    [<TestCase(25u,true,"Small Business Server Premium")>]
    [<TestCase(26u,false,"Home Premium N")>]
    [<TestCase(27u,false,"Windows 10 Enterprise N")>]
    [<TestCase(28u,false,"Ultimate N")>]
    [<TestCase(29u,true,"Web Server (core installation)")>]
    [<TestCase(30u,true,"Windows Essential Business Server Management Server")>]
    [<TestCase(31u,true,"Windows Essential Business Server Security Server")>]
    [<TestCase(32u,true,"Windows Essential Business Server Messaging Server")>]
    [<TestCase(33u,true,"Server Foundation")>]
    [<TestCase(34u,true,"Windows Home Server 2011")>]
    [<TestCase(35u,true,"Windows Server 2008 without Hyper-V for Windows Essential Server Solutions")>]
    [<TestCase(36u,true,"Server Standard without Hyper-V")>]
    [<TestCase(37u,true,"Server Datacenter without Hyper-V (full installation)")>]
    [<TestCase(38u,true,"Server Enterprise without Hyper-V (full installation)")>]
    [<TestCase(39u,true,"Server Datacenter without Hyper-V (core installation)")>]
    [<TestCase(40u,true,"Server Standard without Hyper-V (core installation)")>]
    [<TestCase(41u,true,"Server Enterprise without Hyper-V (core installation)")>]
    [<TestCase(42u,true,"Microsoft Hyper-V Server")>]
    [<TestCase(43u,true,"Storage Server Express (core installation)")>]
    [<TestCase(44u,true,"Storage Server Standard (core installation)")>]
    [<TestCase(45u,true,"Storage Server Workgroup (core installation)")>]
    [<TestCase(46u,true,"Storage Server Enterprise (core installation)")>]
    [<TestCase(47u,false,"Starter N")>]
    [<TestCase(48u,false,"Windows 10 Pro")>]
    [<TestCase(49u,false,"Windows 10 Pro N")>]
    [<TestCase(50u,true,"Windows Small Business Server 2011 Essentials")>]
    [<TestCase(51u,true,"Server For SB Solutions")>]
    [<TestCase(52u,true,"Server Solutions Premium")>]
    [<TestCase(53u,true,"Server Solutions Premium (core installation)")>]
    [<TestCase(54u,true,"Server For SB Solutions EM")>]
    [<TestCase(55u,true,"Server For SB Solutions EM")>]
    [<TestCase(56u,true,"Windows MultiPoint Server")>]
    [<TestCase(59u,true,"Windows Essential Server Solution Management")>]
    [<TestCase(60u,true,"Windows Essential Server Solution Additional")>]
    [<TestCase(61u,true,"Windows Essential Server Solution Management SVC")>]
    [<TestCase(62u,true,"Windows Essential Server Solution Additional SVC")>]
    [<TestCase(63u,true,"Small Business Server Premium (core installation)")>]
    [<TestCase(64u,true,"Server Hyper Core V")>]
    [<TestCase(66u,false,"Not supported")>]
    [<TestCase(67u,false,"Not supported")>]
    [<TestCase(68u,false,"Not supported")>]
    [<TestCase(69u,false,"Not supported")>]
    [<TestCase(70u,false,"Windows 10 Enterprise E")>]
    [<TestCase(71u,false,"Not supported")>]
    [<TestCase(72u,false,"Windows 10 Enterprise Evaluation")>]
    [<TestCase(76u,true,"Windows MultiPoint Server Standard (full installation)")>]
    [<TestCase(77u,true,"Windows MultiPoint Server Premium (full installation)")>]
    [<TestCase(79u,true,"Server Standard (evaluation installation)")>]
    [<TestCase(80u,true,"Server Datacenter (evaluation installation)")>]
    [<TestCase(84u,false,"Windows 10 Enterprise N Evaluation")>]
    [<TestCase(95u,true,"Storage Server Workgroup (evaluation installation)")>]
    [<TestCase(96u,true,"Storage Server Standard (evaluation installation)")>]
    [<TestCase(98u,false,"Windows 10 Home N")>]
    [<TestCase(99u,false,"Windows 10 Home China")>]
    [<TestCase(100u,false,"Windows 10 Home Single Language")>]
    [<TestCase(101u,false,"Windows 10 Home")>]
    [<TestCase(103u,false,"Professional with Media Center")>]
    [<TestCase(104u,false,"Windows 10 Mobile")>]
    [<TestCase(121u,false,"Windows 10 Education")>]
    [<TestCase(122u,false,"Windows 10 Education N")>]
    [<TestCase(123u,false,"Windows 10 IoT Core")>]
    [<TestCase(125u,false,"Windows 10 Enterprise 2015 LTSB")>]
    [<TestCase(126u,false,"Windows 10 Enterprise 2015 LTSB N")>]
    [<TestCase(129u,false,"Windows 10 Enterprise 2015 LTSB Evaluation")>]
    [<TestCase(130u,false,"Windows 10 Enterprise 2015 LTSB N Evaluation")>]
    [<TestCase(131u,false,"Windows 10 IoT Core Commercial")>]
    [<TestCase(133u,false,"Windows 10 Mobile Enterprise")>]
    [<TestCase(145u,true,"Server Datacenter, Semi-Annual Channel (core installation)")>]
    [<TestCase(146u,true,"Server Standard, Semi-Annual Channel (core installation)")>]
    [<TestCase(161u,false,"Windows 10 Pro for Workstations")>]
    [<TestCase(162u,false,"Windows 10 Pro for Workstations N")>]
    let isServerTests (sku:UInt32, expected:bool, name:string) =
        let actual = OperatingSystem.isServerBase (sku)
        Assert.AreEqual(expected,actual,name)
        let actualCurrent = OperatingSystem.isServer
        Assert.AreEqual(false,actualCurrent,name)
        
    [<Test>]
    [<TestCase("WIN10X64")>]
    [<TestCase("WIN10X86")>]
    [<TestCase("WIN81X64")>]
    [<TestCase("WIN81X86")>]
    [<TestCase("WIN8X64")>]
    [<TestCase("WIN8X86")>]
    [<TestCase("WIN7X64")>]
    [<TestCase("WIN7X86")>]    
    [<TestCase("WIN2008X64")>]    
    [<TestCase("WIN2008X86")>]
    [<TestCase("WIN2008R2X64")>]    
    [<TestCase("WIN2008R2X64")>]
    [<TestCase("WIN2012X64")>]    
    [<TestCase("WIN2012X64")>]
    [<TestCase("WIN2012R2X64")>]    
    [<TestCase("WIN2012R2X64")>]
    [<TestCase("WIN2016X64")>]    
    [<TestCase("WIN2016X64")>]
    let getValidOsShortNamesTest (osShortName) =
        let actual = OperatingSystem.getValidOsShortNames
        actual |> Seq.map (fun v -> printfn "%s" v) |> ignore
        Assert.AreEqual(osShortName, actual|>Seq.find (fun v -> v = osShortName) )

    [<Test>]
    [<TestCase("WIN10X64",true)>]
    [<TestCase("WIN10X86",true)>]
    [<TestCase("WIN81X64",true)>]
    [<TestCase("WIN81X86",true)>]
    [<TestCase("WIN8X64",true)>]
    [<TestCase("WIN8X86",true)>]
    [<TestCase("WIN7X64",true)>]
    [<TestCase("WIN7X86",true)>]    
    [<TestCase("WIN2008X64",true)>]    
    [<TestCase("WIN2008X86",true)>]
    [<TestCase("WIN2008R2X64",true)>]    
    [<TestCase("WIN2008R2X64",true)>]
    [<TestCase("WIN2012X64",true)>]    
    [<TestCase("WIN2012X64",true)>]
    [<TestCase("WIN2012R2X64",true)>]    
    [<TestCase("WIN2012R2X64",true)>]
    [<TestCase("WIN2016X64",true)>]    
    [<TestCase("WIN2016X64",true)>]
    [<TestCase("WIN10",false)>]
    let isValidOsShortName (osShortName,expexted) =
        let actual = OperatingSystem.isValidOsShortName osShortName
        Assert.AreEqual(expexted,actual,osShortName)


    [<Test>]
    [<TestCase("1809","1809")>]
    [<TestCase(null,"*")>]
    let getOsBuildForCurrentSystemBaseTests (releaseIdFromRegistry:obj,expected:string) =
        
        let getRegValueStub (keyPath:string) (valueName:string) : option<obj> =
            Assert.AreEqual("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",keyPath,"keyPath not expected.")
            Assert.AreEqual("ReleaseId",valueName,"valueName not expected.")
            
            if(releaseIdFromRegistry = null) then
                None
            else
                Some releaseIdFromRegistry

        let actual = OperatingSystem.getOsReleaseIdForCurrentSystemBase getRegValueStub

        Assert.AreEqual(expected, actual)