function Test-IsAdministrator
{	
    Write-Verbose "Test-IsAdministrator..."
    $isAdministrator = $false
	$windowsIdentity=[System.Security.Principal.WindowsIdentity]::GetCurrent()
  	$windowsPrincipal=new-object System.Security.Principal.WindowsPrincipal($windowsIdentity)
  	$administratorRole=[System.Security.Principal.WindowsBuiltInRole]::Administrator
  	$isAdministrator=$windowsPrincipal.IsInRole($administratorRole)    
    Write-Verbose "Test-IsAdministrator->$isAdministrator"
    return $isAdministrator
}
#TEST: $global:VerbosePreference = "Continue"
#TEST: Test-IsAdministrator