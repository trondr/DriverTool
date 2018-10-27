function Invoke-InstallAction 
{
    param (
        [Parameter(Mandatory=$true)]
        [scriptblock]
        $ActionScriptBlock
    )
    Trace-FunctionCall -Script {
        if(Test-IsTerminalServer -and Test-IsAdministrator) { Invoke-ChangeUserInstall }
        
        $exitCode = 0
        try {
            $exitCode = [int]$(& $ActionScriptBlock)
        }
        catch {
            Write-Log -Level ERROR -Message "Something went wrong when invoking an install action. All install actions must return an integer exit code. The install action failed with: $($PSItem.Exception.Message)"
            Write-Log -Level ERROR -Message "Script: $($PSItem.InvocationInfo.ScriptName)"
            Write-Log -Level ERROR -Message "$($PSItem.InvocationInfo.PositionMessage)"
            $exitCode = 1
        }
        
        if(Test-IsTerminalServer -and Test-IsAdministrator) { Invoke-ChangeUserExecute }
        return $exitCode
    }
}
#TEST: $global:VerbosePreference = "Continue"
#TEST: Invoke-InstallAction -ActionScriptBlock {Get-ChildItem -Path "c:\temp"|Out-Null;"test"}