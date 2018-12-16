function UnRegister-Application
{    
    Trace-FunctionCall -Script {
        $registryPath = "$(Get-ApplicationRegistryPath)".Replace("HKLM\","HKLM:\")       
        if((Test-Path $registryPath))
        {
            Remove-Item -Path $registryPath -Force | Out-Null
        }
    }
}
#TEST: UnRegister-Application