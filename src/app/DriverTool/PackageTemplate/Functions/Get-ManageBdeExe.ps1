function Get-ManageBdeExe{
    Trace-FunctionCall -Script{
        $ManageBdeExe = [System.IO.Path]::Combine($(Get-SystemFolder),"manage-bde.exe")
        Assert-FileExists -FileName $ManageBdeExe -Message "Utility for managing BitLocker not found."
        $ManageBdeExe
    }
}
#TEST: Get-ManageBdeExe