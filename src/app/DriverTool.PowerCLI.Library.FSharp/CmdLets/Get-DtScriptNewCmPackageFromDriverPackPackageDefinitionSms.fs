namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation
open System.Management.Automation.Language
open DriverTool.Library.F0
open DriverTool.Library

/// <summary>
/// <para type="synopsis">Get script for creating new Sccm Package from DriverPack Package Definition Sms.</para>
/// <para type="description">Get script for creating new Sccm Package from DriverPack Package Definition Sms.</para>
/// <example>
///     <code>Get-DtScriptNewCmPackageFromDriverPackPackageDefinitionSms</code>
/// </example>
/// </summary>
[<Cmdlet(VerbsCommon.Get,"DtScriptNewCmPackageFromDriverPackPackageDefinitionSms")>]
[<OutputType(typeof<string>)>]
type GetDtScriptNewCmPackageFromDriverPackPackageDefinitionSms () =
    inherit PSCmdlet ()
    
    /// <summary>
    /// <para type="description">PackageDefinition</para>
    /// </summary>    
    [<Parameter(Mandatory=true,ValueFromPipeline=true,ValueFromPipelineByPropertyName=true)>]    
    member val Path : string[] = Array.empty with get,set
    
    override this.BeginProcessing() =
        ()

    override this.ProcessRecord() =
        this.Path
        |> Array.map(fun p -> 
                let pd = DriverTool.Library.PackageDefinitionSms.readFromFileUnsafe p
                match pd.SourcePath with
                |Some sourcePath ->
                    let sourceFolderPath = DirectoryOperations.getParentFolderPathUnsafe (DriverTool.Library.FileSystem.pathUnSafe sourcePath)
                    let script = DriverTool.Library.Sccm.toNewCmPackagePSScript sourceFolderPath pd
                    this.WriteObject(script)
                |None ->
                    let name = (WrappedString.value pd.Name)
                    raise (toException (sprintf "Source path is missing for %s" name) None)            
            )
        |> ignore
        ()
    
    override this.EndProcessing() =
        ()