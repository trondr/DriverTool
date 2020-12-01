namespace DriverTool.Library
open System

type InvalidOperatingSystemCodeException(operatingSytemCode:string, message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> sprintf "The operating system code '%s' is not valid. Valid values are: %s. %s" operatingSytemCode (String.Join<string>("|",OperatingSystem.getValidOsShortNames))  message
            |true -> sprintf "The operating system code '%s' is not valid. Valid values are: %s." operatingSytemCode (String.Join<string>("|",OperatingSystem.getValidOsShortNames))
            )

type OperatingSystemCode private (operatingSystemCode : string) = 
    member x.Value = operatingSystemCode
    
    static member createWithContinuation success failure (operatingSystemCode:string) (defaultToLocal:bool) : Result<OperatingSystemCode, Exception> =        
        
        let getOperatingSystemCodeForLocalSystem : Result<string, Exception> = 
            try
                Result.Ok OperatingSystem.getOsShortName
            with
            | ex -> Result.Error ((new InvalidOperatingSystemCodeException(String.Empty,sprintf "Failed to get operating system due to: %s" ex.Message)) :> Exception)
                    
        let validateOperatingSystemCode (operatingSystemCode:string) =
            match operatingSystemCode with
            | operatingSystemCode when System.String.IsNullOrWhiteSpace(operatingSystemCode) -> failure (new InvalidOperatingSystemCodeException(operatingSystemCode,"OperatingSystemCode cannot be null or empty.") :> Exception)
            | operatingSytemCode when (DriverTool.Library.OperatingSystem.isValidOsShortName operatingSytemCode) -> 
                success (OperatingSystemCode (operatingSystemCode))
            | _ ->
                failure (new InvalidOperatingSystemCodeException(operatingSystemCode,String.Empty) :> Exception)

        match operatingSystemCode with
        | operatingSystemCode when System.String.IsNullOrWhiteSpace(operatingSystemCode) && defaultToLocal -> 
            match getOperatingSystemCodeForLocalSystem with
            | Ok os -> validateOperatingSystemCode os
            | Error ex -> failure ((new InvalidOperatingSystemCodeException(String.Empty,ex.Message)) :> Exception)
        | _ -> validateOperatingSystemCode operatingSystemCode

    static member create (operatingSystemCode : string) (defaultToLocal:bool) =
        let success (value : OperatingSystemCode) = Result.Ok value
        let failure ex = Result.Error ex
        OperatingSystemCode.createWithContinuation success failure operatingSystemCode defaultToLocal
    
    override x.GetHashCode() =
        hash (operatingSystemCode)
    
    override x.Equals(b) =
        match b with
        | :? OperatingSystemCode as m -> (operatingSystemCode) = (m.Value)
        | _ -> false

    override x.ToString() =
        x.Value


