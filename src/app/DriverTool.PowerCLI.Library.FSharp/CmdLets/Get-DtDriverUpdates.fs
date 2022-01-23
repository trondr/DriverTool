namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation
open DriverTool.Library

/// <summary>
/// <para type="synopsis">Get driver updates</para>
/// <para type="description">Get driver updates</para>
/// <example>
///     <code>Get-DtDriverUpdates -Manufacturer "Lenovo" -Model "20EQ" -OperatingSystem "WIN10"</code>
/// </example>
/// <example>
///     <code>Get-DtDriverUpdates -Manufacturer "Lenovo" -Model "20EQ" -OperatingSystem "WIN10" -ExcludeDriverUpdates @("BIOS","Firmware") </code>
/// </example>
/// </summary>
[<Cmdlet(VerbsCommon.Get,"DtDriverUpdates")>]
[<OutputType(typeof<string>)>]
type GetDtDriverUpdates () =
    inherit PSCmdlet ()
    
    /// <summary>
    /// <para type="description">Manufacturer.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=Constants.SingleModelParameterSetName)>]    
    [<ArgumentCompleter(typeof<ManufacturerCompleter>)>]
    member val Manufacturer :string = System.String.Empty with get,set
    
    /// <summary>
    /// <para type="description">ModelCode.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=Constants.SingleModelParameterSetName)>]    
    [<ArgumentCompleter(typeof<ModelCodeCompleter>)>]
    member val ModelCode :string = System.String.Empty with get,set
    
    /// <summary>
    /// <para type="description">OperatingSystem.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=Constants.SingleModelParameterSetName)>]    
    [<ArgumentCompleter(typeof<OperatingSystemCodeCompleter>)>]
    member val OperatingSystem :string = System.String.Empty with get,set

    /// <summary>
    /// <para type="description">Exlude driver updates specified in this array of regular expression patterns. If there is a match in package info title or package info category the driver update will be excluded.</para>
    /// </summary>
    [<Parameter(Mandatory=false,ParameterSetName=Constants.SingleModelParameterSetName)>]        
    member val ExcludeDriverUpdates :string[] = System.Array.Empty<string>() with get,set

    override this.BeginProcessing() =
        let manufacturer = ManufacturerTypes.toManufacturer this.Manufacturer
        let model = resultToValueUnsafe (ModelCode.create this.ModelCode false)
        let operatingSystem = resultToValueUnsafe (OperatingSystemCode.create this.OperatingSystem false)
        let cacheFolderPath = resultToValueUnsafe (DriverTool.Library.FileSystem.path (DriverTool.Library.Configuration.getDownloadCacheDirectoryPath()))
        let excludeDriverUpdatesRegExPatters = resultToValueUnsafe (this.ExcludeDriverUpdates |> RegExp.toRegexPatterns true)
        let driverUpdates = resultToValueUnsafe (DriverTool.Library.DriverUpdates.loadDriverUpdates (DriverTool.Library.Logging.reportProgressStdOut') cacheFolderPath manufacturer model operatingSystem excludeDriverUpdatesRegExPatters )
        driverUpdates
        |>Array.map(fun du -> this.WriteObject(du))
        |>ignore
        ()
        
    override this.ProcessRecord() =
        
        ()
    
    override this.EndProcessing() =
        ()