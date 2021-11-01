namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Collections
open System.Collections.Generic
open System.Management.Automation
open System.Management.Automation.Language
open DriverTool.Library.ManufacturerTypes


type ManufacturerCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =            
            DriverTool.Library.ManufacturerTypes.getValidManufacturers()
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
/// <para type="synopsis">Get one or more sccm packages</para>
/// <para type="description">Get one or more sccm packages</para>
/// <example>
///     <code>Get-DtSccmPackage</code>
/// </example>
/// </summary>
[<Cmdlet(VerbsCommon.Get,"DtSccmPackage")>]
[<OutputType(typeof<string>)>]
type GetDtSccmPackage () =
    inherit PSCmdlet ()

    //Get mandatory run time parameter with validate set attribute
    let getRunTimeParameter parameterName position validateSet (argumentCompleterType:System.Type) =
        let attributeCollection = new System.Collections.ObjectModel.Collection<System.Attribute>()
        let parameterAttribute = new System.Management.Automation.ParameterAttribute()
        parameterAttribute.Mandatory <- true
        parameterAttribute.Position <- position        
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
    [<Parameter(Mandatory=false)>]
    [<ArgumentCompleter(typeof<ManufacturerCompleter>)>]
    member val Manufacturer :string = System.String.Empty with get,set

    /// <summary>
    /// <para type="description">Model code.</para>
    /// </summary>
    [<Parameter(Mandatory=false)>]
    [<ArgumentCompleter(typeof<ModelCodeCompleter>)>]
    member val ModelCode :string = System.String.Empty with get,set

    member val private _runtimeParameterDictionary: System.Management.Automation.RuntimeDefinedParameterDictionary = null with get,set
    
    override this.BeginProcessing() =
        //TODO: Assign Manufacturer dynamic parameter value.
        if(this.MyInvocation.BoundParameters.ContainsKey(nameof this.Manufacturer)) then
            this.Manufacturer <- (this.MyInvocation.BoundParameters.[nameof this.Manufacturer] :?> string)
        else
            this.Manufacturer <- System.String.Empty

    override this.ProcessRecord() =
        let some = "Manufacturer: " + this.Manufacturer
        this.WriteObject(some)
        ()

    interface IDynamicParameters with
        member this.GetDynamicParameters() =                                    
            let runtimeParameterDictionary = new System.Management.Automation.RuntimeDefinedParameterDictionary()            
            
            //Add Manufacturer parameter
            let manufacturerParameterName = nameof this.Manufacturer
            if(not (runtimeParameterDictionary.ContainsKey(manufacturerParameterName))) then               
               let runtimeParameter = getRunTimeParameter manufacturerParameterName 1 (DriverTool.Library.ManufacturerTypes.getValidManufacturers()) (typeof<ManufacturerCompleter>)
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
                let modelCodeRuntimeParameter = getRunTimeParameter modelCodeParameterName 2 (M.getModelCodes this.Manufacturer) (typeof<ModelCodeCompleter>)
                match modelCodeRuntimeParameter with
                |Some m ->                
                    runtimeParameterDictionary.Add(modelCodeParameterName,m)
                |None -> ()
            else
                ()
            
            //TODO: Add OS parameter if Model has been defined.
            //TODO: Add OSBuild parameter if OS has been defined.
            runtimeParameterDictionary :> obj

    

    