function Get-ScriptFolder
{
    Trace-FunctionCall -Script {
        if([string]::IsNullOrWhiteSpace($global:ScriptFolder))
        {
            Write-Verbose "Getting script folder from current directory (.)"
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
        return $scriptFolder
    }
}
#TEST: Get-ScriptFolder