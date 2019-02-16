namespace DriverTool
open System
open DriverTool.SystemInfo

type InvalidModelCodeException(modelCode:string, message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> sprintf "The model code '%s' is not valid. %s" modelCode message
            |true -> sprintf "The model code '%s' is not valid." modelCode
            )

type ModelCode private (modelCode : string) = 
    member x.Value = modelCode
    
    static member createWithContinuation success failure (modelCode:string) (defaultToLocal:bool) : Result<ModelCode, Exception> =        
        match modelCode with
        | modelCode when System.String.IsNullOrWhiteSpace(modelCode) && defaultToLocal -> 
            match getModelCodeForCurrentSystem() with
            | Ok mc -> success (ModelCode (mc.ToString()) )
            | Error ex -> failure ((new InvalidModelCodeException(String.Empty,sprintf "Failed to get model code for current system. %s" ex.Message)):> Exception)            
        | modelCode when System.String.IsNullOrWhiteSpace(modelCode) -> failure ((new InvalidModelCodeException(modelCode,"ModelCode cannot be null or empty.")) :> Exception)
        | _ -> success (ModelCode modelCode)

    static member create (modelCode : string) (defaultToLocal:bool) =
        let success (value : ModelCode) = Result.Ok value
        let failure ex = Result.Error ex
        ModelCode.createWithContinuation success failure modelCode defaultToLocal
    
    override x.GetHashCode() =
        hash (modelCode)
    
    override x.Equals(b) =
        match b with
        | :? ModelCode as m -> (modelCode) = (m.Value)
        | _ -> false
    
    override x.ToString() =
        x.Value

