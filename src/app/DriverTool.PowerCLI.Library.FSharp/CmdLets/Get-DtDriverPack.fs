namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation
open DriverTool.Library

/// <summary>
/// <para type="synopsis">Get one or more driver packs</para>
/// <para type="description">Get one or more driver packs. An enteprise driver pack typically contains all drivers for a model in inf format and can be injected into the operating system using DISM.</para>
/// <example>
///     <code>Get-DtDriverPack -Manufacturer Dell -ModelCode 79TH</code>
/// </example>
/// <example>
///     <code>Get-DtDriverPack -All</code>
/// </example>
/// <example>
///     <code>
///     # Write-Host "Get driver pack infos for all models found in SCCM"
///     Get-DtCmDeviceModel | Foreach-Object { Get-DtDriverPack -Manufacturer $_.Manufacturer -ModelCode $_.ModelCode -OperatingSystem win10 -Latest }
///     </code>
/// </example>
///      <code>
///      # Write-Host "Get driver pack infos for specified Lenovo models"
///      @("20QW","20QF") | Foreach-Object{ Write-Host "Getting driver pack for model $_";  Get-DtDriverPack -Manufacturer Lenovo -ModelCode $_ -OperatingSystem win10 -OsBuild 21H2 -Verbose }
///      </code>
/// </summary>
[<Cmdlet(VerbsCommon.Get,"DtDriverPack")>]
[<OutputType(typeof<string>)>]
type GetDtDriverPack () =
    inherit PSCmdlet ()

    /// <summary>
    /// <para type="description">Manufacturer.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=Constants.SingleModelParameterSetName)>]
    [<Parameter(Mandatory=true,ParameterSetName=Constants.SingleModelLatestParameterSetName)>]
    [<ArgumentCompleter(typeof<ManufacturerCompleter>)>]
    member val Manufacturer :string = System.String.Empty with get,set
    
    /// <summary>
    /// <para type="description">ModelCode.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=Constants.SingleModelParameterSetName)>]
    [<Parameter(Mandatory=true,ParameterSetName=Constants.SingleModelLatestParameterSetName)>]
    [<ArgumentCompleter(typeof<ModelCodeCompleter>)>]
    member val ModelCode :string = System.String.Empty with get,set

    /// <summary>
    /// <para type="description">OperatingSystem.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=Constants.SingleModelParameterSetName)>]
    [<Parameter(Mandatory=true,ParameterSetName=Constants.SingleModelLatestParameterSetName)>]
    [<ArgumentCompleter(typeof<OperatingSystemCompleter>)>]
    member val OperatingSystem :string = System.String.Empty with get,set

    /// <summary>
    /// <para type="description">OsBuild.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=Constants.SingleModelParameterSetName)>]
    [<ArgumentCompleter(typeof<OsBuildCompleter>)>]
    member val OsBuild :string = System.String.Empty with get,set
    
    /// <summary>
    /// <para type="description">Get all driver packs for all manufacturers.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=Constants.AllModelsParameterSetName)>]
    member val All : SwitchParameter = new SwitchParameter(false) with get,set

    /// <summary>
    /// <para type="description">Get latest driver pack for a specific manufacturer and model.</para>
    /// </summary>
    [<Parameter(Mandatory=true,ParameterSetName=Constants.SingleModelLatestParameterSetName)>]
    member val Latest : SwitchParameter = new SwitchParameter(false) with get,set
    
    override this.BeginProcessing() =        
        ()

    override this.ProcessRecord() =                            
        if(this.All.IsPresent) then            
            Data.allDriverPacks.Value
            |>Array.map(fun dp -> this.WriteObject(dp)) 
            |> ignore            
        elif (this.Latest.IsPresent) then
            let latest =
                Data.allDriverPacks.Value
                |>Array.filter(fun dp -> dp.Manufacturer = this.Manufacturer)
                |>Array.filter(fun dp -> dp.ModelCodes|>Array.contains this.ModelCode)
                |>Array.filter(fun dp -> dp.Os = this.OperatingSystem)
                |>Array.sortByDescending(fun dp -> dp.OsBuild)
                |>Array.head
            this.WriteObject(latest)
            ()
        else     
            Data.allDriverPacks.Value
            |>Array.filter(fun dp -> dp.Manufacturer = this.Manufacturer)
            |>Array.filter(fun dp -> System.String.IsNullOrWhiteSpace(this.ModelCode) || dp.ModelCodes|>Array.contains this.ModelCode)
            |>Array.filter(fun dp -> System.String.IsNullOrWhiteSpace(this.OperatingSystem) || dp.Os = this.OperatingSystem)
            |>Array.filter(fun dp -> System.String.IsNullOrWhiteSpace(this.OsBuild) || dp.OsBuild = this.OsBuild)
            |>Array.map(fun dp -> this.WriteObject(dp))
            |> ignore
  

    