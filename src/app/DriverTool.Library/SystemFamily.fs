namespace DriverTool.Library
open System
open DriverTool.Library.SystemInfo

type InvalidSystemFamilyException(systemFamily:string, message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> sprintf "The system family '%s' is not valid. %s" systemFamily message
            |true -> sprintf "The system family '%s' is not valid." systemFamily
            )

type SystemFamily private (systemFamily : string) = 
    member x.Value = systemFamily
    
    static member createWithContinuation success failure (systemFamily:string) (defaultToLocal:bool) : Result<SystemFamily, Exception> =

        match systemFamily with
        | systemFamily when System.String.IsNullOrWhiteSpace(systemFamily) && defaultToLocal -> 
            match getSystemFamilyForCurrentSystem() with
            | Ok sf -> success (SystemFamily sf)
            | Error ex -> failure ((new InvalidSystemFamilyException(String.Empty,sprintf "Failed to get system family from WMI. %s" ex.Message)):> Exception)
            
        | systemFamily when System.String.IsNullOrWhiteSpace(systemFamily) -> failure ((new InvalidSystemFamilyException(systemFamily,"SystemFamily cannot be null or empty.")) :> Exception)
        | _ -> success (SystemFamily systemFamily)

    static member create (systemFamily : string) =
        let success (value : SystemFamily) = Result.Ok value
        let failure ex = Result.Error ex
        SystemFamily.createWithContinuation success failure systemFamily
    
    override x.GetHashCode() =
        hash (systemFamily)
    
    override x.Equals(b) =
        match b with
        | :? SystemFamily as m -> (systemFamily) = (m.Value)
        | _ -> false
    
    override x.ToString() =
        x.Value

