function Get-DtCmAssignedSite
{
	<#
		.SYNOPSIS
		Get Sccm assigned site
		
		.DESCRIPTION
		Get Sccm assigned site

		.EXAMPLE
		Get-DtCmAssignedSite

		.NOTES        
		Version:        1.0
		Author:         trondr
		Company:        MyCompany
		Repository:     https://github.com/trondr/DriverTool.git
	#>
	[CmdletBinding()]
	Param (
		[Parameter(ValueFromPipeline=$true)]
		$InputObject
	)
	
	begin
	{
		Assert-DtCmClientIsInstalled
	}
	process
	{
		$((New-Object -ComObject "Microsoft.SMS.Client").GetAssignedSite())
	}
	end
	{
	
	}
}