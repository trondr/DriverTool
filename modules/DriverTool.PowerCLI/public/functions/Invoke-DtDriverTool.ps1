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

        .PARAMETER CreateDriverPackage
        Create driver update package for current manufacturer and model.

        .PARAMETER DestinationFolder
        Destination folder where the driver package will be created.

        .PARAMETER PackagePublisher
        Name of the package publisher. Typically your company name.

        .PARAMETER BaseOnLocallyInstalledUpdates
        Create driver package based on locally installed updates. Use this on a fully updated reference machine where the vendor specific update utility (Lenovo System Update) has been run and no more updates are available.

        .PARAMETER InstallDriverPackage
        Install driver package. This command looks for the .\Drivers sub folder. If the .\Drivers does not exist the command looks for the \Drivers.zip file and extracts it to .\Drivers folder. The command then executes each DT-Install-Package.cmd in any sub folders below the .\Drivers folder.

        .PARAMETER UnInstallDriverPackage
        Uninstall driver package. This command looks for the .\Drivers sub folder. If the .\Drivers does not exist the command looks for the \Drivers.zip file and extracts it to .\Drivers folder. The command then executes each DT-UnInstall-Package.cmd in any sub folders below the .\Drivers folder.

        .PARAMETER DriverPackagePath
        Driver package folder path. Below this path there should be a .\Drivers sub folder or Drivers.zip

        .EXAMPLE
		Invoke-DtDriverTool -Help

		.EXAMPLE
		Invoke-DtDriverTool -CmUi

        .EXAMPLE
        Write-Host "Create inital driver update package for current model containing all updates (including BIOS and firmware)."
        Invoke-DtDriverTool -CreateDriverPackage -DestinationFolder "c:\temp\DI" -PackagePublisher "trondr"

        .EXAMPLE
        Write-Host "Create driver update package for current model based on locally installed updates and excluding any BIOS or firmware updates."
        Invoke-DtDriverTool -CreateDriverPackage -DestinationFolder "c:\temp\D" -PackagePublisher "trondr" -BaseOnLocallyInstalledUpdates -ExcludeUpdatePatterns @("BIOS","Firmware")

        .EXAMPLE
        Invoke-DtDriverTool -InstallDriverPackage -DriverPackagePath "C:\temp\d\20EQ0022MN\2021-09-22-1.0\Script\"

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
        $CmUi,

        [Parameter(ParameterSetName="CreateDriverPackage",Mandatory=$true,HelpMessage="Create driver package for given manufacturer
        and model.")]
        [switch]
        $CreateDriverPackage,
        [Parameter(ParameterSetName="CreateDriverPackage",Mandatory=$true,HelpMessage="Destination folder where the driver package will be created.")]
        [String]
        $DestinationFolder,
        [Parameter(ParameterSetName="CreateDriverPackage",Mandatory=$true,HelpMessage="Name of the package publisher. Typically your company name.")]
        [String]
        $PackagePublisher,
        [Parameter(ParameterSetName="CreateDriverPackage",Mandatory=$false,HelpMessage="Create driver package based on locally installed updates. Use this on a fully updated reference machine where the vendor specific update utility (Lenovo System Update) has been run and no more updates are available.")]
        [switch]
        $BaseOnLocallyInstalledUpdates=$false,
        [Parameter(ParameterSetName="CreateDriverPackage",Mandatory=$false,HelpMessage="Exclude updates where title or category match any of the specified regular expression patterns.")]
        [string[]]
        $ExcludeUpdatePatterns=@(),

        [Parameter(ParameterSetName="InstallDriverPackage",Mandatory=$true,HelpMessage="Install driver package. This command looks for the .\Drivers sub folder. If the .\Drivers does not exist the command looks for the \Drivers.zip file and extracts it to .\Drivers folder. The command then executes each DT-Install-Package.cmd in any sub folders below the .\Drivers folder.")]
        [switch]
        $InstallDriverPackage,
        
        [Parameter(ParameterSetName="UnInstallDriverPackage",Mandatory=$true,HelpMessage="Uninstall driver package. This command looks for the .\Drivers sub folder. If the .\Drivers does not exist the command looks for the \Drivers.zip file and extracts it to .\Drivers folder. The command then executes each DT-UnInstall-Package.cmd in any sub folders below the .\Drivers folder.")]
        [switch]
        $UnInstallDriverPackage,
        
        [Parameter(ParameterSetName="InstallDriverPackage",Mandatory=$true,HelpMessage="Driver package folder path. Below this path there should be a .\Drivers sub folder or Drivers.zip")]
        [Parameter(ParameterSetName="UnInstallDriverPackage",Mandatory=$true,HelpMessage="Driver package folder path. Below this path there should be a .\Drivers sub folder or a Drivers.zip.")]
        [string]
        $DriverPackagePath        
	)
	
	begin
	{		
        Assert-DtFileExists -Path $DriverToolExe -Message "DriverTool.exe ($driverToolExe) not found."
	}
	process
	{           
        if($Help){
            & "$driverToolExe" "Help"
        }
        elseif($CmUi){
            & "$driverToolExe" "CmUi"
        }
        elseif($CreateDriverPackage){
            $ExcludeUpdatePatternsArgument = [System.Text.StringBuilder]::new()            
            $ExcludeUpdatePatterns | ForEach-Object{[void]$ExcludeUpdatePatternsArgument.Append("$($_);")}
            & "$driverToolExe" "CreateDriverPackage" "/destinationFolder=`"$DestinationFolder`"" "/packagePublisher=`"$PackagePublisher`"" "/baseOnLocallyInstalledUpdates=$BaseOnLocallyInstalledUpdates" "/excludeUpdatePatterns=[$($ExcludeUpdatePatternsArgument.ToString().Trim(';'))]"
        }
        elseif($InstallDriverPackage)
        {
            & "$driverToolExe" "InstallDriverPackage" "/driverPackagePath=`"$DriverPackagePath`""
        }
        elseif($UnInstallDriverPackage)
        {
            & "$driverToolExe" "UnInstallDriverPackage" "/driverPackagePath=`"$DriverPackagePath`""
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
#Invoke-DtDriverTool -CreateDriverPackage -DestinationFolder "c:\temp\DI" -PackagePublisher "trondr"
#Invoke-DtDriverTool -CreateDriverPackage -DestinationFolder "c:\temp\D" -PackagePublisher "trondr" -BaseOnLocallyInstalledUpdates -ExcludeUpdatePatterns @("BIOS","Firmware")
#Get-Help Invoke-DtDriverTool -Full