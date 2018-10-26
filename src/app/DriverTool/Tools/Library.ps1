
# User specified functions for use in the Install.ps1 can be defined in this file.

###############################################################################
#   Start: Library Script - Do not change
###############################################################################
#   Loading functions
###############################################################################
$functionsFolder = [System.IO.Path]::Combine($(Get-ScriptFolder),"Functions")
$functionScripts = [System.IO.Directory]::GetFiles($functionsFolder,"*.ps1")
$functionScripts | ForEach-Object{
    Write-Verbose "Loading function script '$($_)'..."
    . $_
    If($? -eq $false)
    {
        Write-Host -ForegroundColor Red "Failed to load function script '$($_)' Error: $($error[0])"
        EXIT 1
    }
}
###############################################################################
#   End: Library Script - Do not change
###############################################################################