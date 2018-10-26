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
    Write-Log -Level INFO "Initializing..."
    return $exitCode
}

function Install
{
    $exitCode = 0
    Write-Log -Level INFO "Installling..."
    return $exitCode
}

function UnInstall
{
    $exitCode = 0
    Write-Log -Level INFO "UnInstalling..."
    return $exitCode
}

###############################################################################
#
#   Logging preference
#
###############################################################################
$global:VerbosePreference = "Continue"
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
$global:scriptFolder = Split-Path -parent $script
###############################################################################
#   Loading functions
###############################################################################
$functionsFolder = [System.IO.Path]::Combine($($global:scriptFolder),"Functions")
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
#   Parse action
###############################################################################
switch($Action)
{
    "Install"   { $actionScriptBlock = [scriptblock]$function:Install }
    "UnInstall" { $actionScriptBlock = [scriptblock]$function:UnInstall }
    default { 
        Write-Log -Level ERROR "Unknown action: $Action"
        EXIT 1
    }
}
###############################################################################
#   Executing action
###############################################################################
Write-Verbose "Action=$action"
Write-Log -Level INFO "Executing Init action..."
$exitCode = Invoke-InstallAction([scriptblock]$function:Init)
if($exitCode -eq 0)
{
    Write-Log -Level INFO "Executing $action action..."
    $exitCode = Invoke-InstallAction([scriptblock]$actionScriptBlock)
}
else
{
    Write-Log -Level ERROR  "Init() function failed with error code: $exitCode"
}
Write-Log -Level INFO "Finished executing Install.ps1. Exit code: $exitCode"
EXIT $exitCode
###############################################################################
#
#   End: Main Script - Do not change
#
###############################################################################
