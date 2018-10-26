function Get-RobocopyExe
{
    Write-Verbose "Get-RobocopyExe..."
    $RobocopyExe = [System.IO.Path]::Combine("$(Get-SystemFolder)","Robocopy.exe")
    Assert-FileExists -FileName "$($RobocopyExe)" -Message "Robocopy.exe not found."
    Write-Verbose "Get-RobocopyExe->$RobocopyExe"
    return $RobocopyExe
}
#TEST: Get-RobocopyExe