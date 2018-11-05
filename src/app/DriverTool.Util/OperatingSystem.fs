namespace DriverTool.Util.FSharp

module OperatingSystem =
    open System
    
    let getOperatingSystemSku : Result<int,Exception> =
        let operatingSystemSku = 
            WmiHelper.getWmiProperty "Win32_OperatingSystem" "OperatingSystemSKU"
        operatingSystemSku

    let isServer (operatingSystemSku : int) : bool  =
        match operatingSystemSku with
        |0  -> false
        |1  -> false
        |2  -> false
        |3  -> false
        |4  -> false
        |5  -> false
        |6  -> false
        |7  -> true
        |8  -> true
        |9  -> true
        |10 -> true
        |11 -> false
        |12 -> true
        |13 -> true
        |14 -> true
        |15 -> true
        |16 -> false
        |17 -> true
        |18 -> true
        |19 -> true
        |20 -> true
        |21 -> true
        |22 -> true
        |23 -> true
        |24 -> true
        |25 -> true
        |29 -> true
        |39 -> true
        |40 -> true
        |41 -> true
        |42 -> true
        |_ -> raise (new Exception(String.Format("OperatingSystemSKU '{0}' is unknown. ",operatingSystemSku)))
    
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
    
    let getOsShortName =
        let osMajorVersion = System.Environment.OSVersion.Version.Major
        let osMinorVersion = System.Environment.OSVersion.Version.Minor
        let isX64 = isOperatingSystemX64        
        match getOperatingSystemSku with
        |Ok operatingSystemSku -> 
            let isServerF = (isServer operatingSystemSku)
            getOsShortNameBase (osMajorVersion, osMinorVersion,isX64,isServerF)
        |Error _ -> "UNKNOWN_OS"