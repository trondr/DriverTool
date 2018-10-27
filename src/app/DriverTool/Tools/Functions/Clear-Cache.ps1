function Clear-Cache {
    $cachedValues = Get-Cache
    $cachedValues.Clear()
    $cachedValues
}