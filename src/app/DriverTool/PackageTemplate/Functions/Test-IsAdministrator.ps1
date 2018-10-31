function Test-IsAdministrator
{	
    Trace-FunctionCall -Script {
        $isAdministrator = $false
        $windowsIdentity=[System.Security.Principal.WindowsIdentity]::GetCurrent()
        $windowsPrincipal=new-object System.Security.Principal.WindowsPrincipal($windowsIdentity)
        $administratorRole=[System.Security.Principal.WindowsBuiltInRole]::Administrator
        $isAdministrator=$windowsPrincipal.IsInRole($administratorRole)
        return $isAdministrator
    }
    
}
#TEST: $global:VerbosePreference = "Continue"
#TEST: Test-IsAdministrator