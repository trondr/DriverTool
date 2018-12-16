function Get-RobocopyExe
{
    Trace-FunctionCall -Script {
    $RobocopyExe = [System.IO.Path]::Combine("$(Get-SystemFolder)","Robocopy.exe")
    Assert-FileExists -FileName "$($RobocopyExe)" -Message "Robocopy.exe not found."
    return $RobocopyExe
    }
}
#TEST: Get-RobocopyExe