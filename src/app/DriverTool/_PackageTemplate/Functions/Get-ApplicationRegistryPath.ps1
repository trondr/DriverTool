function Get-ApplicationRegistryPath
{
    Trace-FunctionCall -Script {
        Get-CacedValue -ValueName ApplicationRegistryPath -OnCacheMiss {
            $registryPath = "HKLM\SOFTWARE\$(Get-CompanyName)\Applications\$(Get-ApplicationName)"
            $registryPath
        }
    }
}
#TEST: Get-ApplicationRegistryPath