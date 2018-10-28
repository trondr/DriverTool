function Get-SourceDriverToolResumeBitLockerProtectionCmd
{
    Trace-FunctionCall -Script{
        $SourceDriverToolResumeBitLockerProtectionCmd = [System.IO.Path]::Combine($(Get-FunctionsUtilFolder),"Resume BitLocker",$(Get-DriverToolResumeBitLockerProtectionCmdFileName))
        Assert-FileExists -FileName "$SourceDriverToolResumeBitLockerProtectionCmd" -Message "'$(Get-DriverToolResumeBitLockerProtectionCmdFileName)' not found."
        $SourceDriverToolResumeBitLockerProtectionCmd
    }
}
#TEST: Get-SourceDriverToolResumeBitLockerProtectionCmd