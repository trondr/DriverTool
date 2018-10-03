namespace DriverTool
open System

type InvalidOperatingSystemCodeException(operatingSytemCode:string, message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> String.Format("The operating system code '{0}' is not valid. Valid values are: Win7, Win8, Win10. {1}", operatingSytemCode, message)
            |true -> String.Format("The operating system code '{0}' is not valid. Valid values are: Win7, Win8, Win10.", operatingSytemCode)
            )

type OperatingSystemCode private (operatingSystemCode : string) = 
    member x.Value = operatingSystemCode
    
    static member createWithContinuation success failure (operatingSystemCode:string) (defaultToLocal:bool) : Result<OperatingSystemCode, Exception> =        
        let OsCaptionToOperatingSystemCode (caption:string) = 
            match caption with
            | caption when caption.Contains("Windows 10") -> Result.Ok "Win10"
            | caption when caption.Contains("Windows 7") -> Result.Ok "Win7"
            | caption when caption.Contains("Windows 8") -> Result.Ok "Win8"
            | _ -> Result.Error ((new InvalidOperatingSystemCodeException(String.Empty,String.Format("Operating System '{0}' is not supported.", caption))) :> Exception)

        let getOperatingSystemCodeForLocalSystem : Result<string, Exception> = 
            let captionResult = WmiHelper.getWmiProperty "Win32_OperatingSystem" "Caption"
            match captionResult with
            | Ok c -> OsCaptionToOperatingSystemCode (c.ToString())
            | Error ex -> Result.Error ((new InvalidOperatingSystemCodeException(String.Empty,String.Format("Failed to get operating system from WMI. {0}", ex.Message))) :> Exception)
        
        let validateOperatingSystemCode (operatingSystemCode:string) =
            match operatingSystemCode with
            | operatingSystemCode when System.String.IsNullOrWhiteSpace(operatingSystemCode) -> failure (new InvalidOperatingSystemCodeException(operatingSystemCode,"OperatingSystemCode cannot be null or empty.") :> Exception)
            | _ ->
                match operatingSystemCode.ToUpper() with
                | "WIN7" -> success (OperatingSystemCode ("Win7") )
                | "WIN8" -> success (OperatingSystemCode ("Win8") )
                | "WIN10" -> success (OperatingSystemCode ("Win10") )
                | _ -> failure ((new InvalidOperatingSystemCodeException(operatingSystemCode,String.Empty)) :> Exception)

        match operatingSystemCode with
        | operatingSystemCode when System.String.IsNullOrWhiteSpace(operatingSystemCode) && defaultToLocal -> 
            match getOperatingSystemCodeForLocalSystem with
            | Ok os -> validateOperatingSystemCode os
            | Error ex -> failure ((new InvalidOperatingSystemCodeException(String.Empty,ex.Message)) :> Exception)
        | _ -> validateOperatingSystemCode operatingSystemCode

    static member create (modelCode : string) =
        let success (value : OperatingSystemCode) = Result.Ok value
        let failure ex = Result.Error ex
        OperatingSystemCode.createWithContinuation success failure modelCode
    
    override x.GetHashCode() =
        hash (operatingSystemCode)
    
    override x.Equals(b) =
        match b with
        | :? OperatingSystemCode as m -> (operatingSystemCode) = (m.Value)
        | _ -> false

    override x.ToString() =
        x.Value


