# DriverTool

Downloads drivers and software for current PC model and creates a driver package that can be imported into SCCM as a package or application.

# Introduction

If you are tasked with preparing a new PC model for operating system install using SCCM, DriverTool can provide the heavy lifting of finding, downloading and do the initial packaging of drivers and driver updates.

When the heavy lifting is done you still have to test and make adjustments to the produced package. Also any firmware and BIOS updates need to be adjusted and packaged separately to be run unattended in the task sequence.

Packaging and command line experience is required to use and understand this tool.

DriverTool currently supports PCs from Dell, HP and Lenovo.

# Overall Procedure

1. Install Windows 10 from original media
   * Typically there will be uknown devices in Device Manager after operating system has been installed. 
2. Create system restore snapshot
	* Before making any changes to the system after OS install, create a system restore snapshot. 
	* This snapshot can be restored when testing driver package later in the procedure.
	* The c:\temp working folder will be unaffected by a system restore.
  
3. Install chocolatey package manager
	* This will provide effective install of various tools. Alternatively you could prepare a USB stick with the tools.
	* Open powershell admin command prompt and run:
	```batch
	Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
	```
4. Install Notepad++ if you prefer a more advanced text editor than Notepad.
	* Open admin command prompt and run:
	```batch
	choco install notepadplusplus
	```
5. Install DriverTool to c:\Temp\DriverTool
	* Download from latest release from: https://github.com/trondr/DriverTool/releases
6. Create an initial driver package. This will download _all_ drivers and updates found for the current model. Including any Firmware and BIOS updates.
	```batch
	IF NOT EXIST "c:\temp" mkdir "c:\temp"
	IF NOT EXIST "c:\temp\DI" mkdir "c:\temp\DI"
	CD c:\temp
	"c:\temp\DriverTool\DriverTool.exe" CreateDriverPackage /destinationFolder="c:\temp\DI" /packagePublisher="YourCompany" /baseOnLocallyInstalledUpdates="False"
	```
	![GitHub Logo](/doc/images/InitialDriverPackage_CommandLine.png)
	![GitHub Logo](/doc/images/InitialDriverPackage_Create.png)
	![GitHub Logo](/doc/images/InitialDriverPackage_Root.png)

7. Install SCCM Package, Example:
	```batch
	cd C:\temp\DI\20EQ0022MN\2021-03-05-1.0\Script\Drivers\005_Sccm_Package_2019_11_01
	c:\temp\DI\20EQ0022MN\2021-03-05-1.0\Script\Drivers\005_Sccm_Package_2019_11_01\DT-Install-Package.cmd
	```
	* This will fill up the machine with vendor provided "injecteable" (INF) drivers. Device Manager will show less or no uknown devices after this step.
  
8. Install vendor specific update utility.
	* Lenovo System Update 
		* choco install lenovo-thinkvantage-system-update
	* Dell Command Update
		* choco install DellCommandUpdate
	* HP Image Assistant 
		* https://ftp.hp.com/pub/caps-softpaq/cmit/HPIA.html

9. Run vendor specific update utility and install all recommended updates _exluding_ BIOS and firmware.
	* Do any necessary reboots and check that all devices are ok in Device Manager.
	* BIOS and firmware must be installed in separate packages due to general requirement for hard reboot.

10. Create Driver Package based installed updates.
	```batch
	IF NOT EXIST "c:\temp\D" mkdir "c:\temp\D"
	"c:\temp\DriverTool\DriverTool.exe" CreateDriverPackage /destinationFolder="c:\temp\D" /packagePublisher="YourCompany" /baseOnLocallyInstalledUpdates="True" /excludeUpdatePatterns="['BIOS';'Firmware']"
	```
	* This will create a package only containing updates that were installed with the vendor update utility. 
	* This package will be basis for import into SCCM after testing and any adjustments.
	![GitHub Logo](/doc/images/DriverPackage_Drivers.png)
	
