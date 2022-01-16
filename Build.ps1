<# Build Powershell Module #>
param(
    [Parameter(Mandatory=$false,ValueFromRemainingArguments=$true)]
    [ValidateSet("Default","UpdateVersion")]
    [string]
    $BuildTarget="Default",
    [Parameter(Mandatory=$false)]
    [int]
    $AddDaysToBuildVersion = 0
)
if($BuildTarget -eq "Default")
{
    fake run "Build.fsx" target "$BuildTarget"
    Invoke-psake -taskList $BuildTarget
}
elseif($BuildTarget -eq "UpdateVersion") {    
    Invoke-psake -taskList $BuildTarget -parameters @{ 'AddDaysToBuildVersion'=$AddDaysToBuildVersion; }
}
else {
    fake run "Build.fsx" target "$BuildTarget"
    Invoke-psake -taskList $BuildTarget
}


