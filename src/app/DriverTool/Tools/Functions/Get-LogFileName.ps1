function Get-LogFileName
{
    Write-Verbose "Get-LogFileName..."
    $logFileName = [System.Environment]::ExpandEnvironmentVariables($(Get-InstallProperty -PropertyName LogFileName))
    if($null -eq $logFileName)
    {
        $logFileName = "DriverPackageInstall.log"
    }
    Write-Verbose "Get-LogFileName->$logFileName"
    return $logFileName
}
#TEST: 
# $global:VerbosePreference = "Continue"
# Get-LogFileName