11. Make any adjustments to driver package
	* The downloaded updates are prepared in the folder: C:\Temp\D\<model>\<release date>-<version>\Script\Drivers
	* Updates are installed in alphabetical order based on the folder name. To change the install order, change the prefix of the folder names accordingly.	Examples:
	```text
	  Fingerprint will be installed before audio
	   ...\Drivers\010_Fingerprint_reader_Synaptics_Metallica_MOH_10_64_5.1.327.26
	   ...\Drivers\020_Audio_Realtek_Audio_Driver_10_64_6.0.1.8224

	  Audio will be installed before fingerprint:
	   ...\Drivers\007_Audio_Realtek_Audio_Driver_10_64_6.0.1.8224
	   ...\Drivers\010_Fingerprint_reader_Synaptics_Metallica_MOH_10_64_5.1.327.26

	```
   	* Any folders prefixed with underscore (_) will be excluded from install. Example:	  

	```text
	      ...\Drivers\Audio_Realtek_Audio_Driver_10_64_6.0.1.8224
	   rename to:
	      ...\Drivers\_Audio_Realtek_Audio_Driver_10_64_6.0.1.8224
	```
12. Remove any disabled update folders.
	* Removing disabled update folders will make driver package smaller.

13. Test driver package exit codes.
	* Open Admin command prompt and change directory to example:
	```text
	c:\temp\DI\20EQ0022MN\2021-03-05-1.0\Script
	```
	* Install driver package. Example:   
	```text
	Install.cmd > "%public%\Logs\Lenovo ThinkPad P50 20EQ0022MN WIN10X64 20H2 Drivers 2021-03-05_1.0_Install.cmd.log"
	```
	* Check logs in '%public%\Logs' folder. Pay attention to exit codes other than 3010 and 0. If exit code is 259 (or in the area 250) this might indicate that this is a exit code from DPInst.exe. To solve this, uncomment the following lines after setup.exe in the corresponding DT-Install-Package.cmd
	```batch
	Set DpInstExitCode=%errorlevel%
	"%~dp0..\DpInstExitCode2ExitCode.exe" %DpInstExitCode%
	```
	* Make any other necessary adjustments to DT-Install-Package.cmd. 
	* Note that the original downladed update can be found in the 'c:\temp\DriverToolCache' folder.

14. If required, add any other updates to the Drivers folder.
	* Typically new updates are deployed by Vendor and you need to update the driver package.
	* Or you have connected hardware such as driver for a monitor that you need to add to the driver package.
	* You can choose to manually add such updates to the '...\Drivers\NNN_some update folder' folder as long as you utilize the mechanism that DriverTool expects: ('...\Drivers\NNN_some update folder\DT-Install-Package.cmd')
      
15. Compress Driver Package
	* Run: c:\Temp\D\<model>\<release date>-<version>\Script\_Compress.cmd
	* Verify that Drivers.zip has been created.
	* Delete folder: C:\Temp\D\<model>\<release date>-<version>\Script\Drivers

16. Restore Windows from system restore snapshot.

17. Test driver package, same as step 13.

18. Install and run vendor system update utility to verify that all drivers have been installed by the driver package.

## Command line help

