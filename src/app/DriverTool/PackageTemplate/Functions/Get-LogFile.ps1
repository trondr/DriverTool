function Get-LogFile
{
    Write-Verbose "Get-LogFile..."
    $logFile = Get-CacedValue -ValueName LogFile -OnCacheMiss {
        $logF = [System.IO.Path]::Combine($(Get-LogDirectory),$(Get-LogFileName))
        $logF
    }
    Write-Verbose "Get-LogFile->$logFile"
    $logFile
}
#TEST: Get-LogFile