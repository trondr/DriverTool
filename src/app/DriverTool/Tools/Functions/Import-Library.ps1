

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