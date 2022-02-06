function New-DtCmTaskSequenceFromDriverPackPackageDefinitionSms
{
	<#
		.SYNOPSIS
		Create new task sequence from driver pack package defintion sms.
		
		.DESCRIPTION
		Create new task sequence from driver pack package defintion sms.

		.EXAMPLE
		Write-Host "Package all driver packages in a folder structure and create and add to task sequence."
		$packageDefintionSms = Get-ChildItem -Path "Z:\Applications\CM-Drivers\21H2" -Filter "PackageDefinition.sms" -Recurse | ForEach-Object {$_.FullName} 
		$packageDefintionSms | New-DtCmPackageFromDriverPackPackageDefinitionSms 
		New-DtCmTaskSequenceFromDriverPackPackageDefinitionSms -Path $packageDefintionSms -Name "Test CM Drivers 21H2" -Description "Test CM Drivers 21H2" -ProgramName "INSTALL-OFFLINE-OS"

		.NOTES        
		Version:        1.0
		Author:         github/trondr
		Company:        github/trondr
		Repository:     https://github.com/trondr/DriverTool.git
	#>
	[CmdletBinding()]
	Param (
		[Parameter(ValueFromPipeline=$false,Mandatory=$true)]
		#Path to package definition sms file
		[System.string[]]
		$Path,		
        [Parameter(Mandatory=$true)]
        [ValidateLength(1,50)]
		#Task sequence name. Maximum 50 characters long.
		[string]
		$Name,
		[Parameter(Mandatory=$true)]
		#Task sequence description
		[string]
		$Description,
		[Parameter(Mandatory=$true)]
		#Program name. Must exist in all PackageDefinition.sms files.
		[string]
		$ProgramName
	)
	
	begin
	{
		Connect-DtCmSite		
	}
	process
	{		
		try {				
			$script = Get-DtScriptNewCmTaskSequenceFromDriverPackPackageDefinitionSms -Path $Path -Name $Name -Description $Description -ProgramName $ProgramName
			Write-Host "Invoking script to create Sccm Task Sequence:"
			Write-Host "$script"
			Write-Host "--------------------------------------------------------------------------------"
			Invoke-Expression $script
			Write-Host "--------------------------------------------------------------------------------"
		}
		catch {
			Write-Host "New-DtCmTaskSequenceFromDriverPackPackageDefinitionSms failed processing '$($Path)' due to: $(Convert-DtExceptionToMessage -Exception $_.Exception)" -ForegroundColor Red -ErrorAction Stop
		}		
	}
	end
	{
	
	}
}