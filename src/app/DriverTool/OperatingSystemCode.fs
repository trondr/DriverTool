namespace DriverTool
open System

type OperatingSystemCode private (operatingSystemCode : string) = 
    member x.Value = operatingSystemCode
    
    static member createWithContinuation success failure (operatingSystemCode:string) (defaultToLocal:bool) : Result<'OperatingSystemCode, 'Exception> =
        
        let OsCaptionToOperatingSystemCode (caption:string) = 
            match caption with
            | caption when caption.Contains("Windows 10") -> Result.Ok "Win10"
            | caption when caption.Contains("Windows 7") -> Result.Ok "Win7"
            | caption when caption.Contains("Windows 8") -> Result.Ok "Win8"
            | _ -> Result.Error (new Exception(sprintf "OS %s is not supported." caption))

        let getOperatingSystemCodeForLocalSystem : Result<'string, 'Exception> = 
            let captionResult = WmiHelper.getWmiProperty "Win32_OperatingSystem" "Caption"
            match captionResult with
            | Ok c -> OsCaptionToOperatingSystemCode (c.ToString())
            | Error ex -> Result.Error ex
        
        match operatingSystemCode with
        | operatingSystemCode when System.String.IsNullOrWhiteSpace(operatingSystemCode) && defaultToLocal -> 
            match getOperatingSystemCodeForLocalSystem with
            | Ok s -> 
                match s.ToString().ToUpper() with
                | "WIN7" -> success (OperatingSystemCode ("Win7") )
                | "WIN8" -> success (OperatingSystemCode ("Win8") )
                | "WIN10" -> success (OperatingSystemCode ("Win10") )
                | _ -> failure (new Exception(sprintf "Invalid operatings system code specified. Valid values are: Win7, Win8, Win10."))                
            | Error ex -> failure (new Exception(sprintf "%s" ex.Message))
            
        | operatingSystemCode when System.String.IsNullOrWhiteSpace(operatingSystemCode) -> failure (new ArgumentNullException("Operating system code value cannot be null") :> Exception)
        | _ -> success (OperatingSystemCode operatingSystemCode)

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


