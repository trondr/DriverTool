﻿@{
	# Script module or binary module file associated with this manifest
	RootModule = 'DriverTool.PowerCLI.psm1'
	
	# Version number of this module.
	ModuleVersion = '1.0.0'
	
	# ID used to uniquely identify this module
	GUID = '35971a7e-b1f9-4932-82a1-29597fb6f022'
	
	# Author of this module
	Author = 'github/trondr'
	
	# Company or vendor of this module
	CompanyName = 'github/trondr'
	
	# Copyright statement for this module
	Copyright = 'Copyright (c) 2021 github/trondr'
	
	# Description of the functionality provided by this module
	Description = 'Download drivers and updates for Dell, HP and Lenovo PC models. Package drivers and updates for import into SCCM as a package or Application.'
	
	# Minimum version of the Windows PowerShell engine required by this module
	PowerShellVersion = '5.1'
	
	# Modules that must be imported into the global environment prior to importing this module
	# RequiredModules = @(@{ ModuleName='PSFramework'; ModuleVersion='1.6.205' })
	
	# Assemblies that must be loaded prior to importing this module
	# RequiredAssemblies = @('bin\DriverTool.PowerCLI.dll')
	
	# Type files (.ps1xml) to be loaded when importing this module
	# Expensive for import time, no more than one should be used.
	# TypesToProcess = @('xml\DriverTool.PowerCLI.Types.ps1xml')
	
	# Format files (.ps1xml) to be loaded when importing this module.
	# Expensive for import time, no more than one should be used.
	FormatsToProcess = @('DriverTool.PowerCLI.Format.ps1xml')
	
	# Functions to export from this module
	FunctionsToExport = @("Invoke-DtDriverTool","Get-DtCmDeviceModel")
	
	# Cmdlets to export from this module
	CmdletsToExport = @('Get-DtDriverPack','Invoke-DtDownloadDriverPack')
	
	# Variables to export from this module
	VariablesToExport = @()
	
	# Aliases to export from this module
	AliasesToExport = @()
	
	# List of all files packaged with this module
	FileList = @('')
	
	# Private data to pass to the module specified in ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
	PrivateData = @{
		
		#Support for PowerShellGet galleries.
		PSData = @{
			
			# Tags applied to this module. These help with module discovery in online galleries.
			Tags = @("DriverTool.PowerCLI","DriverTool","PowerCLI","Drivers","Driver Updates")
			
			# A URL to the license for this module.
			# LicenseUri = 'https://github.com/trondr/DriverTool/LICENSE'
			
			# A URL to the main website for this project.
			# ProjectUri = 'https://github.com/trondr/DriverTool'
			
			# A URL to an icon representing this module.
			# IconUri = ''
			
			# ReleaseNotes of this module
			# ReleaseNotes = ''
			
		} # End of PSData hashtable
		
	} # End of PrivateData hashtable
}