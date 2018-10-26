function Get-SystemFolder
{
    Write-Verbose "Get-SystemFolder..."
    $systemFolder = [System.Environment]::GetFolderPath([System.Environment+SpecialFolder]::System)
    Write-Verbose "SystemFolder=$systemFolder"
    return $systemFolder
}
#TEST: Get-SystemFolder