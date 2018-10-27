function Get-LogFileName
{
    Write-Verbose "Get-LogFileName..."
    $logFileName = $(Get-InstallProperty -PropertyName LogFileName -ExpandEnvironmentVariables)
    if($null -eq $logFileName)
    {
        Write-Verbose "Getting default log file name."
        $logFileName = "DriverPackageInstall.log"
    }
    Write-Verbose "Get-LogFileName->$logFileName"
    return $logFileName
}
#TEST: 
# $global:VerbosePreference = "Continue"
# Get-LogFileName