```
DriverTool 1.0.21087.48 - Download drivers and software for current PC model and create a driver package that can be imported into SCCM as a package or application.
Copyright © github/trondr 2018-2019
Usage: DriverTool.exe <command> [parameters]

Commands:
---------
Help                            Display this help text
License                         Display license
Credits                         Display credits
ExportRemoteUdateInfo           Export remote update information for
                                specified model to csv file.
ExportLocalUdateInfo            Export local update information for the
                                current system to csv file.
CreateDriverPackage             Create driver package for given manufacturer
                                and model and operating system.
InstallDriverPackage            Install driver package
UnInstallDriverPackage          Uninstall driver package.
CompressDriverPackage           Compress Drivers folder.
DecompressDriverPackage         Decompress Drivers.zip.

Commands and parameters:
------------------------
ExportRemoteUdateInfo           Export remote update information for
                                specified model to csv file. The update
                                information is extracted from xml files
                                located on the respective vendor web sites.
                                Example for Lenovo:
                                https://download.lenovo.com/catalog. Example
                                for model 20EQ (Lenovo Thinkpad P50) and
                                Win10:
                                https://download.lenovo.com/catalog/20EQ_Win10.xml.
                                The remote update information typically
                                contains all available updates also including
                                those allready installed.
   /csvFileName                 [Required] Path to csv file. Alternative
                                parameter name: /f
   /overWrite                   [Optional] Overwrite csv file if it allready
                                exists. Alternative parameter name: /o.
                                Default value: False
   /manufacturer                [Optional] Manufacturer as specified by the
                                Manufacturer property of the
                                Win32_ComputerSystem wmi class instance.
                                Powershell script to extract the
                                manufacturer: $(Get-WmiObject
                                Win32_ComputerSystem|Select-Object
                                Manufacturer).Manufacturer. If manufacturer
                                is not specified the current system
                                manufacturer will be looked up and used.
                                Alternative parameter name: /ma. Default
                                value:
   /modelCode                   [Optional] Model code. Powershell script to
                                extract the model code for (1)LENOVO:
                                $(Get-WmiObject
                                Win32_ComputerSystem|Select-Object
                                Model).Model.SubString(0,4); (2) Dell:
                                $(Get-WmiObject
                                Win32_ComputerSystem|Select-Object
                                SystemSKUNumber).SystemSKUNumber; (3) HP:
                                (Get-WmiObject -Class MS_SystemInformation
                                -Namespace "root\WMI"| Select-Object
                                BaseBoardProduct).BaseBoardProduct. If model
                                code is not specified the current system
                                model code will be looked up and used.
                                Alternative parameter name: /m. Default
                                value:
   /operatingSystemCode         [Optional] Operating system code. If
                                operating system code is not specified the
                                current system model code will be looked up
                                and used. Alternative parameter name: /op.
                                Default value:
   /excludeUpdatePatterns       [Optional] Exclude updates where title or
                                category match any of the specified regular
                                expression patterns. Alternative parameter
                                name: /xu. Default value: []

   Example: DriverTool.exe ExportRemoteUdateInfo /csvFileName="c:\temp\updates.csv" /overWrite="False" /manufacturer="LENOVO" /modelCode="20EQ" /operatingSystemCode="WIN10X64" /excludeUpdatePatterns="['Software';'BIOS';'Firmware']" 
   Example (alternative): DriverTool.exe ExportRemoteUdateInfo /f="c:\temp\updates.csv" /o="False" /ma="LENOVO" /m="20EQ" /op="WIN10X64" /xu="['Software';'BIOS';'Firmware']" 


ExportLocalUdateInfo            Export local update information for the
                                current system to csv file. The current
                                system is typically a reference machine and
                                the exported info can be used to automate
                                download of necessary updates to be installed
                                on new systems with the same specification.
                                It is required that vendor specific update
                                utility (such as Lenovo System Update) is
                                installed on the system and has been run to
                                install all relevant updates. Currently only
                                Lenovo System update is supported.
   /csvFileName                 [Required] Path to csv file. Alternative
                                parameter name: /f
   /overWrite                   [Optional] Overwrite csv file if it allready
                                exists. Alternative parameter name: /o.
                                Default value: False
   /excludeUpdatePatterns       [Optional] Exclude updates where title or
                                category match any of the specified regular
                                expression patterns. Alternative parameter
                                name: /xu. Default value: []

   Example: DriverTool.exe ExportLocalUdateInfo /csvFileName="c:\temp\updates.csv" /overWrite="False" /excludeUpdatePatterns="['Software';'BIOS';'Firmware']" 
   Example (alternative): DriverTool.exe ExportLocalUdateInfo /f="c:\temp\updates.csv" /o="False" /xu="['Software';'BIOS';'Firmware']" 


CreateDriverPackage             Create driver package for given manufacturer
                                and model. Currently Lenovo is fully
                                supported. Support for Dell and HP is partial
                                and includes creating driver package with all
                                possible updates (needed and and unneeded
                                alike) so for Dell and HP there is more
                                requirement for manual decsion on what to
                                include or not after package has been
                                created. If manufacturer or model or
                                operating system is not given the respective
                                values are automatically looked up for
                                current system.
   /destinationFolder           [Required] Destination folder. Alternative
                                parameter name: /df
   /packagePublisher            [Optional] Package publisher company name.
                                This should be the name of the company of
                                whoever creates the driver package.
                                Alternative parameter name: /pu. Default
                                value: MyCompany
   /manufacturer                [Optional] Manufacturer as specified by the
                                Manufacturer property of the
                                Win32_ComputerSystem wmi class instance.
                                Powershell script to extract the
                                manufacturer: $(Get-WmiObject
                                Win32_ComputerSystem|Select-Object
                                Manufacturer).Manufacturer. If manufacturer
                                is not specified the current system
                                manufacturer will be looked up and used.
                                Alternative parameter name: /ma. Default
                                value:
   /systemFamily                [Optional] SystemFamily as specified by the
                                SystemFamily property of the
                                Win32_ComputerSystem wmi class instance.
                                Powershell script to extract the system
                                family: $(Get-WmiObject
                                Win32_ComputerSystem|Select-Object
                                SystemFamily).SystemFamily. If system family
                                is not specified the current system family
                                will be looked up and used. Alternative
                                parameter name: /sf. Default value:
   /modelCode                   [Optional] Model code. Powershell script to
                                extract the model code for (1)LENOVO:
                                $(Get-WmiObject
                                Win32_ComputerSystem|Select-Object
                                Model).Model.SubString(0,4); (2) Dell:
                                $(Get-WmiObject
                                Win32_ComputerSystem|Select-Object
                                SystemSKUNumber).SystemSKUNumber; (3) HP:
                                (Get-WmiObject -Class MS_SystemInformation
                                -Namespace "root\WMI"| Select-Object
                                BaseBoardProduct).BaseBoardProduct. If model
                                code is not specified the current system
                                model code will be looked up and used.
                                Alternative parameter name: /mo. Default
                                value:
   /operatingSystemCode         [Optional] Operating system code. If
                                operating system code is not specified the
                                current system operating system code will be
                                looked up and used. Alternative parameter
                                name: /op. Default value:
   /baseOnLocallyInstalledUpdates[Optional] Create driver package based on
                                locally installed updates. Use this on a a
                                fully updated reference machine where the
                                vendor specific update utility (Lenovo System
                                Update) has been run and no more updates are
                                available. This option is currently not
                                supported on Dell and HP. Alternative
                                parameter name: /lu. Default value: False
   /excludeUpdatePatterns       [Optional] Exclude updates where title or
                                category match any of the specified regular
                                expression patterns. Alternative parameter
                                name: /xu. Default value: []
   /packageTypeName             [Optional] A short name describing the
                                content of the package. Example: 'Software',
                                'Firmware', 'BIOS'. The package type name
                                will be used in the package name. Alternative
                                parameter name: /ptn. Default value: Drivers
   /excludeSccmPackage          [Optional] Exclude Sccm package from the
                                created package. Typically you set this to
                                true if you want only non-driver related
                                updates, such as BIOS or firmware, to be part
                                of the package. Alternative parameter name:
                                /exs. Default value: False
   /doNotDownloadSccmPackage    [Optional] Do not download Sccm Package. Used
                                as a workaround for web scraping gone wrong
                                when Lenovo has changed the web design. Both
                                the /sccmPackageInstaller and the
                                /sccmPackageReadme parameters must be defined
                                if /doNotDownloadSccmPackage=True.
                                Alternative parameter name: /dnd. Default
                                value: False
   /sccmPackageInstaller        [Optional] Specify sccm package installer to
                                override automatic download of Sccm package.
                                Used as a workaround for web scraping gone
                                wrong when Lenovo has changed the web design.
                                The sccm package installer spesified must be
                                be downloaded manually and saved to the
                                DriverToolCache as specified in the
                                application configuration file
                                (DownloadCacheDirectoryPath). Alternative
                                parameter name: /spi. Default value:
   /sccmPackageReadme           [Optional] Specify sccm package readme to
                                override automatic download of Sccm package.
                                Used as a workaround for web scraping gone
                                wrong when Lenovo has changed the web design.
                                The sccm package readme spesified must be be
                                downloaded manually and saved to the
                                DriverToolCache as specified in the
                                application configuration file
                                (DownloadCacheDirectoryPath). Alternative
                                parameter name: /spr. Default value:
   /sccmPackageReleased         [Optional] Specify sccm package relase date.
                                Alternative parameter name: /sprd. Default
                                value: 2001-01-01

   Example: DriverTool.exe CreateDriverPackage /destinationFolder="c:\temp\Drivers\SomeModel" /packagePublisher="MyCompany" /manufacturer="LENOVO" /systemFamily="ThinkPad P50" /modelCode="20EQ" /operatingSystemCode="WIN10X64" /baseOnLocallyInstalledUpdates="False" /excludeUpdatePatterns="['Software';'BIOS';'Firmware']" /packageTypeName="Drivers" /excludeSccmPackage="False" /doNotDownloadSccmPackage="False" /sccmPackageInstaller="tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_201910.exe" /sccmPackageReadme="tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_201910.txt" /sccmPackageReleased="2019-10-01" 
   Example (alternative): DriverTool.exe CreateDriverPackage /df="c:\temp\Drivers\SomeModel" /pu="MyCompany" /ma="LENOVO" /sf="ThinkPad P50" /mo="20EQ" /op="WIN10X64" /lu="False" /xu="['Software';'BIOS';'Firmware']" /ptn="Drivers" /exs="False" /dnd="False" /spi="tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_201910.exe" /spr="tp_x1carbon_mt20qd-20qe-x1yoga_mt20qf-20qg_w1064_1809_201910.txt" /sprd="2019-10-01" 


InstallDriverPackage            Install driver package. This command looks
                                for the .\Drivers sub folder. If the
                                .\Drivers does not exist the command looks
                                for the \Drivers.zip file and extracts it to
                                .\Drivers folder. The command then executes
                                each DT-Install-Package.cmd in any sub
                                folders below the .\Drivers folder.
   /driverPackagePath           [Required] Driver package folder path. Below
                                this path there should be a .\Drivers sub
                                folder or Drivers.zip. Alternative parameter
                                name: /dpp

   Example: DriverTool.exe InstallDriverPackage /driverPackagePath="c:\temp\Drivers\SomeModel" 
   Example (alternative): DriverTool.exe InstallDriverPackage /dpp="c:\temp\Drivers\SomeModel" 


UnInstallDriverPackage          Uninstall driver package. This command looks
                                for the .\Drivers sub folder. If the
                                .\Drivers does not exist the command looks
                                for the \Drivers.zip file and extracts it to
                                .\Drivers folder. The command then executes
                                each DT-UnInstall-Package.cmd in any sub
                                folders below the .\Drivers folder.
   /driverPackagePath           [Required] Driver package folder path. Below
                                this path there should be a .\Drivers sub
                                folder or a Drivers.zip. Alternative
                                parameter name: /dpp

   Example: DriverTool.exe UnInstallDriverPackage /driverPackagePath="c:\temp\Drivers\SomeModel" 
   Example (alternative): DriverTool.exe UnInstallDriverPackage /dpp="c:\temp\Drivers\SomeModel" 


CompressDriverPackage           Compress the Drivers folder to Drivers.zip
   /driverPackagePath           [Required] Driver package folder path. Below
                                this path there should be a .\Drivers sub
                                folder. Alternative parameter name: /dpp

   Example: DriverTool.exe CompressDriverPackage /driverPackagePath="c:\temp\Drivers\SomeModel" 
   Example (alternative): DriverTool.exe CompressDriverPackage /dpp="c:\temp\Drivers\SomeModel" 


DecompressDriverPackage         Decompress Drivers.zip to \Drivers folder.
   /driverPackagePath           [Required] Driver package folder path. Below
                                this path there should be a .\Drivers.zip
                                file. Alternative parameter name: /dpp

   Example: DriverTool.exe DecompressDriverPackage /driverPackagePath="c:\temp\Drivers\SomeModel" 
   Example (alternative): DriverTool.exe DecompressDriverPackage /dpp="c:\temp\Drivers\SomeModel" 
 
```

