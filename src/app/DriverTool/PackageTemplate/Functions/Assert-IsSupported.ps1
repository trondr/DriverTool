function Assert-IsSupported
{
    Trace-FunctionCall -Script{
        
        $ComputerModel = $(Get-InstallProperty -PropertyName "ComputerModel")            
        $OsShortName = $(Get-InstallProperty -PropertyName "OsShortName")
        $CurrentComputerModel = $(Get-WmiObject Win32_ComputerSystem).Model
        $CurrentOsShortName = Get-OsShortName
        $IsSupportedComputerModel = ([string]$CurrentComputerModel).ToUpper().StartsWith(([string]$ComputerModel).ToUpper())
        Write-Log -Level INFO -Message "Is current computer model '$CurrentComputerModel' supported: $IsSupportedComputerModel"
        $IsSupportedOperatingSystem = ([string]$CurrentOsShortName).ToUpper().Equals(([string]$OsShortName).ToUpper())
        Write-Log -Level INFO -Message "Is current operating system '$CurrentOsShortName' supported: $IsSupportedOperatingSystem"
        $IsSupported = ($IsSupportedComputerModel -eq $true) -and ($IsSupportedOperatingSystem -eq $true)
        if($IsSupported -eq $false)
        {
            throw "This machine is not supported by this driver package. The driver package is supported on computer model '$($ComputerModel)*' and operating system '$OsShortName'"
        }
    }
}

