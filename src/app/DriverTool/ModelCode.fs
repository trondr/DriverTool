namespace DriverTool
open System

type ModelCode private (modelCode : string) = 
    member x.Value = modelCode
    
    static member createWithContinuation success failure (modelCode:string) (defaultToLocal:bool) : Result<'ModelCode, 'Exception> =
        let getModelCodeForLocalSystem : Result<'string, 'Exception> = 
            let localModelCodeResult = WmiHelper.getWmiProperty "Win32_ComputerSystem" "Model"
            localModelCodeResult
        
        match modelCode with
        | modelCode when System.String.IsNullOrWhiteSpace(modelCode) && defaultToLocal -> 
            match getModelCodeForLocalSystem with
            | Ok mc -> success (ModelCode (mc.ToString()) )
            | Error ex -> failure (new Exception(sprintf "%s" ex.Message))
            
        | modelCode when System.String.IsNullOrWhiteSpace(modelCode) -> failure (new ArgumentNullException("Model code value cannot be null") :> Exception)
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


