Write-Verbose "Initializing module: DriverTool.PowerCLI"
function Get-ScriptFolder {
    #Source: https://github.com/PowerShell/vscode-powershell/issues/633
    [CmdletBinding()] 
    param()
    if ( $script:PSScriptRoot ) {
        $script:PSScriptRoot
    } elseif ( $script:psEditor ) {
        $script:psEditor.GetEditorContext().CurrentFile.Path | Split-Path -Parent
    } elseif ( $script:psISE ) {
        $script:psISE.CurrentFile.FullPath | Split-Path -Parent
    } else {
        Write-Error 'Failed to resolve or replace $PSScriptRoot.'
        #Get-PSCallStack | Select-object -First 1 -ExpandProperty ScriptName | Split-Path -Parent
    }
}
$global:ModuleRootPath = Get-ScriptFolder
$VerbosePreference = "SilentlyContinue"
$allFunctions = @()
# Perform Actions before loading the rest of the content
$allFunctions += [System.IO.FileInfo]"$ModuleRootPath\internal\scripts\preimport.ps1"
$filters = @()
$filters += [System.IO.Path]::Combine($($global:ModuleRootPath),"public","classes","*.ps1")
$filters += [System.IO.Path]::Combine($($global:ModuleRootPath),"public","functions","*.ps1")
$filters += [System.IO.Path]::Combine($($global:ModuleRootPath),"internal","functions","*.ps1")
$allFunctions += ($filters | Foreach-Object {
    Get-ChildItem -Path "$($_)" -File -Recurse -ErrorAction SilentlyContinue
})
# Perform Actions after loading the module contents
$allFunctions += [System.IO.FileInfo]([System.IO.Path]::Combine($($global:ModuleRootPath),"binary","DriverTool.PowerCLI.Library.CSharp","DriverTool.PowerCLI.Library.CSharp.dll"))
$allFunctions += [System.IO.FileInfo]([System.IO.Path]::Combine($($global:ModuleRootPath),"binary","DriverTool.PowerCLI.Library.FSharp","Fsharp.Core.dll"))
$allFunctions += [System.IO.FileInfo]([System.IO.Path]::Combine($($global:ModuleRootPath),"binary","DriverTool.PowerCLI.Library.FSharp","DriverTool.PowerCLI.Library.FSharp.dll"))
$allFunctions += [System.IO.FileInfo]"$ModuleRootPath\internal\scripts\postimport.ps1"
Write-Verbose "Importing # functions and libraries: $($allFunctions.Length)"
$allFunctions | ForEach-Object {
    $moduleFile = $_.FullName
    try {
        Write-Verbose "Importing: '$moduleFile'"
        Import-Module $moduleFile
    }
    catch {
        Write-Error -Message "Failed to import: '$moduleFile' due to $($_.Exception.Message)"
    }
}