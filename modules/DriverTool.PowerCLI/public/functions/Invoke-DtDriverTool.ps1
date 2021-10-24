function Invoke-DtDriverTool
{
	<#
		.SYNOPSIS
		Invoke DriverTool.exe
		
		.DESCRIPTION
		Invoke-DtDriverTool enables call to DriverTool.exe as the first bridge to the PowerShell command line.
        This CmdLet defines a parameter set for each DriverTool command as described in the 'DriverTool.exe Help'
        command.

        .PARAMETER Help
        Runs DriverTool.exe Help to open the native DriverTool.exe help in a text editor.

        .PARAMETER CmUi
        Start user interface for download and packaging of CM device drivers.

        .EXAMPLE
		Invoke-DtDriverTool -Help

		.EXAMPLE
		Invoke-DtDriverTool -CmUi
        
		.NOTES        
		Version:        1.0
		Author:         github/trondr
		Company:        github/trondr
		Repository:     https://github.com/trondr/DriverTool.git
	#>
	[CmdletBinding()]
	Param (
        [Parameter(ParameterSetName="Help",Mandatory=$true,HelpMessage="Runs DriverTool.exe Help to open the native DriverTool.exe help in a text editor.")]
        [switch]
        $Help,
        [Parameter(ParameterSetName="CmUi",Mandatory=$true,HelpMessage="Start user interface for download and packaging of CM device drivers.")]
        [switch]
        $CmUi        
	)
	
	begin
	{
		$driverToolExe = [System.IO.Path]::Combine($global:ModuleRootPath,"internal","tools","DriverTool","DriverTool.exe")
        Assert-DtFileExists -Path $driverToolExe -Message "DriverTool.exe ($driverToolExe) not found."
	}
	process
	{           
        if($Help){
            & "$driverToolExe" "Help"
        }
        elseif($CmUi){
            & "$driverToolExe" "CmUi"
        }
        else {
            Write-Host "TODO: Implement call to DriverTool.exe commands"
        }
	}
	end
	{
	
	}
}
#TEST
#Invoke-DtDriverTool -Help
#Invoke-DtDriverTool -CmUi
#Get-Help Invoke-DtDriverTool -Full