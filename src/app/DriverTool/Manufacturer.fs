namespace DriverTool
open System
open DriverTool
open DriverTool.SystemInfo

type InvalidManufacturerException(manufacturer:string, message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> String.Format("The manufacturer value '{0}' is not valid. {1}", manufacturer, message)
            |true -> String.Format("The manufacturer value '{0}' is not valid.", manufacturer)
            )

type Manufacturer private (manufacturerName : ManufacturerName) = 
    member x.Value = manufacturerName
    
    static member createWithContinuation success failure (manufacturerString:string) (defaultToLocal:bool) : Result<Manufacturer, Exception> =

        match manufacturerString with
        | manufacturer when System.String.IsNullOrWhiteSpace(manufacturer) && defaultToLocal -> 
            match getManufacturerForCurrentSystem() with
            | Ok mc -> success (Manufacturer (mc) )
            | Error ex -> failure ((new InvalidManufacturerException(String.Empty,String.Format("Failed to model code for current system. {0}", ex.Message))):> Exception)  
        | manufacturer when System.String.IsNullOrWhiteSpace(manufacturer) -> 
            failure ((new InvalidManufacturerException(manufacturer,"Manufacturer cannot be null or empty.")) :> Exception)
        | _ ->  
            let manufacturerNameResult = manufacturerToManufacturerName manufacturerString
            match manufacturerNameResult with
            |Ok m -> success (Manufacturer (m))
            |Error ex -> failure ((new InvalidManufacturerException(manufacturerString,ex.Message)) :> Exception)

    static member create (manufacturer : string) =
        let success (value : Manufacturer) = Result.Ok value
        let failure ex = Result.Error ex
        Manufacturer.createWithContinuation success failure manufacturer
    
    override x.GetHashCode() =
        hash (manufacturerName)
    
    override x.Equals(b) =
        match b with
        | :? Manufacturer as m -> (manufacturerName) = (m.Value)
        | _ -> false
    
    override x.ToString() =
        x.Value.ToString()