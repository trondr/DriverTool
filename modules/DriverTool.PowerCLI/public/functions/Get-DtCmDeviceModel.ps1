function Get-DtCmDeviceModel
{
	<#
		.SYNOPSIS
		Get device models from SCCM
		
		.DESCRIPTION
		Get device models from SCCM

		.EXAMPLE
		Get-DtCmDeviceModel

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
		Connect-DtCmSite
		$Query = @'
                            select distinct             
                                SMS_G_System_COMPUTER_SYSTEM.Model,
                                SMS_G_System_COMPUTER_SYSTEM.Manufacturer,
                                SMS_G_System_COMPUTER_SYSTEM_PRODUCT.Version 
                            from  SMS_R_System 
                            inner join SMS_G_System_COMPUTER_SYSTEM on SMS_G_System_COMPUTER_SYSTEM.ResourceID = SMS_R_System.ResourceId        
                            inner join SMS_G_System_COMPUTER_SYSTEM_PRODUCT on SMS_G_System_COMPUTER_SYSTEM_PRODUCT.ResourceID = SMS_R_System.ResourceId
                            where SMS_G_System_COMPUTER_SYSTEM.Manufacturer = 'LENOVO'
'@
		Write-Host "Only LENOVO currently supported by the Get-DtCmDeviceModel CmdLet" -ForegroundColor Yellow
		Write-Host "TODO: Add support for Dell" -ForegroundColor Yellow
		Write-Host "TODO: Add support for HP" -ForegroundColor Yellow
	}
	process
	{
		Invoke-CMWmiQuery -Query $Query | Foreach-Object {
            [pscustomobject][ordered]@{                
                Manufacturer = $_.SMS_G_System_COMPUTER_SYSTEM.Manufacturer
                ModelCode    = $_.SMS_G_System_COMPUTER_SYSTEM.Model
				ModelCode4   = $($_.SMS_G_System_COMPUTER_SYSTEM.Model).SubString(0,4)
                ModelName    = $_.SMS_G_System_COMPUTER_SYSTEM_PRODUCT.Version               
            }
        }|Select-Object Manufacturer,ModelCode,ModelName
	}
	end
	{
	
	}
}