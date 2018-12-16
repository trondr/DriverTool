function Get-Cache {
    if($(Test-Path variable:global:cachedValues) -and ($null -ne $cachedValues))
    {
        $cachedValues = $global:cachedValues
    }
    else {
        $global:cachedValues = New-Object 'system.collections.generic.dictionary[string,object]'
        $cachedValues = $global:cachedValues
    }
    return $cachedValues
}
#TEST
# $cachedValues = Get-Cache
# $cachedValues.GetType().Name