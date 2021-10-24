using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;  // Windows PowerShell assembly.

namespace DriverTool.PowerCLI.Library.CSharp.CmdLets
{
    /// <summary>
    /// <para type="synopsis">Send greeting</para>
    /// <para type="description">Send greeting to the pipe line.</para>
    /// <example>
    ///     <code>Send-Greeting</code>
    /// </example>
    /// </summary>
    [Cmdlet(VerbsCommunications.Send, "Greeting")]
    public class SendGreetingCommand : Cmdlet
    {
        /// <summary>
        /// <para type="description">Name to greet.</para>
        /// </summary>
        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        /// <summary>
        /// Override the ProcessRecord method to process
        /// the supplied user name and write out a
        /// greeting to the user by calling the WriteObject
        /// method.
        /// </summary>
        protected override void ProcessRecord()
        {
            WriteObject("Hello " + Name + "!");
        }
    }
}
