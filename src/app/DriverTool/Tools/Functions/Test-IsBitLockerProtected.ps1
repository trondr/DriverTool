function Test-IsBitLockerProtected
{
    Trace-FunctionCall -Script {
        $IsBitLockerProtected = $false
        try
        {
            Assert-IsAdministrator -Message "Administrative privileges are required to check BitLocker status."
            $volumeC = Get-WmiObject -Namespace "root\cimv2\security\MicrosoftVolumeEncryption" -Class "Win32_EncryptableVolume" -Filter "DriveLetter='C:'" -ErrorAction Stop
            if($volumeC.ProtectionStatus -eq "1")
            {
                $IsBitLockerProtected = $true
            }
            elseif ($volumeC.ProtectionStatus -eq "0") {
                $IsBitLockerProtected = $false
            }
        }
        catch
        {
            Write-Log -Level ERROR -Message "Failed to get BitLocker protection status. Exception: $($_.Exception.GetType().FullName). Error message: $($_.Exception.Message) Line: $($_.InvocationInfo.ScriptLineNumber) Script: $($_.InvocationInfo.ScriptName)"
        }
        return $IsBitLockerProtected
    }
    
}
#TEST: Test-IsBitLockerProtected