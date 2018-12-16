function Import-Log4NetLibrary
{
    Trace-FunctionCall -Script{
        $(Get-CacedValue -ValueName "Log4NetLibrary" -OnCacheMiss {
            #$assembly = [System.Reflection.Assembly]::LoadWithPartialName("System.Reflection.TypeExtensions")
            $assembly = Import-Library -Path $([System.IO.Path]::Combine($(Get-FunctionsFolder),"Util","Log4Net","log4net.dll"))
            Import-Library -Path $([System.IO.Path]::Combine($(Get-FunctionsFolder),"Util","Log4Net","Log4NetCMTrace.dll")) | Out-Null
            $assembly
        })
    }
}