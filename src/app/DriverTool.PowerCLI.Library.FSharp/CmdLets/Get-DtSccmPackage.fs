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

//TODO: Implement ModelCompleter to returning model codes for specified Manufacturer. Manufacturer should be available via fakeBoundParameters.

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
    
    let getManufacturerRuntimeParameter parameterName =        
        let attributeCollection = new System.Collections.ObjectModel.Collection<System.Attribute>()
        let parameterAttribute = new System.Management.Automation.ParameterAttribute()
        parameterAttribute.Mandatory <- true
        parameterAttribute.Position <- 1
        attributeCollection.Add(parameterAttribute)
        let manufacturers = DriverTool.Library.ManufacturerTypes.getValidManufacturers()
        let validateSetAttribute = new System.Management.Automation.ValidateSetAttribute(manufacturers)
        attributeCollection.Add(validateSetAttribute)
        let manufacturerCompleterAttribute = new System.Management.Automation.ArgumentCompleterAttribute(typeof<ManufacturerCompleter>)
        attributeCollection.Add(manufacturerCompleterAttribute)
        let runTimeParameter = new System.Management.Automation.RuntimeDefinedParameter(parameterName,typeof<string>,attributeCollection)
        runTimeParameter

    let getModelCodeRuntimeParameter modelCodeParameterName manufacturerName =
        let attributeCollection = new System.Collections.ObjectModel.Collection<System.Attribute>()
        let parameterAttribute = new System.Management.Automation.ParameterAttribute()
        parameterAttribute.Mandatory <- true
        parameterAttribute.Position <- 1
        attributeCollection.Add(parameterAttribute)
        let modelCodes = M.getModelCodes manufacturerName
        if( Array.length modelCodes > 0) then
            let validateSetAttribute = new System.Management.Automation.ValidateSetAttribute(modelCodes)
            attributeCollection.Add(validateSetAttribute)
            let completerAttribute = new System.Management.Automation.ArgumentCompleterAttribute(typeof<ModelCodeCompleter>)
            attributeCollection.Add(completerAttribute)
            let runTimeParameter = new System.Management.Automation.RuntimeDefinedParameter(modelCodeParameterName,typeof<string>,attributeCollection)
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
            
            let manufacturerParameterName = nameof this.Manufacturer
            if(not (runtimeParameterDictionary.ContainsKey(manufacturerParameterName))) then               
               let runtimeParameter = getManufacturerRuntimeParameter manufacturerParameterName
               runtimeParameter.Value <- this.Manufacturer
               runtimeParameterDictionary.Add(manufacturerParameterName,runtimeParameter)               
            else
                ()            
            
            if (not (System.String.IsNullOrWhiteSpace(this.Manufacturer))) then
                let modelCodeParameterName = nameof this.ModelCode
                let modelCodeRuntimeParameter = getModelCodeRuntimeParameter modelCodeParameterName this.Manufacturer
                match modelCodeRuntimeParameter with
                |Some m ->                
                    runtimeParameterDictionary.Add(modelCodeParameterName,m)
                |None -> ()
            else
                ()
            
            //TODO: Add OS parameter if Model has been defined.
            //TODO: Add OSBuild parameter if OS has been defined.
            runtimeParameterDictionary :> obj

    

    