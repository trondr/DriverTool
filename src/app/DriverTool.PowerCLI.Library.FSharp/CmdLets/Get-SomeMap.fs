namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation

/// <summary>
/// <para type="synopsis">Get Some map</para>
/// <para type="description">Get some map defined by "rectangular" area defined by minimum longitude/lattitude and maximum longitude/lattitude</para>
/// <example>
///     <code>Get-SomeMap -MinLongitude 10.1554 -MinLattitude 59.7368 -MaxLongitude 10.2276 -MaxLattitude 59.7448</code>
/// </example>
/// </summary>
[<Cmdlet(VerbsCommon.Get,"SomeMap")>]
[<OutputType(typeof<string>)>]
type GetSomeMapCommand () =
    inherit PSCmdlet ()
    
    /// <summary>
    /// <para type="description">Minimum longitude of the map.</para>
    /// </summary>
    [<Parameter(Mandatory=true)>]
    member val MinLongitude :float = 0.0 with get,set
    
    /// <summary>
    /// <para type="description">Minimum lattitude of the map.</para>
    /// </summary>
    [<Parameter(Mandatory=true)>]
    member val MinLattitude :float = 0.0 with get,set
    
    /// <summary>
    /// <para type="description">Maximum longitude of the map.</para>
    /// </summary>
    [<Parameter(Mandatory=true)>]
    member val MaxLongitude :float = 0.0 with get,set
    
    /// <summary>
    /// <para type="description">Maximum lattitude of the map.</para>
    /// </summary>
    [<Parameter(Mandatory=true)>]
    member val MaxLattitude :float = 0.0 with get,set        

    override this.ProcessRecord() =
        let someMap = "Some downloaded map."
        this.WriteObject(someMap)
        ()
