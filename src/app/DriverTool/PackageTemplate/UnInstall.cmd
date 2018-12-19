@Echo Off
@pushd %~dp0

title Uninstall driver package

@Echo Run DriverTool InstallDriverPacakge
Set DriverToolExe=%~dp0DriverTool\DriverTool.exe
@Echo DriverToolExe=%DriverToolExe%
"%DriverToolExe%" UnInstallDriverPackage /driverPackagePath="%~dp0"
@Set ExitCode=%errorlevel%

@popd
@Echo ExitCode=%ExitCode%
@Exit /B %ExitCode%