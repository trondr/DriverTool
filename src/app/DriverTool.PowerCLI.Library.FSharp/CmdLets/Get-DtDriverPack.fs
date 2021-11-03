namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Collections
open System.Collections.Generic
open System.Management.Automation
open System.Management.Automation.Language
open DriverTool.Library.ManufacturerTypes
open DriverTool.Library


type ManufacturerCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =            
            DriverTool.Library.ManufacturerTypes.getValidManufacturerNames()
            |>Seq.filter(fun m -> (new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase)).IsMatch(m))
            |>Seq.map(fun m -> new CompletionResult(m))

module M =
    //TODO: Get modelCode list from downloaded list of all models
    let getModelCodes manufacturerName =        
        let manufacturer = DriverTool.Library.ManufacturerTypes.manufacturerStringToManufacturer (manufacturerName,false)                    
        match manufacturer with
        |Result.Ok m ->
            match m with
            |Dell _ -> [|"79EQ";"79TH";"79L1"|]
            |Lenovo _ -> [|"20EQ";"20TH";"20L1"|]
            |HP _ -> [|"68EQ";"68TH";"68L1"|]
        |Result.Error _ ->                         
            [||]

    [<Literal>]
    let singleModelparameterSetName = "SingleModel"

    [<Literal>]
    let allModelsparameterSetName = "AllModels"

type ModelCodeCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =
            //TODO: Get modelCode list from downloaded list of all models
            let modelCodes =
                if(fakeBoundParameters.Contains("Manufacturer")) then
                    let manufacturerName = fakeBoundParameters.["Manufacturer"] :?>string
                    M.getModelCodes manufacturerName         
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

    /// <summary>
    /// <para type="description">Model code.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=M.singleModelparameterSetName)>]
    [<ArgumentCompleter(typeof<ModelCodeCompleter>)>]
    member val ModelCode :string = System.String.Empty with get,set
    
    [<Parameter(Mandatory=false,ParameterSetName=M.allModelsparameterSetName)>]
    member val All : SwitchParameter = new SwitchParameter(false) with get,set
    
    override this.BeginProcessing() =
        //TODO: Assign Manufacturer dynamic parameter value.
        if(this.MyInvocation.BoundParameters.ContainsKey(nameof this.Manufacturer)) then
            this.Manufacturer <- (this.MyInvocation.BoundParameters.[nameof this.Manufacturer] :?> string)
        else
            this.Manufacturer <- System.String.Empty

    override this.ProcessRecord() =                            
        if(this.All.IsPresent) then
            match (result{
                let cacheFolder = DriverTool.Library.FileSystem.pathUnSafe (DriverTool.Library.Configuration.getDownloadCacheDirectoryPath())                
                let! driverPackInfos = DriverTool.Library.DriverPacks.loadDriverPackInfos cacheFolder
                return driverPackInfos
            }) with
            |Result.Ok dps ->
                dps|>Array.map(fun dp ->                            
                        this.WriteObject(dp)                        
                        ) |> ignore
            |Result.Error ex -> (raise ex)
        else
            //TODO: Get info for specific model code.
            ()

    interface IDynamicParameters with
        member this.GetDynamicParameters() =                                    
            let runtimeParameterDictionary = new System.Management.Automation.RuntimeDefinedParameterDictionary()            
            
            if(this.All.IsPresent) then
                ()
            else
                //Add Manufacturer parameter
                let manufacturerParameterName = nameof this.Manufacturer
                if(not (runtimeParameterDictionary.ContainsKey(manufacturerParameterName))) then               
                   let runtimeParameter = getRunTimeParameter manufacturerParameterName M.singleModelparameterSetName 1 (DriverTool.Library.ManufacturerTypes.getValidManufacturerNames()) (typeof<ManufacturerCompleter>)
                   match runtimeParameter with
                   |Some r ->
                        r.Value <- this.Manufacturer                
                        runtimeParameterDictionary.Add(manufacturerParameterName,r)
                   |None -> ()
                else
                   ()            
            
                //Add ModelCode parameter
                if (not (System.String.IsNullOrWhiteSpace(this.Manufacturer))) then
                    let modelCodeParameterName = nameof this.ModelCode
                    let modelCodeRuntimeParameter = getRunTimeParameter modelCodeParameterName M.singleModelparameterSetName 2 (M.getModelCodes this.Manufacturer) (typeof<ModelCodeCompleter>)
                    match modelCodeRuntimeParameter with
                    |Some m ->                
                        runtimeParameterDictionary.Add(modelCodeParameterName,m)
                    |None -> ()
                else
                    ()
            
            //TODO: Add OS parameter if Model has been defined.
            //TODO: Add OSBuild parameter if OS has been defined.
            runtimeParameterDictionary :> obj

    

    