function Get-InstallXml
{
    Write-Log -Level DEBUG -Message "Get-InstallXml..."
    $installXml = [System.IO.Path]::Combine("$(Get-ScriptFolder)","Install.xml")
    Write-Log -Level DEBUG -Message "Get-InstallXml->$installXml"
    return $installXml
}
#TEST: Get-InstallXml