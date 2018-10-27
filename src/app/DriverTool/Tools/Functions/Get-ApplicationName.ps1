function Get-ApplicationName
{
    Trace-FunctionCall -Script{
        Get-CacedValue -ValueName "ApplicationName" -OnCacheMiss {
            Get-InstallProperty -PropertyName "PackageName"
        }
    }
}
#Get-ApplicationName