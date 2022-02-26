namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation
open System.Management.Automation.Language
open DriverTool.Library.PackageXml
open DriverTool.Library

/// <summary>
/// <para type="synopsis">Check if driver update is required (applicable and not allready installed) on the current system.</para>
/// <para type="description">Check if driver update is required (applicable and not allready installed) on the current system.</para>
/// <example>
///     <code>Test-DtIsDriverUpdateRequired</code>
/// </example>
/// </summary>
[<Cmdlet(VerbsDiagnostic.Test,"DtIsDriverUpdateRequired")>]
[<OutputType(typeof<string>)>]
type TestDtIsDriverUpdateRequired () =
    inherit PSCmdlet ()
    
    /// <summary>
    /// <para type="description">Driver update</para>
    /// </summary>    
    [<Parameter(Mandatory=true)>]    
    member val PackageInfo : PackageInfo = PackageInfo.Default with get, set

    /// <summary>
    /// <para type="description">All driver updates for current model. Must be provided to check for driver update dependencies.</para>
    /// </summary>
    [<Parameter(Mandatory=true)>]
    member val AllPackageInfos : PackageInfo[] = Array.empty with get, set
    
    override this.BeginProcessing() =
        ()

    override this.ProcessRecord() =
        let cacheFolderPath = resultToValueUnsafe (DriverTool.Library.FileSystem.path (DriverTool.Library.Configuration.getDownloadCacheDirectoryPath()))
        let currentManufacturer = resultToValueUnsafe (ManufacturerTypes.getManufacturerForCurrentSystem())
        let isDriverUpdateRequired = DriverTool.Updates.isDriverUpdateRequiredFunc currentManufacturer
        let isRequired = resultToValueUnsafe (isDriverUpdateRequired cacheFolderPath this.PackageInfo this.AllPackageInfos)
        this.WriteObject(isRequired)
            
    override this.EndProcessing() =
        ()