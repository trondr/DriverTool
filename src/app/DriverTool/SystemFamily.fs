namespace DriverTool
open System
open DriverTool

type InvalidSystemFamilyException(systemFamily:string, message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> String.Format("The system family '{0}' is not valid. {1}", systemFamily, message)
            |true -> String.Format("The system family '{0}' is not valid.", systemFamily)
            )

type SystemFamily private (systemFamily : string) = 
    member x.Value = systemFamily
    
    static member createWithContinuation success failure (systemFamily:string) (defaultToLocal:bool) : Result<SystemFamily, Exception> =
        let getSystemFamilyForLocalSystem : Result<string, Exception> = 
            let localSystemFamilyResult = WmiHelper.getWmiPropertyDefault "Win32_ComputerSystem" "SystemFamily"
            localSystemFamilyResult
        
        match systemFamily with
        | systemFamily when System.String.IsNullOrWhiteSpace(systemFamily) && defaultToLocal -> 
            match getSystemFamilyForLocalSystem with
            | Ok mc -> success (SystemFamily (mc.ToString()) )
            | Error ex -> failure ((new InvalidSystemFamilyException(String.Empty,String.Format("Failed to get system family from WMI. {0}", ex.Message))):> Exception)
            
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

