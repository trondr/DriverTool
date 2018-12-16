function Resume-BitLockerProtection
{
    Trace-FunctionCall -Script {
        Assert-IsAdministrator -Message "Administrative privileges are required to suspend BitLocker."      
        $managedBdeExitCode = Start-ConsoleProcess -FilePath "$(Get-ManageBdeExe)" -CommandArguments "-protectors -enable C:"
        Start-ConsoleProcess -FilePath "$(Get-SchTasksExe)" -CommandArguments "/Delete /tn `"$(Get-DriverToolResumeBitLockerProtectionScheduledTaskName)`" /F" | Out-Null
        $managedBdeExitCode
    }
}
#TEST:Resume-BitLockerProtection