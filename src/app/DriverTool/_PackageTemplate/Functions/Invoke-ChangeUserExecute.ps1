function Invoke-ChangeUserExecute
{
    Trace-FunctionCall -Script {
        $exitCode = Start-ConsoleProcess -FilePath "$(Get-ChangeExe)" -Arguments "User /Execute"
        return $exitCode  
    }
}
#TEST: Invoke-ChangeUserExecute


