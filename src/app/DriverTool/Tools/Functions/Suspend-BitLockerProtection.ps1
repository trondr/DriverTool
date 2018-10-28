function Suspend-BitLockerProtection
{
    Trace-FunctionCall -Script {
        Assert-IsAdministrator -Message "Administrative privileges are required to suspend BitLocker."      
        $managedBdeExitCode = Start-ConsoleProcess -FilePath "$(Get-ManageBdeExe)" -CommandArguments "-protectors -disable C:"
        Start-ConsoleProcess -FilePath "$(Get-SchTasksExe)" -CommandArguments "/Delete /tn `"$(Get-DriverToolResumeBitLockerProtectionScheduledTaskName)`"" | Out-Null
        Start-ConsoleProcess -FilePath "$(Get-SchTasksExe)" -CommandArguments "/Create /tn `"$(Get-DriverToolResumeBitLockerProtectionScheduledTaskName)`" /XML `"$(Get-DriverToolResumeBitLockerProtectionScheduledTaskXml)`"" | Out-Null
        $managedBdeExitCode
    }
}
#TEST:Suspend-BitLockerProtection















