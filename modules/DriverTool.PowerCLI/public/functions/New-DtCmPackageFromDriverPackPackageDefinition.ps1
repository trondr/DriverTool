function New-DtCmPackageFromDriverPackPackageDefinitionSms
{
	<#
		.SYNOPSIS
		Create new Sccm package from driver pack package definition.
		
		.DESCRIPTION
		Create new Sccm package from driver pack package definition.

		.EXAMPLE
		New-DtCmPackageFromDriverPackPackageDefinitionSms

		.NOTES        
		Version:        1.0
		Author:         trondr
		Company:        MyCompany
		Repository:     https://github.com/trondr/DriverTool.git
	#>
	[CmdletBinding()]
	Param (
		[Parameter(ValueFromPipeline=$true)]
		#Path to package definition sms file
		[System.string[]]
		$Path
	)
	
	begin
	{
		Connect-DtCmSite
		Write-Host "Connected"
	}
	process
	{
		foreach($p in $Path)
		{
			try {
				Write-Host "TODO: Implemented processing of each item in the pipe line: $p" -ForegroundColor Yellow
				$script = Get-DtScriptNewCmPackageFromDriverPackPackageDefinitionSms -Path $p
				Write-Host "Invoking script to create Sccm Package:"
				Write-Host "$script"
				Write-Host "--------------------------------------------------------------------------------"
				Invoke-Expression $script
				Write-Host "--------------------------------------------------------------------------------"
			}
			catch {
				Write-Host "New-DtCmPackageFromDriverPackPackageDefinitionSms failed processing '$($p)' due to: $(Convert-DtExceptionToMessage -Exception $_.Exception)" -ForegroundColor Red
			}
		}
	}
	end
	{
	
	}
}