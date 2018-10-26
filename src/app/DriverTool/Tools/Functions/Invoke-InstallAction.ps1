function Invoke-InstallAction 
{
    param (
        [Parameter(Mandatory=$true)]
        [scriptblock]
        $ActionScriptBlock
    )
    Write-Verbose "Invoke-InstallAction..."
    if(Test-IsTerminalServer -and Test-IsAdministrator) { Invoke-ChangeUserInstall }
    
    $exitCode = 0
    try {
        $exitCode = [int]$(& $ActionScriptBlock)
    }
    catch {
        Write-Host -ForegroundColor Red "Something went wrong when invoking an install action. All install actions must return an integer exit code. The install action failed with: $($PSItem.Exception.Message)"
        Write-Host -ForegroundColor Red "Script: $($PSItem.InvocationInfo.ScriptName)"
        Write-Host -ForegroundColor Red "$($PSItem.InvocationInfo.PositionMessage)"
        $exitCode = 1
    }
    
    if(Test-IsTerminalServer -and Test-IsAdministrator) { Invoke-ChangeUserExecute }
    Write-Verbose "Invoke-InstallAction->$exitCode"
    return $exitCode
}
#TEST: $global:VerbosePreference = "Continue"
#TEST: Invoke-InstallAction -ActionScriptBlock {Get-ChildItem -Path "c:\temp"|Out-Null;"test"}