function Import-IniFileParserLibrary
{
    Trace-FunctionCall -Script{
        $(Get-CacedValue -ValueName "INIFileParserLibrary" -OnCacheMiss {
            $assembly = Import-Library -Path $([System.IO.Path]::Combine($(Get-FunctionsFolder),"Util","INIFileParser","INIFileParser.dll"))
            $assembly
        })
    }
}
