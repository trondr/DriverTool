function Get-DestinationDriverToolResumeBitLockerProtectionCmd
{
    Trace-FunctionCall -Script{
        [System.IO.Path]::Combine("$(Get-DriverToolProgramDataFolder)","$(Get-DriverToolResumeBitLockerProtectionCmdFileName)")
    }
}
#TEST: Get-DestinationDriverToolResumeBitLockerProtectionCmd