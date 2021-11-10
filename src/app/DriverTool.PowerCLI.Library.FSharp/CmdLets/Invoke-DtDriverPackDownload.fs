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
        
        let reportProgress activity status currentOperation (percentageComplete:float option) isBusy (id:int option) = 
            let progressRecord = new ProgressRecord((match id with |Some i -> i|None -> 0),activity,status)
            progressRecord.CurrentOperation <- currentOperation
            progressRecord.PercentComplete <- match percentageComplete with |Some p->(System.Convert.ToInt32(p)) |None -> 0
            if(isBusy)then
                progressRecord.RecordType <- ProgressRecordType.Processing
            else
                progressRecord.RecordType <- ProgressRecordType.Completed                        
            //TODO: Calling WriteProgress must be done on the main thread. Create a solution for this. Maybe this one: https://stackoverflow.com/questions/12852494/best-way-to-update-cmdlet-progress-from-a-separate-thread
            //this.WriteProgress(progressRecord)
            ()
        let reportProgressStdOut =
            DriverTool.Library.Logging.reportProgressStdOut'

        this.DriverPack
        |> Array.map(fun dp ->        
                let ddp = InvokeDtDownloadDriverPack.downloadDriverPackInfo reportProgress dp                
                this.WriteObject(ddp)
                )
        |> ignore
        ()
    
    override this.EndProcessing() =
        ()