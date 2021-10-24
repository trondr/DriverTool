# DriverTool.PowerCLI
PowerShell command line interface for ...

## Installation
Clone this repository into your personal Powershell Modules folder:

```powershell
$modulesFolder = $env:PSModulePath -Split ";" | Select-Object -First 1
mkdir $modulesFolder -ErrorAction Ignore
cd $modulesFolder
git clone https://github.com/trondr/DriverTool/trondr/DriverTool.PowerCLI.git
Import-Module DriverTool.PowerCLI
```

Powershell should now be able to discover module commands.