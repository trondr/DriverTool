namespace DriverTool
open System
open DriverTool.Util.FSharp

type InvalidOperatingSystemCodeException(operatingSytemCode:string, message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> String.Format("The operating system code '{0}' is not valid. Valid values are: Win7, Win8, Win10. {1}", operatingSytemCode, message)
            |true -> String.Format("The operating system code '{0}' is not valid. Valid values are: Win7, Win8, Win10.", operatingSytemCode)
            )

type OperatingSystemCode private (operatingSystemCode : string) = 
    member x.Value = operatingSystemCode
    
    static member createWithContinuation success failure (operatingSystemCode:string) (defaultToLocal:bool) : Result<OperatingSystemCode, Exception> =        
        
        let getOperatingSystemCodeForLocalSystem : Result<string, Exception> = 
            try
                Result.Ok OperatingSystem.getOsShortName
            with
            | ex -> Result.Error ((new InvalidOperatingSystemCodeException(String.Empty,String.Format("Failed to get operating system due to: {0}", ex.Message))) :> Exception)
                    
        let validateOperatingSystemCode (operatingSystemCode:string) =
            match operatingSystemCode with
            | operatingSystemCode when System.String.IsNullOrWhiteSpace(operatingSystemCode) -> failure (new InvalidOperatingSystemCodeException(operatingSystemCode,"OperatingSystemCode cannot be null or empty.") :> Exception)
            | operatingSytemCode when (DriverTool.Util.FSharp.OperatingSystem.isValidOsShortName operatingSytemCode) -> 
                success (OperatingSystemCode (operatingSystemCode))
            | _ ->
                failure (new InvalidOperatingSystemCodeException(operatingSystemCode,String.Format("Invalid operating system code. Valid codes are: ...")) :> Exception)

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


