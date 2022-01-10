namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation
open DriverTool.Library
open DriverTool.Library.DriverPack

module InvokeDtDownloadDriverPack =
    
    let downloadDriverPackInfo reportProgress dp =
        match(result{
            let! cacheFolder = DriverTool.Library.FileSystem.path (DriverTool.Library.Configuration.getDownloadCacheDirectoryPath())                
            let! driverPackInfos = DriverTool.Updates.downloadDriverPackInfo cacheFolder reportProgress dp
            return driverPackInfos
        })with
        |Result.Ok dps -> dps
        |Result.Error ex -> 
            raise ex

/// <summary>
/// <para type="synopsis">Download driverpack</para>
/// <para type="description">Download driverpack</para>
/// <example>
///     <code>Invoke-DtDownloadDriverPack -DriverPack $driverPack</code>
/// </example>
/// <example>
///     <code>Get-DtDriverPack -Manufacturer Lenovo -ModelCode 20EQ -OperatingSystem win10 -Latest | Invoke-DtDownloadDriverPack</code>
/// </example>
/// </summary>
[<Cmdlet(VerbsLifecycle.Invoke,"DtDownloadDriverPack")>]
[<OutputType(typeof<string>)>]
type InvokeDtDownloadDriverPack () =
    inherit PSCmdlet ()

    /// <summary>
    /// <para type="description">Driver pack info</para>
    /// </summary>
    [<Parameter(Mandatory=true,ValueFromPipeline=true)>]
    member val DriverPack :DriverPackInfo[] = Array.empty with get,set
    
    override this.BeginProcessing() =
        ()

    override this.ProcessRecord() =        
        
        let reportProgressStdOut =
            DriverTool.Library.Logging.reportProgressStdOut'

        this.DriverPack
        |> Array.map(fun dp ->        
                let ddp = InvokeDtDownloadDriverPack.downloadDriverPackInfo reportProgressStdOut dp                
                this.WriteObject(ddp)
                )
        |> ignore
        ()
    
    override this.EndProcessing() =
        ()