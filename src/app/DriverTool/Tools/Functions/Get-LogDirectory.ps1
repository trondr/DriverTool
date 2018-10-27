function Get-LogDirectory {
    Write-Log -Level DEBUG -Message "Get-LogDirectory..."
    $logDirectory = Get-CacedValue -ValueName LogDirectory -OnCacheMiss {
        $logDir = $(Get-InstallProperty -PropertyName LogDirectory -ExpandEnvironmentVariables)
        if($null -ne $logDir)
        {
            if($(Test-Path -Path $logDir) -eq $false)
            {
                Write-Log -Level DEBUG -Message "Creating log directory '$logDir'..."
                New-Item -ItemType Directory -Path $logDir -Force | Out-Null
            }
        }
        else {
            Write-Log -Level DEBUG -Message "Getting default log directory"
            $logDir = [System.IO.Path]::Combine([System.Environment]::ExpandEnvironmentVariables("%PUBLIC%"),"Logs")
            New-Item -ItemType Directory -Path $logDir -Force | Out-Null
        }
        $logDir
    }
    Write-Log -Level DEBUG -Message "Get-LogDirectory->$logDirectory"
    $logDirectory
}
#TEST: 
# Remove-Variable logDirectory
# $global:VerbosePreference = "Continue"
# Clear-Cache
# Get-LogDirectory