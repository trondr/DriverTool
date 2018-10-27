function Get-InstallProperty {
    param (
        [Parameter(Mandatory=$true)]
        [ValidateSet("LogDirectory","LogFileName","Publisher","PackageName","PackageVersion")]
        [string]
        $PropertyName
    )
    Write-Verbose "Get-InstallProperty -PropertyName $PropertyName..."
    $installXml = [xml] $(Get-Content -Path "$(Get-InstallXml)")
    $propertyValue = [System.Environment]::ExpandEnvironmentVariables($($installXml.configuration.$PropertyName))
    Write-Verbose "Get-InstallProperty  -PropertyName $PropertyName->$propertyValue"
    return $propertyValue

}
<# TEST:
$global:VerbosePreference = "Continue"
Get-InstallProperty -PropertyName Publisher
Get-InstallProperty -PropertyName PackageName
Get-InstallProperty -PropertyName PackageVersion
Get-InstallProperty -PropertyName LogDirectory
Get-InstallProperty -PropertyName LogFileName
#>
