function UnInstall-DriverToolResumeBitLockerProtectionCmd
{
    Trace-FunctionCall -Script {
        $DestinationDriverToolResumeBitLockerProtectionCmd = $(Get-DestinationDriverToolResumeBitLockerProtectionCmd)
        if($(Test-Path "$($DestinationDriverToolResumeBitLockerProtectionCmd)") -eq $true)
        {
            [System.IO.File]::Delete("$($DestinationDriverToolResumeBitLockerProtectionCmd)")
        }
        $DestinationDriverToolResumeBitLockerProtectionCmd
    }
}
#TEST: UnInstall-DriverToolResumeBitLockerProtectionCmd