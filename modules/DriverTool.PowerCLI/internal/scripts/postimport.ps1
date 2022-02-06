# Place all code that should be run after functions are imported here
$global:DriverToolExe = [System.IO.Path]::Combine($global:ModuleRootPath,"binary","DriverTool.PowerCLI.Library.FSharp","DriverTool.exe")
Assert-DtFileExists -Path $DriverToolExe -Message "DriverTool.exe ($driverToolExe) not found."