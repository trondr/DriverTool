#source: https://stackoverflow.com/questions/26652204/powershell-assembly-binding-redirect-not-found-in-application-configuration-fi

if (!("Redirector" -as [type]))
{
    $source = 
@'
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    public class Redirector
    {
        public readonly string[] ExcludeList;

        public Redirector(string[] ExcludeList = null)
        {
            this.ExcludeList  = ExcludeList;
            this.EventHandler = new ResolveEventHandler(AssemblyResolve);
        }

        public readonly ResolveEventHandler EventHandler;

        protected Assembly AssemblyResolve(object sender, ResolveEventArgs resolveEventArgs)
        {
            //Console.WriteLine("Attempting to resolve: " + resolveEventArgs.Name); // remove this after its verified to work
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var pattern  = "PublicKeyToken=(.*)$";
                var info     = assembly.GetName();
                var included = ExcludeList == null || !ExcludeList.Contains(resolveEventArgs.Name.Split(',')[0], StringComparer.InvariantCultureIgnoreCase);

                if (included && resolveEventArgs.Name.StartsWith(info.Name, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (Regex.IsMatch(info.FullName, pattern))
                    {
                        var Matches        = Regex.Matches(info.FullName, pattern);
                        var publicKeyToken = Matches[0].Groups[1];

                        if (resolveEventArgs.Name.EndsWith("PublicKeyToken=" + publicKeyToken, StringComparison.InvariantCultureIgnoreCase))
                        {
                            //Console.WriteLine("Redirecting lib to: " + info.FullName); // remove this after its verified to work
                            return assembly;
                        }
                    }
                }
            }

            return null;
        }
    }
'@

    $type = Add-Type -TypeDefinition $source -PassThru 
}

#exclude all powershell related stuff, not sure this strictly necessary
$redirectExcludes = 
    @(
        "System.Management.Automation", 
        "Microsoft.PowerShell.Commands.Utility",
        "Microsoft.PowerShell.Commands.Management",
        "Microsoft.PowerShell.Security",
        "Microsoft.WSMan.Management",    
        "Microsoft.PowerShell.ConsoleHost",
        "Microsoft.Management.Infrastructure",
        "Microsoft.Powershell.PSReadline",
        "Microsoft.PowerShell.GraphicalHost"
        "System.Management.Automation.HostUtilities",

        "System.Management.Automation.resources",
        "Microsoft.PowerShell.Commands.Management.resources",
        "Microsoft.PowerShell.Commands.Utility.resources",
        "Microsoft.PowerShell.Security.resources",
        "Microsoft.WSMan.Management.resources",
        "Microsoft.PowerShell.ConsoleHost.resources",
        "Microsoft.Management.Infrastructure.resources",
        "Microsoft.Powershell.PSReadline.resources",
        "Microsoft.PowerShell.GraphicalHost.resources",
        "System.Management.Automation.HostUtilities.resources"
    )
try
{
    $redirector = [Redirector]::new($redirectExcludes)
    [System.AppDomain]::CurrentDomain.add_AssemblyResolve($redirector.EventHandler)
}
catch
{
    #.net core uses a different redirect method
    Write-Warning "Unable to register assembly redirect(s). Are you on ARM (.Net Core)?"
}



function Import-Library {
    [CmdletBinding()]
    param (
        # Path to .NET assembly (*.dll, *.exe)
        [Parameter(Position=0,Mandatory=$true,ValueFromPipeline=$true,ValueFromPipelineByPropertyName=$true)]
        [ValidateScript({$(Test-Path -Path $_)})]
        [string[]]
        $Path
    )
    
    begin {
    }
    
    process {
        foreach($p in $Path)
        {
            try {
                Get-CacedValue -ValueName $p -OnCacheMiss {
                    $a = [System.Reflection.Assembly]::LoadFrom($p)
                    Write-Log -Level DEBUG -Message "Assembly path '$p' was successfully imported. Assembly: $($a.FullName)"
                    $a
                }    
            }
            catch {
                Write-Log -Level ERROR -Message "Exception Type: $($_.Exception.GetType().FullName)"
                Write-Log -Level ERROR -Message "Exception Message: $($_.Exception.Message)"
                Write-Error "Failed to import assembly '$p'" -ErrorAction $ErrorActionPreference
            }
        }
    }
    
    end {
    }
}

#TEST:
#Clear-Cache
#$global:VerbosePreference = "Continue"
#Import-Library -Path ".\Functions\Util\Script.Install.Tools.Library\Common.Logging.dll"
#Import-Library -Path ".\Functions\Util\Script.Install.Tools.Library\Script.Install.Tools.Library.dll"