# Build Environment

* From an admin command line run:
```batch
@"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))" && SET "PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin"
choco feature enable -n allowGlobalConfirmation
choco install git
choco install git-credential-winstore
choco install fake
choco upgrade fake
choco install windows-sdk-10-version-1903-all
choco install windows-sdk-10-version-2004-all
choco install netfx-4.7.2-devpack
choco install netfx-4.8-devpack
choco install visualstudio2019buildtools
choco install wixtoolset
choco insall nunit
choco install lenovo-thinkvantage-system-update
choco feature disable -n allowGlobalConfirmation
```

*From a new admin command line:

dotnet new -i NUnit3.DotNetNew.Template

* From a standard commmand line run:

```batch
mkdir c:\dev\github.trondr
cd c:\dev\github.trondr
git clone https://github.com/trondr/DriverTool.git .\DriverTool
cd DriverTool
Build.cmd
```

## Development Environment

* From an admin command line run:

```batch
choco feature enable -n allowGlobalConfirmation
choco install sourcetree
choco install notepadplusplus
choco install vscode	
choco install visualstudio2019enterprise
REM choco install visualstudio2019professional
choco feature disable -n allowGlobalConfirmation
```

## Build

* Install chocolatey 
	```batch
	@"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))" && SET "PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin"
	```
	
* Install fake
	
	```batch
	choco install fake
	
* Upgrade fake
	
	```batch
	choco upgrade fake
	
	```
* Install NUnit Template

	```batch
	dotnet new -i NUnit3.DotNetNew.Template
	```
	
* Build
	
	```batch
	fake run build.fsx
	```
