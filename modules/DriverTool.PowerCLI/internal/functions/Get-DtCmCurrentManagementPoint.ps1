function Get-DtCmCurrentManagementPoint
{
	<#
		.SYNOPSIS
		Get current Sccm management point
		
		.DESCRIPTION
		Get current Sccm management point

		.EXAMPLE
		Get-DtCmCurrentManagementPoint

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
		(New-Object -ComObject "Microsoft.SMS.Client").GetCurrentManagementPoint()
	}
	end
	{
	
	}
}