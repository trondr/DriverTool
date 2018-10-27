function Register-Application
{
    param(
        [string]
        $InstallRevision="000"
    )

    Trace-FunctionCall -Script {
        $registryPath = "$(Get-ApplicationRegistryPath)".Replace("HKLM\","HKLM:\")
        $name = "InstallRevision"
        $value = $InstallRevision
        if(!(Test-Path $registryPath))
        {
            New-Item -Path $registryPath -Force -ErrorAction Stop | Out-Null        
        }
        New-ItemProperty -Path $registryPath -Name $name -Value $value -PropertyType String -Force -ErrorAction Stop | Out-Null
    }
}
#TEST: Register-Application