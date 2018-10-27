function Get-InstallProperty {
    param (
        [Parameter(Mandatory=$true)]
        [ValidateSet("LogDirectory","LogFileName","Publisher","PackageName","PackageVersion")]
        [string]
        $PropertyName,
        [switch]
        $ExpandEnvironmentVariables
    )
    Write-Verbose "Get-InstallProperty -PropertyName $PropertyName..."
    $installXml = [xml] $(Get-Content -Path "$(Get-InstallXml)")
    $unExpandandedPropertyValue = $($installXml.configuration.$PropertyName)
    if($ExpandEnvironmentVariables)
    {
        Write-Verbose "Expanding '$unExpandandedPropertyValue'..."
        $propertyValue = [System.Environment]::ExpandEnvironmentVariables($unExpandandedPropertyValue)
    }
    else 
    {
        $propertyValue = $unExpandandedPropertyValue
    }
    Write-Verbose "Get-InstallProperty  -PropertyName $PropertyName->$propertyValue"
    return $propertyValue
}
# TEST:
# $global:VerbosePreference = "Continue"
# Get-InstallProperty -PropertyName Publisher
# Get-InstallProperty -PropertyName PackageName
# Get-InstallProperty -PropertyName PackageVersion
# Get-InstallProperty -PropertyName LogDirectory
# Get-InstallProperty -PropertyName LogDirectory -ExpandEnvironmentVariables
# Get-InstallProperty -PropertyName LogFileName

