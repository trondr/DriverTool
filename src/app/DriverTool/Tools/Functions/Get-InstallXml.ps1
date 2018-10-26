function Get-InstallXml
{
    Write-Verbose "Get-InstallXml..."
    $installXml = [System.IO.Path]::Combine("$(Get-ScriptFolder)","Install.xml")
    Write-Verbose "Get-InstallXml->$installXml"
    return $installXml
}
#TEST: Get-InstallXml