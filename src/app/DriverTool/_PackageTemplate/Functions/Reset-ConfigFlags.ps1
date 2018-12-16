# Credits: https://social.technet.microsoft.com/Forums/systemcenter/en-US/7173099f-a498-40a4-ba2b-53e7e8c8379e/device-driver-install-uac-prompt-after-user-first-logon?forum=configmgrgeneral
# Credits: https://lukesalter.wordpress.com/tag/uac/
# Credits: https://www.red-gate.com/simple-talk/dotnet/net-framework/high-performance-powershell-linq/#post-71022-_Toc482783751

function Reset-ConfigFlags
{
    Trace-FunctionCall -Script{
        Write-Log -Level INFO -Message "Reset all ConfigFlag's having value 131072 to 0. This will avoid UAC prompts due driver initialization at standard user logon."
        $regKeys = Get-ChildItem -Path "HKLM:\SYSTEM\CurrentControlSet\Enum" -Recurse -ErrorAction SilentlyContinue | Where-Object {$_.ValueCount -gt 0}
        foreach($regKey in $regKeys)
        {
            $delegate =  [Func[string,bool]]{ $args[0].Contains("ConfigFlags") }
            $hasConfigFlags =[Linq.Enumerable]::Any([string[]]$($regKey.Property), $delegate)
            if($hasConfigFlags)
            {
                $configFlags = $regKey.GetValue("ConfigFlags")
                if($configFlags -eq 131072) 
                {
                    #The ConfigFlag value 131072 signals a driver initialization, 
                    #which we do not want for a standard user user at logon, so set 
                    #ConfigFlags to 0
                    $path = $regKey.Name.Replace("HKEY_LOCAL_MACHINE\","HKLM:\")
                    Set-ItemProperty -Path $path -Name "ConfigFlags" -Value 0
                    Write-Log -Level INFO -Message "ConfigFlag in '$path' was reset to 0."
                }
            }
        }
    } | Out-Null
}