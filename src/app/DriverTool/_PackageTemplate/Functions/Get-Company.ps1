function Get-CompanyName
{
    Trace-FunctionCall -Script {
        Get-CacedValue -ValueName CompanyName -OnCacheMiss {
            Get-InstallProperty -PropertyName Publisher
        }
    }
}
#TEST: 
#Clear-Cache
#Get-CompanyName