function Get-LogDirectoryFromInstallXml
{
    Write-Verbose "Get-LogDirectoryFromInstallXml..."
    $installXml = [xml] $(Get-Content -Path "$(Get-InstallXml)")
    $logDirectory = [System.Environment]::ExpandEnvironmentVariables($($installXml.configuration.LogDirectory))
    Write-Verbose "Get-LogDirectoryFromInstallXml->$logDirectory"
    return $logDirectory
}
#TEST: Get-LogDirectoryFromInstallXml