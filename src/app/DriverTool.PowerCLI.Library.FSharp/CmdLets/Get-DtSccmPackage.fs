namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Collections
open System.Collections.Generic
open System.Management.Automation
open System.Management.Automation.Language


type ManufacturerCompleter () =
    interface IArgumentCompleter with
        member this.CompleteArgument(commandName:string,parameterName:string,wordToComplete:string,commandAst:CommandAst,fakeBoundParameters:IDictionary) =
            //TODO: Get manufacturer list from from DriverTool.Library.Manufacturer type
            ["Dell1";"HP1";"Lenovo1"]
            |>Seq.filter(fun m -> (new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase)).IsMatch(m))
            |>Seq.map(fun m -> new CompletionResult(m))

//TODO: Implement ModelCompleter to returning model codes for specified Manufacturer. Manufacturer should be available via fakeBoundParameters.

/// <summary>
/// <para type="synopsis">Get Some map</para>
/// <para type="description">Get some map defined by "rectangular" area defined by minimum longitude/lattitude and maximum longitude/lattitude</para>
/// <example>
///     <code>Get-SomeMap -MinLongitude 10.1554 -MinLattitude 59.7368 -MaxLongitude 10.2276 -MaxLattitude 59.7448</code>
/// </example>
/// </summary>
[<Cmdlet(VerbsCommon.Get,"DtSccmPackage")>]
[<OutputType(typeof<string>)>]
type GetDtSccmPackage () =
    inherit PSCmdlet ()
    
    /// <summary>
    /// <para type="description">Minimum longitude of the map.</para>
    /// </summary>
    [<Parameter(Mandatory=true)>]
    [<ArgumentCompleter(typeof<ManufacturerCompleter>)>]
    member val Manufacturer :string = System.String.Empty with get,set
    
    override this.ProcessRecord() =
        let some = "Manufacturer: " + this.Manufacturer
        this.WriteObject(some)
        ()

    interface IDynamicParameters with
        member this.GetDynamicParameters() =
            //TODO: Add Model parameter if Manufacturer has been defined.
            //TODO: Add OS parameter if Model has been defined.
            //TODO: Add OSBuild parameter if OS has been defined.
            null

    