function Invoke-ChangeUserInstall
{
    Trace-FunctionCall -Script {
        $exitCode = Start-ConsoleProcess -FilePath "$(Get-ChangeExe)" -Arguments "User /Install"
        return $process.ExitCode
    }
}
#TEST: Invoke-ChangeUserInstall