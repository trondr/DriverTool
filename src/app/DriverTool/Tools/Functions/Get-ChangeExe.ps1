


function Get-ChangeExe
{
    Write-Verbose "Get-ChangeExe"
    $changeExe = [System.IO.Path]::Combine($(Get-SystemFolder), "change.exe")
    Write-Verbose "Get-ChangeExe->$changeExe"
    return $changeExe
}
#TEST: Get-ChangeExe