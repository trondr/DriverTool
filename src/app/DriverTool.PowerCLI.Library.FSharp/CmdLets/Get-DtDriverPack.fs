namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Collections
open System.Collections.Generic
open System.Management.Automation
open System.Management.Automation.Language
open DriverTool.Library.ManufacturerTypes
open DriverTool.Library
open DriverTool.Library.Logging

module M =    
    let reportProgressSilently:reportProgressFunction = (fun activity status currentOperation percentComplete isBusy id -> 
        ()//Do not report progress
    )
    
    let getAllDriverPacks () =
        match(result{
            let! cacheFolder = DriverTool.Library.FileSystem.path (DriverTool.Library.Configuration.getDownloadCacheDirectoryPath())                
            let! driverPackInfos = DriverTool.Library.DriverPacks.loadDriverPackInfos cacheFolder reportProgressSilently
            return driverPackInfos
        })with
        |Result.Ok dps -> dps
        |Result.Error ex -> 
            raise ex
            
    let allDriverPacks = lazy (getAllDriverPacks ())

    let getModelCodes manufacturer (driverPacks:DriverPack.DriverPackInfo[]) =
        driverPacks
        |>Array.filter(fun dp -> dp.Manufacturer = manufacturer)
        |>Array.map(fun dp -> dp.ModelCodes |> Array.map(fun m -> m))
        |>Array.collect id
    
    [<Literal>]
    let singleModelparameterSetName = "SingleModel"

    [<Literal>]
    let allModelsparameterSetName = "AllModels"

type ManufacturerCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =
            DriverTool.Library.ManufacturerTypes.getValidManufacturerNames()
            |>Seq.filter(fun m -> (new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase)).IsMatch(m))
            |>Seq.map(fun m -> new CompletionResult(m))

type ModelCodeCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =
            let modelCodes =
                if(fakeBoundParameters.Contains("Manufacturer")) then
                    let manufacturerName = fakeBoundParameters.["Manufacturer"] :?>string
                    M.getModelCodes manufacturerName M.allDriverPacks.Value        
                else
                    [||]            
            modelCodes
            |>Seq.filter(fun mc -> (new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase)).IsMatch(mc))
            |>Seq.map(fun mc -> new CompletionResult(mc))

/// <summary>
/// <para type="synopsis">Get one or more driver packs</para>
/// <para type="description">Get one or more driver packs. An enteprise driver pack typically contains all drivers for a model in inf format and can be injected into the operating system using DISM.</para>
/// <example>
///     <code>Get-DtDriverPack -Manufacturer Dell -ModelCode 79TH</code>
/// </example>
/// <example>
///     <code>Get-DtDriverPack -All</code>
/// </example>
/// </summary>
[<Cmdlet(VerbsCommon.Get,"DtDriverPack")>]
[<OutputType(typeof<string>)>]
type GetDtDriverPack () =
    inherit PSCmdlet ()

    //Get mandatory run time parameter with validate set attribute
    let getRunTimeParameter parameterName parameterSetName position validateSet (argumentCompleterType:System.Type) =
        let attributeCollection = new System.Collections.ObjectModel.Collection<System.Attribute>()
        let parameterAttribute = new System.Management.Automation.ParameterAttribute()
        parameterAttribute.Mandatory <- true
        parameterAttribute.Position <- position
        parameterAttribute.ParameterSetName <- parameterSetName
        attributeCollection.Add(parameterAttribute)        
        if( Array.length validateSet > 0) then
            let validateSetAttribute = new System.Management.Automation.ValidateSetAttribute(validateSet)
            attributeCollection.Add(validateSetAttribute)
            let completerAttribute = new System.Management.Automation.ArgumentCompleterAttribute(argumentCompleterType)
            attributeCollection.Add(completerAttribute)
            let runTimeParameter = new System.Management.Automation.RuntimeDefinedParameter(parameterName,typeof<string>,attributeCollection)
            Some runTimeParameter
        else
            None

    /// <summary>
    /// <para type="description">Manufacturer.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=M.singleModelparameterSetName)>]
    [<ArgumentCompleter(typeof<ManufacturerCompleter>)>]    
    member val Manufacturer :string = System.String.Empty with get,set
    
    member val ModelCode :string = System.String.Empty with get,set
    
    [<Parameter(Mandatory=false,ParameterSetName=M.allModelsparameterSetName)>]
    member val All : SwitchParameter = new SwitchParameter(false) with get,set
    
    override this.BeginProcessing() =
        //TODO: Assign Manufacturer dynamic parameter value.
        if(this.MyInvocation.BoundParameters.ContainsKey(nameof this.ModelCode)) then
            this.ModelCode <- (this.MyInvocation.BoundParameters.[nameof this.ModelCode] :?> string)
        else
            this.ModelCode <- System.String.Empty
        ()

    override this.ProcessRecord() =                            
        if(this.All.IsPresent) then            
            M.allDriverPacks.Value|>Array.map(fun dp -> this.WriteObject(dp)) |> ignore            
        else            
            M.allDriverPacks.Value
            |>Array.filter(fun dp -> dp.Manufacturer = this.Manufacturer)
            |>Array.filter(fun dp -> dp.ModelCodes|>Array.contains this.ModelCode)
            |>Array.map(fun dp -> this.WriteObject(dp)) |> ignore
            
    interface IDynamicParameters with
        member this.GetDynamicParameters() =                                    
            let runtimeParameterDictionary = new System.Management.Automation.RuntimeDefinedParameterDictionary()            
            
            if(this.All.IsPresent) then
                ()
            else     
                //Add ModelCode parameter
                if (not (System.String.IsNullOrWhiteSpace(this.Manufacturer))) then
                    let modelCodeParameterName = nameof this.ModelCode
                    let modelCodeRuntimeParameter = getRunTimeParameter modelCodeParameterName M.singleModelparameterSetName 2 (M.getModelCodes this.Manufacturer M.allDriverPacks.Value) (typeof<ModelCodeCompleter>)
                    match modelCodeRuntimeParameter with
                    |Some m ->                
                        runtimeParameterDictionary.Add(modelCodeParameterName,m)
                    |None -> ()
                else
                    ()
            
            //TODO: Add OS parameter if Model has been defined.
            //TODO: Add OSBuild parameter if OS has been defined.
            runtimeParameterDictionary :> obj

    

    