function Get-ScriptFolder
{
    Write-Verbose "Get-ScriptFolder..."    
    if((Test-Path variable:global:scriptFolder) -and ([string]::IsNullOrWhiteSpace($global:scriptFolder) -eq $false))
    {
        $scriptFolder = $global:scriptFolder
    }
    else {
        throw "Global scriptFolder variable has not been set."
    }
    Write-Verbose "Get-ScriptFolder->$scriptFolder"
    return $scriptFolder
}
#TEST: Get-ScriptFolder