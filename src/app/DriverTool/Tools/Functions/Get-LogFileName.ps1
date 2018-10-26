function Get-LogFileName
{
    Write-Verbose "Get-LogFileName..."
    $installXml = [xml] $(Get-Content -Path "$(Get-InstallXml)")
    $logFileName = [System.Environment]::ExpandEnvironmentVariables($($installXml.configuration.LogFileName))
    if($null -eq $logFileName)
    {
        $logFileName = "DriverPackageInstall.log"
    }
    Write-Verbose "Get-LogFileName->$logFileName"
    return $logFileName
}
#TEST: Get-LogFileName
