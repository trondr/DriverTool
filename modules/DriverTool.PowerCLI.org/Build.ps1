<# Build Powershell Module #>
param(
    [Parameter(Mandatory=$false,ValueFromRemainingArguments=$true)]
    [ValidateSet("BuildModule","BuildTemplate","Default")]
    [string]
    $BuildTarget="Default"
)

If($BuildTarget -eq "BuildModule")
{
    fake run "Build.fsx" target "BuildModule"
}
ElseIf($BuildTarget -eq "BuildTemplate")
{
    . fake run "Build.fsx" target "BuildTemplate"
    Remove-Item ([System.IO.Path]::Combine($ScriptRoot,"build.fsx.lock"))
    Remove-Item ([System.IO.Path]::Combine($ScriptRoot,".fake")) -Recurse    
}
Else
{
    fake run Build.fsx
}
