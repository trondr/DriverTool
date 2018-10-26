function Invoke-ChangeUserExecute
{
    Write-Verbose "Invoke-ChangeUserExecute..."
    $exitCode = Start-ConsoleProcess -FilePath "$(Get-ChangeExe)" -Arguments "User /Execute"
    Write-Verbose "Invoke-ChangeUserExecute->$exitCode"
    return $exitCode  
}
#TEST: Invoke-ChangeUserExecute


