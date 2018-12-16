function Get-SchTasksExe{
    Trace-FunctionCall -Script{
        $SchTasksExe = [System.IO.Path]::Combine($(Get-SystemFolder),"schtasks.exe")
        Assert-FileExists -FileName $SchTasksExe -Message "Utility for managing scheduled tasks not found."
        $SchTasksExe
    }
}
#TEST: Get-SchTasksExe