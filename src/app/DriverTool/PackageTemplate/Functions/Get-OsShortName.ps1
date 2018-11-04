function Get-OsShortName
{
    Import-DriverToolUtilLibrary | Out-Null
    $obj = New-Object 'DriverTool.Util.Class1'
    Write-Host $obj.X
    Write-Log -Level ERROR -Message "Get-OsShortName : Not Implemented."
    "Win10X64"
}
#TEST: Get-OsShortName