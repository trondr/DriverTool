# DriverTool

Downloads drivers and software for current PC model and creates a driver package that can be imported into SCCM as a package or application.

# Introduction

If you are tasked with preparing a new PC model for operating system install using SCCM, DriverTool can provide the heavy lifting of finding, downloading and do the initial packaging of drivers and driver updates.

When the heavy lifting is done you still have to test and make adjustments to the produced package. Also any firmware and BIOS updates need to be adjusted and packaged separately to be run unattended in the task sequence.

Packaging and command line experience is required to use and understand this tool.

DriverTool currently supports PCs from Dell, HP and Lenovo.

# Procedure (DriverTool.exe)

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
	![GitHub Logo](/doc/images/InitialDriverPackage_Drivers.png)

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

10. Create Driver Package based on installed updates.
	```batch
	IF NOT EXIST "c:\temp\D" mkdir "c:\temp\D"
	"c:\temp\DriverTool\DriverTool.exe" CreateDriverPackage /destinationFolder="c:\temp\D" /packagePublisher="YourCompany" /baseOnLocallyInstalledUpdates="True" /excludeUpdatePatterns="['BIOS';'Firmware']"
	```
	* This will create a package containing updates that were installed with the vendor update utility. 
	* This package will be basis for import into SCCM after testing and any adjustments.
	![GitHub Logo](/doc/images/DriverPackage_Root.png)
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
	* Run: c:\Temp\D\<model>\<release date>-<version>\Script\\_Compress.cmd
	* Verify that Drivers.zip has been created.
	* Delete folder: C:\Temp\D\<model>\<release date>-<version>\Script\Drivers

16. Restore Windows from system restore snapshot.

17. Test driver package, same as step 13.

18. Install and run vendor system update utility to verify that all drivers have been installed by the driver package.

19. Manually split the driver package in two, one "DISM" package with CM device drivers and one Driver Updates package.
A future version of DriverTool should probably do this for you. But for now you will have to do it manually.
Copy the driver package to SCCM server. Your structure should look something like the following.

   * DISM
   ```text
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script-DISM
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script-DISM\Drivers
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script-DISM\Drivers\005_Sccm_Package_2019_11_01
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script-DISM\PackageDefinition-DISM.sms
   ```
   
   * Driver Updates (Compressed)
   ```text
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\_Compress.cmd
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\_Decompress.cmd
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\Drivers.zip
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\DriverTool
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\DriverTool\Common.Logging.dll
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\DriverTool\DriverTool.exe
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\DriverTool\DriverTool.exe.config
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\DriverTool\DriverTool.Library.dll
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\DriverTool\FSharp.Core.dll
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\Install.cmd
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\Install.xml
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\PackageDefinition.sms
   \\CM01\PkgSrc$\Packages\Lenovo ThinkPad\P50 Drivers\1909-1.0\Script\UnInstall.cmd
   ```

20. Import the two packages using "Create Package from defintion..." by browsing to the PackageDefinition.sms and PackageDefinition-DISM.sms respectively.

21. Add CM Device driver package to the task sequence in the Windows PE phace 

![GitHub Logo](/doc/images/TaskSeqence-CM-Device-Drivers-Wmi-Query.png)
![GitHub Logo](/doc/images/TaskSeqence-CM-Device-Drivers.png)

22. Add the Driver Updates package to the task sequence in operating system phase. 
As DriverTool does not calculate dependencies between updates it will in many cases be necessary to install driver updates package twice with a reboot between installs.

![GitHub Logo](/doc/images/TaskSeqence-Device-Driver-Updates-Wmi-Query.png)
![GitHub Logo](/doc/images/TaskSeqence-Device-Driver-Updates-1.png)
![GitHub Logo](/doc/images/TaskSeqence-Device-Driver-Updates-Restart.png)
![GitHub Logo](/doc/images/TaskSeqence-Device-Driver-Updates-2.png)

23. Install Windows 10 on the PC model in question using the task sequence. Verify result by running vendor driver update utility and checking that no driver updates are required.

## Command line help

```
DriverTool 1.0.22016 - Download drivers and software for current PC model and create a driver package that can be imported into SCCM as a package or application.
Copyright © github/trondr 2018-2022
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
CmUi                            Start user interface for download and
                                packaging of CM device drivers into SCCM.

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


CmUi                            Start user interface for download and
                                packaging of CM device drivers into SCCM.

   Example: DriverTool.exe CmUi 
``` 

# Procedure (PowerShell)
	
## Install Powershell Module
	
1. Create local PS Repository 

