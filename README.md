# DriverTool

Downloads drivers and software for a specific PC model and creates a driver package that can be imported into SCCM as a package or application.

## Command line help

```
DriverTool 1.0.0.0 - Downloads drivers and software for a specific PC model and creates a driver package that can be imported into SCCM as a package or application.
Copyright © <github.com/trondr> 2018-2019
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

   Example: DriverTool.exe ExportRemoteUdateInfo /csvFileName="c:\temp\updates.csv" /overWrite="False" /manufacturer="LENOVO" /modelCode="20EQ" /operatingSystemCode="WIN10X64" 
   Example (alternative): DriverTool.exe ExportRemoteUdateInfo /f="c:\temp\updates.csv" /o="False" /ma="LENOVO" /m="20EQ" /op="WIN10X64" 


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

   Example: DriverTool.exe ExportLocalUdateInfo /csvFileName="c:\temp\updates.csv" /overWrite="False" 
   Example (alternative): DriverTool.exe ExportLocalUdateInfo /f="c:\temp\updates.csv" /o="False" 


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

   Example: DriverTool.exe CreateDriverPackage /destinationFolder="c:\temp\Drivers\SomeModel" /packagePublisher="MyCompany" /manufacturer="LENOVO" /systemFamily="ThinkPad P50" /modelCode="20EQ" /operatingSystemCode="WIN10X64" /baseOnLocallyInstalledUpdates="False" 
   Example (alternative): DriverTool.exe CreateDriverPackage /df="c:\temp\Drivers\SomeModel" /pu="MyCompany" /ma="LENOVO" /sf="ThinkPad P50" /mo="20EQ" /op="WIN10X64" /lu="False" 


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


## Build

* Install chocolatey 
	```batch
	@"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))" && SET "PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin"
	```
* Install fake
	
	```batch
	choco install fake -pre
	```
* Install NUnit Template

	```batch
	dotnet new -i NUnit3.DotNetNew.Template
	```
	
* Build
	
	```batch
	fake run build.fsx
	```