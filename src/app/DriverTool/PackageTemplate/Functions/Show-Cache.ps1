function Show-Cache {
    $cachedValues = Get-Cache
    Write-Log -Level INFO -Message "Content of Value Cache:"
    foreach($cachedValueKey in $cachedValues.Keys)
    {
        Write-Log -Level INFO -Message "$($cachedValueKey)=$($cachedValues[$cachedValueKey])"
    }
}
#TEST:
#$global:VerbosePreference = "SilentlyContinue"
#Show-Cache