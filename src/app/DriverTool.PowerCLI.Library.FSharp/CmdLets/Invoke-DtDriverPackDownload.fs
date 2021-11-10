namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation
open DriverTool.Library.DriverPack

/// <summary>
/// <para type="synopsis">Download driverpack</para>
/// <para type="description">Download driverpack</para>
/// <example>
///     <code>Invoke-DtDownloadDriverPack -DriverPack $driverPack</code>
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
        this.DriverPack
        |> Array.map(fun dp -> this.WriteObject(dp))
        |> ignore
        ()
    
    override this.EndProcessing() =
        ()