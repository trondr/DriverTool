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
    
    let isOperatingSystemX64Plain intPtrSize processorArchitecture =
        match intPtrSize with
        | 8 -> true
        | _ -> 
            match String.IsNullOrWhiteSpace(processorArchitecture) with
            |false -> 
                processorArchitecture.EndsWith("64")
            |true -> false

    let isOperatingSystemX64 =
        isOperatingSystemX64Plain (System.IntPtr.Size) (System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))
    
    