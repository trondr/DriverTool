function Test-IsTerminalServer
{
	Trace-FunctionCall -Script {
		$isTerminalServer = $false
		try
		{
			$terminalServerSettings = Get-WmiObject Win32_TerminalServiceSetting -Namespace "root\CIMv2\TerminalServices" | Select-Object -first 1
			if($terminalServerSettings.TerminalServerMode -eq 1)
			{
				$isTerminalServer = $true
			}
			else
			{
				$isTerminalServer = $false
			}
		}
		catch
		{
			$isTerminalServer = $false
		}
		return $isTerminalServer
	}
	
}
#TEST: $global:VerbosePreference = "Continue"
#TEST: Test-IsTerminalServer