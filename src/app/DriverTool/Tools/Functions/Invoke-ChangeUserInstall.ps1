function Invoke-ChangeUserInstall
{
    Write-Verbose "Invoke-ChangeUserInstall..."
    $exitCode = Start-ConsoleProcess -FilePath "$(Get-ChangeExe)" -Arguments "User /Install"
    Write-Verbose "Invoke-ChangeUserInstall->$exitCode"
    return $process.ExitCode
}
#TEST: Invoke-ChangeUserInstall