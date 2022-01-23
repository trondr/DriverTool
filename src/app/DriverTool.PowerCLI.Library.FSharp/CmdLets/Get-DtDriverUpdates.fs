namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation
open System.Management.Automation.Language

/// <summary>
/// <para type="synopsis">Get driver updates</para>
/// <para type="description">Get driver updates</para>
/// <example>
///     <code>Get-DtDriverUpdates</code>
/// </example>
/// </summary>
[<Cmdlet(VerbsCommon.Get,"DtDriverUpdates")>]
[<OutputType(typeof<string>)>]
type GetDtDriverUpdates () =
    inherit PSCmdlet ()
    
    /// <summary>
    /// <para type="description">ComputerName</para>
    /// </summary>    
    [<Parameter(Mandatory=true,ValueFromPipeline=true,ValueFromPipelineByPropertyName=true)>]    
    member val ComputerName :string[] = Array.empty with get,set
    
    override this.BeginProcessing() =
        ()

    override this.ProcessRecord() =
        this.ComputerName
        |> Array.map(fun cn -> this.WriteObject(cn))
        |> ignore
        ()
    
    override this.EndProcessing() =
        ()