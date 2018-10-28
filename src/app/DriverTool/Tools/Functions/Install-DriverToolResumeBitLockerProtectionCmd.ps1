function Install-DriverToolResumeBitLockerProtectionCmd
{
    Trace-FunctionCall -Script {
        $DestinationDriverToolResumeBitLockerProtectionCmd = $(Get-DestinationDriverToolResumeBitLockerProtectionCmd)
        [System.IO.File]::Copy("$(Get-SourceDriverToolResumeBitLockerProtectionCmd)","$($DestinationDriverToolResumeBitLockerProtectionCmd)",$true)
        $DestinationDriverToolResumeBitLockerProtectionCmd
    }
}
#TEST: Install-DriverToolResumeBitLockerProtectionCmd