```PowerShell
New-Item -Path c:\ -Name LocalPSRepository -ItemType Directory -Force
Register-PSRepository -Name LocalPSRepository -SourceLocation "C:\LocalPSRepository" -PublishLocation "C:\LocalPSRepository" -InstallationPolicy Trusted
```
2. Download and copy DriverTool.PowerCLI.%version%.nupkg to C:\LocalPSRepository
	
3. Install PowerShell Module

```PowerShell
Install-Module -Repository LocalPSRepository -Name DriverTool.PowerCLI
```
	
## Create DriverPack Packages and task sequence.
	
Automated procedure for downloading, extracting, packaging and adding driver packs to a new task sequence. This will replace the CM Device driver package steps in the "DriverTool.exe" procedure above. The task sequence created can be added as a sub task sequence to the main OSD task sequence.

Steps:

1. Download, extract and package driver packs. Example:
	
```
@("20QW","20QF") | Foreach-Object{ Write-Host "Getting driver pack for model $_";  Get-DtDriverPack -Manufacturer Lenovo -ModelCode "$_" -OperatingSystem "win10" -OsBuild "21H2" -Verbose } | Invoke-DtDownloadDriverPack
```
The packages will be created in c:\temp\D on developement machine and should be copied to final location on Sccm Server manually.

2. Create SCCM packages after copying the package folders to server. Example:
	
```
$packageDefintionSms = Get-ChildItem -Path "Z:\Packages\CM-Drivers\21H2" -Filter "PackageDefinition.sms" -Recurse | ForEach-Object {$_.FullName} 
$packageDefintionSms
$packageDefintionSms | New-DtCmPackageFromDriverPackPackageDefinitionSms
```

3. Create task sequence with all the driver packages added. Example:
	
```
$packageDefintionSms = Get-ChildItem -Path "Z:\Applications\CM-Drivers\21H2" -Filter "PackageDefinition.sms" -Recurse | ForEach-Object {$_.FullName} 
$packageDefintionSms
New-DtCmTaskSequenceFromDriverPackPackageDefinitionSms -Path $packageDefintionSms -Name "Test CM Drivers 21H2" -Description "Test CM Drivers 21H2" -ProgramName "INSTALL-OFFLINE-OS"
```

## Create DriverUpdates Packages and task sequence

Automated procedure for downloading, extracting, packaging and adding driver updates to a new task sequence. The resulting task sequence can be added as a sub task sequence to the main OSD task sequence.

This procedures downloads _all_ driver updates for a specified computer model and can be run without having access to the computer model in question.

Note! Some of the driver updates for a model might allready be installed or not even required due to differences in actual hardware components installed. 
The resulting driver updates package can therefore be manually trimmed for unrequired drivers. If the resulting driver package from this procedure is 
not trimmed for unrequired drivers, the package might still work sucessfully but assumes that all driver updates quits gracefully if not applicable. Testing 
is allways neccessary! 

Note! Be sure to exclude BIOS and other Firmware from a driver update package as such updates requires special care and should be packaged separatly.

Note! An automated procedure for creating an allready trimmed package is availble when signed into the actual computer model in question. 
The required drivers can then be calculated based on locally installed updates after filling the machine up using the vendor update utility.
See DriverTool procedure further up.

Steps:

1. Download, extract and package driver packs. Example:

```
@("20QW","20QF") | Foreach-Object{ Write-Host "Getting driver updates for model $_";  Get-DtDriverUpdates -Manufacturer Lenovo -ModelCode "$_" -OperatingSystem "WIN10X64" -OsBuild "21H2" -ExcludeDriverUpdates @("BIOS","Firmware") -Verbose } | Invoke-DtDownloadDriverUpdates
```
Note! Some driver updates when extracted might result in a path longer than 256 characters. Try enabling long path support in registry ( [HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem]LongPathsEnabled=1) or extract such updates manually to a shorter root path before moving the result to the package drivers folder.
	
The packages will be created in c:\temp\DU on developement machine and should be copied to final location on Sccm Server manually. Example server locations: 
* Z:\Packages\DriverUpdates\21H2\20QW
* Z:\Packages\DriverUpdates\21H2\20QF

2. Create SCCM packages after copying the package folders to server. Example:
	
```
$packageDefintionSms = Get-ChildItem -Path "Z:\Packages\DriverUpdates\21H2" -Filter "PackageDefinition.sms" -Recurse -Depth 3| ForEach-Object {$_.FullName} 
$packageDefintionSms
$packageDefintionSms | New-DtCmPackageFromDriverPackPackageDefinitionSms
```
Note! When recursing the directory, the recursion depth is set to avoid getting PackageDefinition.sms for driver updates in the Drivers sub folders. The recursion depth must be set depending on start point of recursion.

