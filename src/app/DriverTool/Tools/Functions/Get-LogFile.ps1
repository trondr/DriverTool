function Get-LogFile 
{
    Write-Verbose "Get-LogFile..."
    if((Test-Path variable:global:logFile) -and ($null -ne $logFile))
    {
        $global:logFile = $global:logFile
    }
    else {
        $global:logFile = [System.IO.Path]::Combine($(Get-LogDirectory),$(Get-LogFileName))    
    }
    
    Write-Verbose "Get-LogFile->$logFile"
    return $global:logFile
}
#TEST: Get-LogFile