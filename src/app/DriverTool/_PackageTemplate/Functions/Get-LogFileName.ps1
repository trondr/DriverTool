function Get-LogFileName
{
    Trace-FunctionCall -Script {
        $logFileName = $(Get-InstallProperty -PropertyName LogFileName -ExpandEnvironmentVariables)
        if($null -eq $logFileName)
        {
            Write-Log -Level DEBUG -Message "Getting default log file name."
            $logFileName = "DriverPackageInstall.log"
        }
        return $logFileName
    }
}
#TEST: 
# $global:VerbosePreference = "Continue"
# Get-LogFileName
