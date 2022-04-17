namespace DriverTool.PowerCLI.Library.FSharp.CmdLets

open System.Management.Automation

module ConvertDtExceptionToMessage =

    /// <summary>
    /// <para type="synopsis">Convert exception object to message recursively.</para>
    /// <para type="description">Convert exception object to message recursing inner exceptions.</para>
    /// <example>
    ///     <code>Convert-DtExceptionToMessage -Exception $ex</code>
    /// </example>    
    /// </summary>
    [<Cmdlet(VerbsData.Convert,"DtExceptionToMessage")>]
    [<OutputType(typeof<string>)>]
    type ConvertDtExceptionToMessage () =
        inherit PSCmdlet ()
    
        /// <summary>
        /// <para type="description">Exception object</para>
        /// </summary>
        [<Parameter(Mandatory=true, ValueFromPipeline=true)>]
        member val Exception :System.Exception = null with get,set
        
        override this.BeginProcessing() =
            ()
    
        override this.ProcessRecord() =        
            let msg = DriverTool.Library.F0.toExceptionMessages this.Exception
            this.WriteObject(msg)
        
        override this.EndProcessing() =
            ()