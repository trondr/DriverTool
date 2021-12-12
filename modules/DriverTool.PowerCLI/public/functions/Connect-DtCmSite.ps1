function Connect-DtCmSite
{
	<#
		.SYNOPSIS
		Connect to Sccm site
		
		.DESCRIPTION
		Connect to Sccm site. It is required that System Configuration Manager Console is installed.

		.EXAMPLE
		Connect-DtCmSite

		.NOTES        
		Version:        1.0
		Author:         github/trondr
		Company:        github/trondr
		Repository:     https://github.com/trondr/DriverTool.git
	#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[string]
		$SiteCode=$(Get-DtCmAssignedSite),
		[Parameter(Mandatory=$false)]
		[string]
		$SiteServer=$(Get-DtCmCurrentManagementPoint)
	)
	
	begin
	{		
		Assert-DtCmManagerConsoleIsInstalled
		# Customizations
		$initParams = @{}
		#$initParams.Add("Verbose", $true) # Uncomment this line to enable verbose logging
		$initParams.Add("ErrorAction", "Stop") # Uncomment this line to stop the script on any errors
	}
	process
	{
		if($null -eq (Get-Module ConfigurationManager)) {
			Import-Module "$($ENV:SMS_ADMIN_UI_PATH)\..\ConfigurationManager.psd1" @initParams 
		}

		if($null -eq (Get-PSDrive -Name $SiteCode -PSProvider CMSite -ErrorAction SilentlyContinue)) {
			New-PSDrive -Name $SiteCode -PSProvider CMSite -Root $SiteServer @initParams
		}

		# Set the current location to be the site code.
		Set-Location "$($SiteCode):\" @initParams
	}
	end
	{
	
	}
}