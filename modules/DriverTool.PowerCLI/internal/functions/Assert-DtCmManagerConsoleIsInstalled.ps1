function Assert-DtCmManagerConsoleIsInstalled
{
	<#
		.SYNOPSIS
		Assert that Sccm Manager Console is installed.
		
		.DESCRIPTION
		Assert that Sccm Manager Console is installed.

		.EXAMPLE
		Assert-DtCmManagerConsoleIsInstalled

		.NOTES        
		Version:        1.0
		Author:         github.com/trondr
		Company:        github.com/trondr
		Repository:     https://github.com/trondr/DriverTool.git
	#>
	[CmdletBinding()]
	Param (
		[Parameter(ValueFromPipeline=$true)]
		$InputObject
	)
	
	begin
	{
		
	}
	process
	{
		if((Test-Path -Path "$($ENV:SMS_ADMIN_UI_PATH)\..\ConfigurationManager.psd1") -eq $false)
		{
			throw "SCCM Manager Console is not installed. Please install SCCM Manager Console and try again."
		}
	}
	end
	{
	
	}
}
#TEST:
#Assert-DtCmManagerConsoleIsInstalled