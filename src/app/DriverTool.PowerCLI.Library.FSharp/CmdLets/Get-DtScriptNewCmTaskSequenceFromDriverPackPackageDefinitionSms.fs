namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation
open System.Management.Automation.Language
open DriverTool.Library.F0
open DriverTool.Library

/// <summary>
/// <para type="synopsis">Create new task sequence from driver pack package definitions.</para>
/// <para type="description">Create new task sequence from driver pack package definitions.</para>
/// <example>
///     <code>Get-DtScriptNewCmTaskSequenceFromDriverPackPackageDefinitionSms -Path @("\\sccmserver01\PkgSrc\Packages\Package1\PackageDefinition.sms","\\sccmserver01\PkgSrc\Packages\Package2\PackageDefinition.sms")</code>
/// </example>
/// </summary>
[<Cmdlet(VerbsCommon.Get,"DtScriptNewCmTaskSequenceFromDriverPackPackageDefinitionSms")>]
[<OutputType(typeof<string>)>]
type GetDtScriptNewCmTaskSequenceFromDriverPackPackageDefinitionSms () =
    inherit PSCmdlet ()
    
    /// <summary>
    /// <para type="description">Path to PackageDefinition.sms</para>
    /// </summary>    
    [<Parameter(Mandatory=true,ValueFromPipeline=true,ValueFromPipelineByPropertyName=true)>]    
    member val Path : string[] = Array.empty with get,set

    /// <summary>
    /// <para type="description">Task sequence name.</para>
    /// </summary>    
    [<Parameter(Mandatory=true)>]    
    member val Name : string = System.String.Empty with get,set

    /// <summary>
    /// <para type="description">Task sequence description.</para>
    /// </summary>    
    [<Parameter(Mandatory=true)>]    
    member val Description : string = System.String.Empty with get,set

    /// <summary>
    /// <para type="description">Program name to run. Must exist in all PackageDefinition.sms files provided.</para>
    /// </summary>    
    [<Parameter(Mandatory=true)>]    
    member val ProgramName : string = System.String.Empty with get,set
    
    override this.BeginProcessing() =
        ()

    override this.ProcessRecord() =
        let packageDefinitions = 
            this.Path
            |> Array.map DriverTool.Library.PackageDefinitionSms.readFromFileUnsafe        
        let script = resultToValueUnsafe (DriverTool.Library.Sccm.toCustomTaskSequenceScript this.Name this.Description this.ProgramName packageDefinitions)
        this.WriteObject(script)
        ()
    
    override this.EndProcessing() =
        ()