function Suspend-BitLockerProtection
{
    Trace-FunctionCall -Script {
        Assert-IsAdministrator -Message "Administrative privileges are required to suspend BitLocker."      
        Start-ConsoleProcess -FilePath "$(Get-ManageBdeExe)" -CommandArguments "-protectors -disable C:"
        Start-ConsoleProcess -FilePath "$(Get-SchTasksExe)" -CommandArguments "/Create /tn "DriverTool Resume BitLocker Protection" /XML ´"$(Get-DriverToolResumeBitLockerProtectionXml)´""
    }
}
#TEST:Suspend-BitLockerProtection

