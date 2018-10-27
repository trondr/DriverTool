function Initialize-Logging {
    Get-LogFile | Out-Null
    $global:LoggingIsInitialized = $true
}