
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
        Write-Log -Level DEBUG -Message "Value name '$ValueName' found in cache with value: '$cachedValue'"
    }
    else {
        Write-Log -Level DEBUG -Message "Value name '$ValueName' not found in cache, execute cache miss script." 
        $value = Invoke-Command $OnCacheMiss -ErrorAction Stop
        $cachedValue = $value
        if($($null -ne $cachedValue) -and $($cachedValues.ContainsKey($ValueName) -eq $false))
        {
            $cachedValues.Add($ValueName,$cachedValue)
            Write-Log -Level DEBUG -Message "Value name '$ValueName' and value '$cachedValue' has been added to cache."
        }  
    }
    return $cachedValue
}
# TEST:
# $global:VerbosePreference = "Continue"
#Get-CacedValue -ValueName "testValueName" -OnCacheMiss { "DefaultTestValue"} 