3. Create task sequence with all the driver update packages added. Example:

```
$packageDefintionSms = Get-ChildItem -Path "Z:\Packages\DriverUpdates\21H2" -Filter "PackageDefinition.sms" -Recurse -Depth 3| ForEach-Object {$_.FullName}
$packageDefintionSms
New-DtCmTaskSequenceFromDriverPackPackageDefinitionSms -Path $packageDefintionSms -Name "Test Driver Updates 21H2" -Description "Test Driver Updates 21H2" -ProgramName "INSTALL"
```

# Build Environment

From a PowerShell Admin command prompt run:

```PowerShell
try{Set-ExecutionPolicy Bypass -Scope Process -Force -ErrorAction SilentlyContinue}catch{}
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
choco feature enable -n allowGlobalConfirmation
choco install git
choco install git-credential-winstore
choco install netfx-4.8-devpack
choco install visualstudio2019buildtools
choco install visualstudio2019-workload-netcorebuildtools
choco install visualstudio2019-workload-manageddesktopbuildtools
$vsBuildToolsExe = "c:\windows\temp\vs_buildtools.exe"
if((Test-Path -Path $vsBuildToolsExe) -eq $false)
{
    Invoke-WebRequest -Uri "https://aka.ms/vs/16/release/vs_buildtools.exe" -OutFile "$vsBuildToolsExe"
}
. $vsBuildToolsExe --add "Microsoft.VisualStudio.Component.FSharp.MSBuild" --passive --norestart --quiet
choco install fake
choco upgrade fake
choco install nunit
choco feature disable -n allowGlobalConfirmation
```

*From a new PowerShell admin command prompt run:

dotnet new -i NUnit3.DotNetNew.Template

* From a standard commmand prompt run:

```batch
mkdir c:\dev\github.trondr
cd c:\dev\github.trondr
git clone https://github.com/trondr/DriverTool.git .\DriverTool
cd DriverTool
Build.cmd
```

## Development Environment

* From a PowerShell admin command prompt run:

```batch
try{Set-ExecutionPolicy Bypass -Scope Process -Force -ErrorAction SilentlyContinue}catch{}
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
choco feature enable -n allowGlobalConfirmation
choco install git
choco install git-credential-winstore
choco install netfx-4.8-devpack
choco install visualstudio2019buildtools
choco install visualstudio2019-workload-netcorebuildtools
choco install visualstudio2019-workload-manageddesktopbuildtools
$vsBuildToolsExe = "c:\windows\temp\vs_buildtools.exe"
if((Test-Path -Path $vsBuildToolsExe) -eq $false)
{
    Invoke-WebRequest -Uri "https://aka.ms/vs/16/release/vs_buildtools.exe" -OutFile "$vsBuildToolsExe"
}
. $vsBuildToolsExe --add "Microsoft.VisualStudio.Component.FSharp.MSBuild" --passive --norestart --quiet
choco install fake
choco upgrade fake
choco install nunit
choco install SourceTree
choco install notepadplusplus
choco install vscode
choco install visualstudio2019enterprise
# choco install visualstudio2019professional
choco install visualstudio2019-workload-manageddesktop
$vsInstallerExe = "c:\windows\temp\vs_enterprise.exe"
# $vsInstallerExe = "c:\windows\temp\vs_professional.exe"
if((Test-Path -Path $vsInstallerExe) -eq $false)
{
    Invoke-WebRequest -Uri "https://aka.ms/vs/16/release/vs_enterprise.exe" -OutFile "$vsInstallerExe"
}
. $vsInstallerExe --add "Microsoft.VisualStudio.Component.FSharp.Desktop" --passive --norestart --quiet
Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force
Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
Install-Module VSSetup
Install-Module psake
Set-PSRepository -Name PSGallery -InstallationPolicy Untrusted
choco feature disable -n allowGlobalConfirmation
```

# Debug PowerShell CmdLet  written in F# (Example)

## Executable
```
"c:\Windows\system32\WindowsPowerShell\v1.0\powershell.exe" 
```
## Arguments

```
-NoExit -Command "& Import-Module 'C:\Dev\github.trondr\DriverTool\src\app\DriverTool.PowerCLI.Library.FSharp\bin\Debug\net48\DriverTool.PowerCLI.Library.FSharp.dll' -Verbose;Get-DtDriverPack -Manufacturer Lenovo -ModelCode 20EQ -OperatingSystem win10 -Latest | Invoke-DtDownloadDriverPack"
```



## Build

```batch
.\Build.ps1 -BuildTarget Default
```
	
