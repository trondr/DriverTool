namespace DriverTool
open System
open DriverTool

type InvalidModelCodeException(modelCode:string, message : string) =
        inherit Exception(
            match String.IsNullOrWhiteSpace(message) with
            |false  -> String.Format("The model code '{0}' is not valid. {1}", modelCode, message)
            |true -> String.Format("The model code '{0}' is not valid.", modelCode)
            )

type ModelCode private (modelCode : string) = 
    member x.Value = modelCode
    
    static member createWithContinuation success failure (modelCode:string) (defaultToLocal:bool) : Result<ModelCode, Exception> =
        let getModelCodeForLocalSystem : Result<string, Exception> = 
            let localModelCodeResult = WmiHelper.getWmiProperty "Win32_ComputerSystem" "Model"
            localModelCodeResult
        
        match modelCode with
        | modelCode when System.String.IsNullOrWhiteSpace(modelCode) && defaultToLocal -> 
            match getModelCodeForLocalSystem with
            | Ok mc -> success (ModelCode (mc.ToString()) )
            | Error ex -> failure ((new InvalidModelCodeException(String.Empty,String.Format("Failed to get model code from WMI. {0}", ex.Message))):> Exception)
            
        | modelCode when System.String.IsNullOrWhiteSpace(modelCode) -> failure ((new InvalidModelCodeException(modelCode,"ModelCode cannot be null or empty.")) :> Exception)
        | _ -> success (ModelCode modelCode)

    static member create (modelCode : string) =
        let success (value : ModelCode) = Result.Ok value
        let failure ex = Result.Error ex
        ModelCode.createWithContinuation success failure modelCode
    
    override x.GetHashCode() =
        hash (modelCode)
    
    override x.Equals(b) =
        match b with
        | :? ModelCode as m -> (modelCode) = (m.Value)
        | _ -> false
    
    override x.ToString() =
        x.Value

