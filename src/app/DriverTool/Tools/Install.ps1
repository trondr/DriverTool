Param(
    [Parameter(Position=0,Mandatory=$true,HelpMessage="Action must be in the set: Install,UnInstall,CompressDrivers,ExpandDrivers")]
    [ValidateNotNullOrEmpty()]
    [ValidateSet("Install","UnInstall","CompressDrivers","ExpandDrivers")]    
    [string]    
    $Action
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
    Assert-IsSupported
    Assert-IsAdministrator -Message "Administrative privileges are required to run install."
    UnRegister-Application
    $exitCode = Suspend-BitLockerProtection
    $exitCode = Copy-Drivers
    #$exitCode = Install-Drivers
    Reset-ConfigFlags | Out-Null    
    Register-Application
    return $exitCode
}

function UnInstall
{
    $exitCode = 0
    Write-Log -Level INFO "UnInstalling..."
    Assert-IsSupported
    Assert-IsAdministrator -Message "Administrative privileges are required to run uninstall."
    UnRegister-Application
    Remove-DriverToolProgramDataFolder |Out-Null   
    $exitCode = Resume-BitLockerProtection
    return $exitCode
}

function CompressDrivers
{
    $exitCode = Compress-Drivers
    $exitCode
}

function ExpandDrivers
{
    $exitCode = Expand-Drivers
    $exitCode
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
Clear-Cache
Initialize-Logging
###############################################################################
#   Parse action
###############################################################################
switch($Action)
{
    "Install"   { $actionScriptBlock = [scriptblock]$function:Install }
    "UnInstall" { $actionScriptBlock = [scriptblock]$function:UnInstall }
    "CompressDrivers" {$actionScriptBlock = [scriptblock]$function:CompressDrivers}
    "ExpandDrivers" {$actionScriptBlock = [scriptblock]$function:ExpandDrivers}
    default { 
        Write-Log -Level ERROR "Unknown action: $Action"
        EXIT 1
    }
}
###############################################################################
#   Executing action
###############################################################################
Write-Log -Level DEBUG -Message "Action=$action"
Write-Log -Level INFO "Executing Init action..."
$exitCode = Invoke-InstallAction -ActionScriptBlock $([scriptblock]$function:Init)
if($exitCode -eq 0)
{
    Write-Log -Level INFO "Executing $action action..."
    $exitCode = Invoke-InstallAction -ActionScriptBlock $([scriptblock]$actionScriptBlock)
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
