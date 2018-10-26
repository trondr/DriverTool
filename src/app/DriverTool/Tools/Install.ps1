Param(
    [Parameter(Position=0)]
    [ValidateNotNullOrEmpty()]
    [ValidateSet("Install","UnInstall")]
    [string]
    $Action=$(throw "Missing command line parameter. First parameter must be an action in the set: Install,UnInstall")
)

Set-PSDebug -Strict

function Init
{    
    $exitCode = 0
    Write-Host "Initializing..."
    return $exitCode
}

function Install
{
    $exitCode = 0
    Write-Host "Installling..."
    return $exitCode
}


function UnInstall
{
    $exitCode = 0
    Write-Host "UnInstalling..."
    return $exitCode
}

switch($Action)
{
    "Install"   { $actionScriptBlock = [scriptblock]$function:Install }
    "UnInstall" { $actionScriptBlock = [scriptblock]$function:UnInstall }
    default { 
        Write-Host "Unknown action: $Action" -ForegroundColor Red
        EXIT 1
    }
}
###############################################################################
#
#   Logging preference
#
###############################################################################
$global:VerbosePreference = "SilentlyContinue"
$global:DebugPreference = "SilentlyContinue"
$global:WarningPreference = "Continue"
$global:ErrorActionPreference = "Continue"
$global:ProgressPreference = "Continue"
###############################################################################
#
#   Start: Main Script - Do not change
#
###############################################################################
$global:script = $MyInvocation.MyCommand.Definition
function Get-ScriptFolder
{
    Write-Verbose "Get-ScriptFolder..."    
    $scriptFolder = Split-Path -parent $script
    Write-Verbose "ScriptFolder=$scriptFolder"
    return $scriptFolder
}
#TEST: Get-ScriptFolder

###############################################################################
#   Loading script library
###############################################################################
$scriptLibrary = [System.IO.Path]::Combine($(Get-ScriptFolder) ,"Library.ps1")
if((Test-Path $scriptLibrary) -eq $false)
{
    Write-Host -ForegroundColor Red "Script library '$scriptLibrary' not found."
    EXIT 1
}
Write-Verbose "ScriptLibrary=$scriptLibrary"
Write-Verbose "Loading script library '$scriptLibrary'..."
. $scriptLibrary
If ($? -eq $false) 
{ 
    Write-Host -ForegroundColor Red "Failed to load library '$scriptLibrary'. Error: $($error[0])"; break 
    EXIT 1
};
###############################################################################
#   Executing action
###############################################################################
Write-Verbose "Action=$action"
Write-Host "Executing Init action..."
$exitCode = Invoke-InstallAction([scriptblock]$function:Init)
if($exitCode -eq 0)
{
    Write-Host "Executing $action action..."
    $exitCode = Invoke-InstallAction([scriptblock]$actionScriptBlock)
}
else
{
    Write-Host -ForegroundColor Red "Init() function failed with error code: $exitCode"
}
Write-Host "Finished executing Install.ps1. Exit code: $exitCode"
EXIT $exitCode
###############################################################################
#
#   End: Main Script - Do not change
#
###############################################################################
