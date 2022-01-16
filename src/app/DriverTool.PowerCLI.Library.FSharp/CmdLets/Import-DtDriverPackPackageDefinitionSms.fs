namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation

/// <summary>
/// <para type="synopsis">Import Driver Pack Package Definiton Sms file.</para>
/// <para type="description">Import Driver Pack Package Definiton Sms file.</para>
/// <example>
///     <code>Import-DtDriverPackPackageDefinitionSms Path ""</code>
/// </example>
/// </summary>
[<Cmdlet(VerbsData.Import,"DtDriverPackPackageDefinitionSms")>]
[<OutputType(typeof<string>)>]
type ImportDtDriverPackPackageDefintionSms () =
    inherit PSCmdlet ()
    
    /// <summary>
    /// <para type="description">ComputerName</para>
    /// </summary>    
    [<Parameter(Mandatory=true,ValueFromPipeline=true,ValueFromPipelineByPropertyName=true)>]    
    member val Path :string[] = Array.empty with get,set
    
    override this.BeginProcessing() =
        ()

    override this.ProcessRecord() =
        this.Path
        |> Array.map(fun p -> 
            let packageDefinitionSms = DriverTool.Library.PackageDefinitionSms.readFromFileUnsafe p
            this.WriteObject(packageDefinitionSms)
            )
        |> ignore
        ()
    
    override this.EndProcessing() =
        ()