namespace DriverTool.Library

module OperatingSystem =
    open System
    open DriverTool.Library.F
    open DriverTool.Library
    
    let getOperatingSystemSku : Result<UInt32,Exception> =
        let operatingSystemSku = 
            WmiHelper.getWmiPropertyDefault "Win32_OperatingSystem" "OperatingSystemSKU"
        operatingSystemSku
    
    let isServerBase (operatingSystemSku : UInt32) : bool  =
        match operatingSystemSku with
        |0u -> false //An unknown product
        |1u -> false //Ultimate
        |2u -> false //Home Basic
        |3u -> false //Home Premium
        |4u -> false //Windows 10 Enterprise
        |5u -> false //Home Basic N
        |6u -> false //Business
        |7u -> true //Server Standard (full installation. For Server Core installations of Windows Server 2012 and later, use the method Determining whether Server Core is running.)
        |8u -> true //Server Datacenter (full installation. For Server Core installations of Windows Server 2012 and later, use the method Determining whether Server Core is running.)|9u -> true //Windows Small Business Server
        |9u -> true //Windows Small Business Server Edition
        |10u -> true //Server Enterprise (full installation)
        |11u -> false //Starter
        |12u -> true //Server Datacenter (core installation, Windows Server 2008 R2 and earlier)
        |13u -> true //Server Standard (core installation, Windows Server 2008 R2 and earlier)
        |14u -> true //Server Enterprise (core installation)
        |15u -> true //Server Enterprise for Itanium-based Systems
        |16u -> false //Business N
        |17u -> true //Web Server (full installation)
        |18u -> true //HPC Edition
        |19u -> true //Windows Storage Server 2008 R2 Essentials
        |20u -> true //Storage Server Express
        |21u -> true //Storage Server Standard
        |22u -> true //Storage Server Workgroup
        |23u -> true //Storage Server Enterprise
        |24u -> true //Windows Server 2008 for Windows Essential Server Solutions
        |25u -> true //Small Business Server Premium
        |26u -> false //Home Premium N
        |27u -> false //Windows 10 Enterprise N
        |28u -> false //Ultimate N
        |29u -> true //Web Server (core installation)
        |30u -> true //Windows Essential Business Server Management Server
        |31u -> true //Windows Essential Business Server Security Server
        |32u -> true //Windows Essential Business Server Messaging Server
        |33u -> true //Server Foundation
        |34u -> true //Windows Home Server 2011
        |35u -> true //Windows Server 2008 without Hyper-V for Windows Essential Server Solutions
        |36u -> true //Server Standard without Hyper-V
        |37u -> true //Server Datacenter without Hyper-V (full installation)
        |38u -> true //Server Enterprise without Hyper-V (full installation)
        |39u -> true //Server Datacenter without Hyper-V (core installation)
        |40u -> true //Server Standard without Hyper-V (core installation)
        |41u -> true //Server Enterprise without Hyper-V (core installation)
        |42u -> true //Microsoft Hyper-V Server
        |43u -> true //Storage Server Express (core installation)
        |44u -> true //Storage Server Standard (core installation)
        |45u -> true //Storage Server Workgroup (core installation)
        |46u -> true //Storage Server Enterprise (core installation)
        |47u -> false //Starter N
        |48u -> false //Windows 10 Pro
        |49u -> false //Windows 10 Pro N
        |50u -> true //Windows Small Business Server 2011 Essentials
        |51u -> true //Server For SB Solutions
        |52u -> true //Server Solutions Premium
        |53u -> true //Server Solutions Premium (core installation)
        |54u -> true //Server For SB Solutions EM
        |55u -> true //Server For SB Solutions EM
        |56u -> true //Windows MultiPoint Server
        |59u -> true //Windows Essential Server Solution Management
        |60u -> true //Windows Essential Server Solution Additional
        |61u -> true //Windows Essential Server Solution Management SVC
        |62u -> true //Windows Essential Server Solution Additional SVC
        |63u -> true //Small Business Server Premium (core installation)
        |64u -> true //Server Hyper Core V
        |66u -> false //Not supported
        |67u -> false //Not supported
        |68u -> false //Not supported
        |69u -> false //Not supported
        |70u -> false //Windows 10 Enterprise E
        |71u -> false //Not supported
        |72u -> false //Windows 10 Enterprise Evaluation
        |76u -> true //Windows MultiPoint Server Standard (full installation)
        |77u -> true //Windows MultiPoint Server Premium (full installation)
        |79u -> true //Server Standard (evaluation installation)
        |80u -> true //Server Datacenter (evaluation installation)
        |84u -> false //Windows 10 Enterprise N Evaluation
        |95u -> true //Storage Server Workgroup (evaluation installation)
        |96u -> true //Storage Server Standard (evaluation installation)
        |98u -> false //Windows 10 Home N
        |99u -> false //Windows 10 Home China
        |100u -> false //Windows 10 Home Single Language
        |101u -> false //Windows 10 Home
        |103u -> false //Professional with Media Center
        |104u -> false //Windows 10 Mobile
        |121u -> false //Windows 10 Education
        |122u -> false //Windows 10 Education N
        |123u -> false //Windows 10 IoT Core
        |125u -> false //Windows 10 Enterprise 2015 LTSB
        |126u -> false //Windows 10 Enterprise 2015 LTSB N
        |129u -> false //Windows 10 Enterprise 2015 LTSB Evaluation
        |130u -> false //Windows 10 Enterprise 2015 LTSB N Evaluation
        |131u -> false //Windows 10 IoT Core Commercial
        |133u -> false //Windows 10 Mobile Enterprise
        |145u -> true //Server Datacenter, Semi-Annual Channel (core installation)
        |146u -> true //Server Standard, Semi-Annual Channel (core installation)
        |161u -> false //Windows 10 Pro for Workstations
        |162u -> false //Windows 10 Pro for Workstations N
        |_ -> raise (new Exception(sprintf "OperatingSystemSKU '%i' is unknown. " operatingSystemSku))
    
    let isServer =
        match(getOperatingSystemSku) with
        |Ok sku -> isServerBase sku
        |Error ex -> raise (new Exception("Failed to determine if current operatingsystem is a server edition due to: " + ex.Message))

    let isOperatingSystemX64Base intPtrSize processorArchitecture =
        match intPtrSize with
        | 8 -> true
        | _ -> 
            match String.IsNullOrWhiteSpace(processorArchitecture) with
            |false -> 
                processorArchitecture.EndsWith("64")
            |true -> false

    let isOperatingSystemX64 =
        isOperatingSystemX64Base (System.IntPtr.Size) (System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))
    
    let getOsShortNamev10 (osMinorVersion:int, isX64: bool, isServer:bool) =
        ""
    
    let getOsShortNamev60 (isX64: bool, isServer:bool) =
        match isServer with
        |true -> 
            match isX64 with
            |true -> "WIN2008X64"
            |false -> "WIN2008X86"
        |false ->
            match isX64 with
            |true -> "VISTAX64"
            |false -> "VISTAX86"
     
    let getOsShortNamev61 (isX64: bool, isServer:bool) =
        match isServer with
        |true -> 
            match isX64 with
            |_  -> "WIN2008R2X64" //X86 do not exist as Windows 2008 R2 Server is X64 only
        |false ->
            match isX64 with
            |true -> "WIN7X64"
            |false -> "WIN7X86"
    
    let getOsShortNamev62 (isX64: bool, isServer:bool) =
        match isServer with
        |true -> 
            match isX64 with
            |_  -> "WIN2012X64" //X86 do not exist as Windows 2012 Server is X64 only
        |false ->
            match isX64 with
            |true -> "WIN8X64"
            |false -> "WIN8X86"
    
    let getOsShortNamev63 (isX64: bool, isServer:bool) =
        match isServer with
        |true -> 
            match isX64 with
            |_ -> "WIN2012R2X64" //X86 do not exist as Windows 2012 R2 Server is X64 only
        |false ->
            match isX64 with
            |true -> "WIN81X64"
            |false -> "WIN81X86"

    let getOsShortNamev100 (isX64: bool, isServer:bool) =
        match isServer with
        |true -> 
            match isX64 with
            |_ -> "WIN2016X64" //X86 do not exist as Windows 2012 Server is X64 only
        |false ->
            match isX64 with
            |true -> "WIN10X64"
            |false -> "WIN10X86"

    let getOsShortNameBase (osMajorVersion:int, osMinorVersion:int, isX64: bool, isServer:bool) = 
        match osMajorVersion with
        | 6  -> 
            match osMinorVersion with
            | 0 -> getOsShortNamev60 (isX64, isServer)
            | 1 -> getOsShortNamev61 (isX64, isServer)
            | 2 -> getOsShortNamev62 (isX64, isServer)
            | 3 -> getOsShortNamev63 (isX64, isServer)
            |_ -> raise (new Exception(sprintf "Unsupported Operating System versjon '%i.%i'. " osMajorVersion osMinorVersion))
        | 10 -> 
            match osMinorVersion with
            | 0 -> getOsShortNamev100 (isX64, isServer)
            |_ -> raise (new Exception(sprintf "Unsupported Operating System versjon '%i.%i'. " osMajorVersion osMinorVersion))
        |_ -> raise (new Exception(sprintf "Unsupported Operating System major versjon '%i'. " osMajorVersion))
    
    let versionToMajorMinorVersion (version:string) =
        match version with
        |Regex @"^(\d+)\.(\d+)" [majorVersion;minorVersion] -> (Convert.ToInt32(majorVersion),Convert.ToInt32(minorVersion))
        |_ -> (0,0)

    let getOsVersion =
        let osVersion = 
            WmiHelper.getWmiPropertyDefault "Win32_OperatingSystem" "Version"
        match osVersion with
        |Ok osVer -> versionToMajorMinorVersion osVer
        |Error ex -> raise (new Exception("Failed to get os version due to: " + ex.Message))

    let getOsShortName =
        let (osMajorVersion,osMinorVersion) = getOsVersion
        let isX64 = isOperatingSystemX64        
        getOsShortNameBase (osMajorVersion, osMinorVersion, isX64, isServer)

    let getValidOsShortNames =
        let versions = 
            seq{ 
                   for isX64 in [|true;false|] do                   
                        for isServer in [|true;false|] do
                            yield (10, 0, isX64, isServer)
                            yield (6, 0, isX64, isServer)
                            yield (6, 1, isX64, isServer)
                            yield (6, 2, isX64, isServer)
                            yield (6, 3, isX64, isServer)                
               }
        versions |> Seq.map (fun v -> getOsShortNameBase v)
        
    let isValidOsShortName osShortName =
        getValidOsShortNames |> Seq.exists (fun o -> o = osShortName)
    
    let getOsBuildFromName name = 
        match name with
        |Regex @"(\d{4})" [osBuild] -> osBuild
        | _ -> "*"

    let getOsBuildFromName2 name = 
        match name with
        |Regex @"(1607)" [osBuild] -> osBuild
        |Regex @"(1703)" [osBuild] -> osBuild
        |Regex @"(1709)" [osBuild] -> osBuild
        |Regex @"(1803)" [osBuild] -> osBuild
        |Regex @"(1809)" [osBuild] -> osBuild
        |Regex @"(1903)" [osBuild] -> osBuild
        |Regex @"(1909)" [osBuild] -> osBuild
        |Regex @"(2003)" [osBuild] -> osBuild
        |Regex @"(2009)" [osBuild] -> osBuild
        |Regex @"(2103)" [osBuild] -> osBuild
        |Regex @"(2109)" [osBuild] -> osBuild
        |Regex @"(2203)" [osBuild] -> osBuild
        |Regex @"(2209)" [osBuild] -> osBuild
        | _ -> "*"

    let getOsBuildForCurrentSystemBase (getRegValueFunc:(string -> string -> option<obj>)) =
        let relaseId = getRegValueFunc "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion" "ReleaseId"
        relaseId|> Option.fold (fun _ r -> r.ToString()) "*"

    /// <summary>
    /// Get os build (relase id) for current system from registry. If release id is not found, return *. 
    /// Release id is typically 1709, 1803, 1809, 1903 etc defining the semi anual release of Windows 10
    /// </summary>
    let getOsBuildForCurrentSystem =
        getOsBuildForCurrentSystemBase RegistryOperations.getRegValue        