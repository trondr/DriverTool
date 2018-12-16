function Get-ScriptFolder
{
    Write-Log -Level DEBUG -Message "Get-ScriptFolder..."
    if([string]::IsNullOrWhiteSpace($global:ScriptFolder))
    {
        Write-Log -Level DEBUG -Message "Getting script folder from current directory (.)"
        $global:ScriptFolder = [System.IO.Path]::GetFullPath(".")
        $scriptFolder = $global:scriptFolder
    } 
    if((Test-Path variable:global:scriptFolder) -and ([string]::IsNullOrWhiteSpace($global:scriptFolder) -eq $false))
    {
        $scriptFolder = $global:scriptFolder
    }
    else {
        throw "Global scriptFolder variable has not been set."
    }
    Write-Log -Level DEBUG -Message "Get-ScriptFolder->$scriptFolder"
    return $scriptFolder
}
#TEST: Get-ScriptFolder