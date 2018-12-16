function Get-OsShortName
{
    Trace-FunctionCall -Script {
        Get-CacedValue -ValueName "OsShortName" -OnCacheMiss {
            Import-DriverToolUtilLibrary | Out-Null
            [DriverTool.Util.OperatingSystemOperations]::GetOsShortName()
        }
    }
}
#TEST
#Clear-Cache
#$global:VerbosePreference = "Continue"
#Get-OsShortName