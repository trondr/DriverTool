
function Get-CacedValue {
    param (
        [Parameter(Mandatory=$true)]
        [string]
        $ValueName,
        [Parameter(Mandatory=$true)]
        [scriptblock]
        $OnCacheMiss
    )
    $cachedValues = Get-Cache
    if($cachedValues.ContainsKey($ValueName))
    {
        $cachedValue = $cachedValues[$ValueName]
        Write-Verbose "Value found in cache: $cachedValue"
    }
    else {
        Write-Verbose "$ValueName value not found in cache, execute cache miss script." 
        $value = Invoke-Command $OnCacheMiss
        $cachedValues.Add($ValueName,$value)
        $cachedValue = $value
        Write-Verbose "$ValueName value '$cachedValue' has been added to cache."   
    }
    return $cachedValue
}
# TEST:
# $global:VerbosePreference = "Continue"
#Get-CacedValue -ValueName "testValueName" -OnCacheMiss { "DefaultTestValue"} 
