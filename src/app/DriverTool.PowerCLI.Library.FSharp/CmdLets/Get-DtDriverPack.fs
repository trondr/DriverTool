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

    let getOperatingSystems manufacturer modelCode (driverPacks:DriverPack.DriverPackInfo[]) =
        driverPacks
        |>Array.filter(fun dp -> dp.Manufacturer = manufacturer)
        |>Array.filter(fun dp ->  dp.ModelCodes |> Array.contains modelCode)
        |>Array.map(fun dp-> dp.Os)
        |>Array.distinct

    let getOsBuild manufacturer modelCode operatingSystem (driverPacks:DriverPack.DriverPackInfo[]) =
        driverPacks
        |>Array.filter(fun dp -> dp.Manufacturer = manufacturer)
        |>Array.filter(fun dp ->  dp.ModelCodes |> Array.contains modelCode)
        |>Array.filter(fun dp -> dp.Os = operatingSystem)
        |>Array.map(fun dp-> dp.OsBuild)
        |>Array.distinct
    
    [<Literal>]
    let singleModelparameterSetName = "SingleModel"

    [<Literal>]
    let allModelsparameterSetName = "AllModels"

    [<Literal>]
    let singleModelLatestparameterSetName = "SingleModelLatest"

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

type OperatingSystemCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =
            let modelCodes =
                if(fakeBoundParameters.Contains("Manufacturer") && fakeBoundParameters.Contains("ModelCode")) then
                    let manufacturerName = fakeBoundParameters.["Manufacturer"] :?>string
                    let modelCode = fakeBoundParameters.["ModelCode"] :?>string
                    M.getOperatingSystems manufacturerName modelCode M.allDriverPacks.Value        
                else
                    [||]            
            modelCodes
            |>Seq.filter(fun mc -> (new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase)).IsMatch(mc))
            |>Seq.map(fun mc -> new CompletionResult(mc))

type OsBuildCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =
            let modelCodes =
                if(fakeBoundParameters.Contains("Manufacturer") && fakeBoundParameters.Contains("ModelCode")) then
                    let manufacturerName = fakeBoundParameters.["Manufacturer"] :?>string
                    let modelCode = fakeBoundParameters.["ModelCode"] :?>string
                    let operatingSystem = fakeBoundParameters.["OperatingSystem"] :?>string
                    M.getOsBuild manufacturerName modelCode operatingSystem M.allDriverPacks.Value        
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

    /// <summary>
    /// <para type="description">Manufacturer.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=M.singleModelparameterSetName)>]
    [<Parameter(Mandatory=true,ParameterSetName=M.singleModelLatestparameterSetName)>]
    [<ArgumentCompleter(typeof<ManufacturerCompleter>)>]
    member val Manufacturer :string = System.String.Empty with get,set
    
    /// <summary>
    /// <para type="description">ModelCode.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=M.singleModelparameterSetName)>]
    [<Parameter(Mandatory=true,ParameterSetName=M.singleModelLatestparameterSetName)>]
    [<ArgumentCompleter(typeof<ModelCodeCompleter>)>]
    member val ModelCode :string = System.String.Empty with get,set

    /// <summary>
    /// <para type="description">OperatingSystem.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=M.singleModelparameterSetName)>]
    [<Parameter(Mandatory=true,ParameterSetName=M.singleModelLatestparameterSetName)>]
    [<ArgumentCompleter(typeof<OperatingSystemCompleter>)>]
    member val OperatingSystem :string = System.String.Empty with get,set

    /// <summary>
    /// <para type="description">OsBuild.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=M.singleModelparameterSetName)>]
    [<ArgumentCompleter(typeof<OsBuildCompleter>)>]
    member val OsBuild :string = System.String.Empty with get,set
    
    /// <summary>
    /// <para type="description">Get all driver packs for all manufacturers.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=M.allModelsparameterSetName)>]
    member val All : SwitchParameter = new SwitchParameter(false) with get,set

    /// <summary>
    /// <para type="description">Get latest driver pack for a specific manufacturer and model.</para>
    /// </summary>
    [<Parameter(Mandatory=true,ParameterSetName=M.singleModelLatestparameterSetName)>]
    member val Latest : SwitchParameter = new SwitchParameter(false) with get,set
    
    override this.BeginProcessing() =        
        ()

    override this.ProcessRecord() =                            
        if(this.All.IsPresent) then            
            M.allDriverPacks.Value
            |>Array.map(fun dp -> this.WriteObject(dp)) 
            |> ignore            
        elif (this.Latest.IsPresent) then
            let latest =
                M.allDriverPacks.Value
                |>Array.filter(fun dp -> dp.Manufacturer = this.Manufacturer)
                |>Array.filter(fun dp -> dp.ModelCodes|>Array.contains this.ModelCode)
                |>Array.filter(fun dp -> dp.Os = this.OperatingSystem)
                |>Array.sortByDescending(fun dp -> dp.OsBuild)
                |>Array.head
            this.WriteObject(latest)
            ()
        else     
            M.allDriverPacks.Value
            |>Array.filter(fun dp -> dp.Manufacturer = this.Manufacturer)
            |>Array.filter(fun dp -> System.String.IsNullOrWhiteSpace(this.ModelCode) || dp.ModelCodes|>Array.contains this.ModelCode)
            |>Array.filter(fun dp -> System.String.IsNullOrWhiteSpace(this.OperatingSystem) || dp.Os = this.OperatingSystem)
            |>Array.filter(fun dp -> System.String.IsNullOrWhiteSpace(this.OsBuild) || dp.OsBuild = this.OsBuild)
            |>Array.map(fun dp -> this.WriteObject(dp))
            |> ignore
  

    