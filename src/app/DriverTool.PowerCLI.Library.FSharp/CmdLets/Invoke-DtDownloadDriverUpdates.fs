namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation
open DriverTool.Library
open DriverTool.Library.DriverUpdates

module InvokeDtDriverUpdatesDownload =
    
    let packageDriverUpdates reportProgress modelInfo packagePublisher packageInstallLogDirectory =
        match(result{
            let! cacheFolder = DriverTool.Library.FileSystem.path (DriverTool.Library.Configuration.getDownloadCacheDirectoryPath())            
            let! pacakgeDefinitionSms = DriverTool.Library.CmUi.UiModels.packageDriverUpdates cacheFolder reportProgress modelInfo packagePublisher packageInstallLogDirectory
            return FileSystem.pathValue pacakgeDefinitionSms
        })with
        |Result.Ok pdSmsPath -> pdSmsPath
        |Result.Error ex -> 
            raise ex

/// <summary>
/// <para type="synopsis">Download and package driver updates for a specified model</para>
/// <para type="description">Download and package driver updates for a specified model</para>
/// <example>
///     <code> Get-DtDriverUpdates -Manufacturer "Lenovo" -Model "20EQ" -OperatingSystem "WIN10" -ExcludeDriverUpdates @("BIOS","Firmware") | Invoke-DtDriverUpdatesDownload </code>
/// </example>
/// <example>
///     <code>
///     @("20QW","20QF") | Foreach-Object{ Write-Host "Getting driver updates for model $_";  Get-DtDriverUpdates -Manufacturer Lenovo -ModelCode "$_" -OperatingSystem "WIN10X64" -Verbose } | Invoke-DtDownloadDriverUpdates
///     </code>
/// </example>
/// <example>
///     <code>
///     @("20QW","20QF") | Foreach-Object{ Write-Host "Getting driver updates for model $_";  Get-DtDriverUpdates -Manufacturer Lenovo -ModelCode "$_" -OperatingSystem "WIN10X64" -OsBuild "21H2" -ExcludeDriverUpdates @("BIOS","Firmware") -Verbose } | Invoke-DtDownloadDriverUpdates
///     </code>
/// </example>
/// </summary>
[<Cmdlet(VerbsLifecycle.Invoke,"DtDownloadDriverUpdates")>]
[<OutputType(typeof<string>)>]
type InvokeDtDriverUpdatesDownload () =
    inherit PSCmdlet ()
    
    /// <summary>
    /// <para type="description">Model info with driver updates</para>
    /// </summary>
    [<Parameter(Mandatory=true,ValueFromPipeline=true)>]
    member val ModelInfo :ModelInfo[] = Array.empty with get,set
    
    /// <summary>
    /// <para type="description">Optional. Package publisher name. Typically set to developers name or company name. Default value: DT</para>
    /// </summary>
    [<Parameter(Mandatory=false)>]
    member val PackagePublisher : string = "DT" with get,set

    /// <summary>
    /// <para type="description">Otional. Package install log directory. The location where log files will be written when driver updates package is installed on a target computer. Default value: %TEMP%</para>
    /// </summary>
    [<Parameter(Mandatory=false)>]
    member val PackageInstallLogDirectory : string = "%TEMP%" with get,set

    override this.BeginProcessing() =
        ()

    override this.ProcessRecord() =
        let reportProgressStdOut =
            DriverTool.Library.Logging.reportProgressStdOut'

        this.ModelInfo
        |> Array.map(fun mi ->        
                let ddp = InvokeDtDriverUpdatesDownload.packageDriverUpdates reportProgressStdOut mi this.PackagePublisher this.PackageInstallLogDirectory               
                this.WriteObject(ddp)
                )
        |> ignore
    
    override this.EndProcessing() =
        ()