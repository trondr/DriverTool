@Echo Off
@pushd %~dp0

title Install driver package

@Echo Run DriverTool InstallDriverPacakge
Set DriverToolExe=%~dp0DriverTool\DriverTool.exe
@Echo DriverToolExe=%DriverToolExe%
"%DriverToolExe%" InstallDriverPackage /driverPackagePath="%~dp0"
@Set ExitCode=%errorlevel%

@popd
@Echo ExitCode=%ExitCode%
@Exit /B %ExitCode%