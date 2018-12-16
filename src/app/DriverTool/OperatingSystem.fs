namespace DriverTool

module OperatingSystem =
    open System
    
    let getOperatingSystemSku : Result<UInt32,Exception> =
        let operatingSystemSku = 
            WmiHelper.getWmiProperty "Win32_OperatingSystem" "OperatingSystemSKU"
        operatingSystemSku
    
    let isServerBase (operatingSystemSku : UInt32) : bool  =
        match operatingSystemSku with
        |0u  -> false
        |1u  -> false
        |2u  -> false
        |3u  -> false
        |4u -> false
        |5u  -> false
        |6u  -> false
        |7u  -> true
        |8u  -> true
        |9u  -> true
        |10u -> true
        |11u -> false
        |12u -> true
        |13u -> true
        |14u -> true
        |15u -> true
        |16u -> false
        |17u -> true
        |18u -> true
        |19u -> true
        |20u -> true
        |21u -> true
        |22u -> true
        |23u -> true
        |24u -> true
        |25u -> true
        |29u -> true
        |39u -> true
        |40u -> true
        |41u -> true
        |42u -> true
        |_ -> raise (new Exception(String.Format("OperatingSystemSKU '{0}' is unknown. ",operatingSystemSku)))
    
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
            |_ -> raise (new Exception(String.Format("Unsupported Operating System versjon '{0}.{1}'. ", osMajorVersion, osMinorVersion)))
        | 10 -> 
            match osMinorVersion with
            | 0 -> getOsShortNamev100 (isX64, isServer)
            |_ -> raise (new Exception(String.Format("Unsupported Operating System versjon '{0}.{1}'. ", osMajorVersion, osMinorVersion)))
        |_ -> raise (new Exception(String.Format("Unsupported Operating System major versjon '{0}'. ",osMajorVersion)))
    
    open F    

    let versionToMajorMinorVersion (version:string) =
        match version with
        |Regex @"^(\d+)\.(\d+)" [majorVersion;minorVersion] -> (Convert.ToInt32(majorVersion),Convert.ToInt32(minorVersion))
        |_ -> (0,0)

    let getOsVersion =
        let osVersion = 
            WmiHelper.getWmiProperty "Win32_OperatingSystem" "Version"
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