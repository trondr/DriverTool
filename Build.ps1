<# Build Powershell Module #>
param(
    [Parameter(Mandatory=$false,ValueFromRemainingArguments=$true)]
    [ValidateSet("Default")]
    [string]
    $BuildTarget="Default"
)
fake run "Build.fsx" target "$BuildTarget"

