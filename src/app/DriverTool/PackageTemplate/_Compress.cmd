@Echo Off
@pushd %~dp0

title Compress .\Drivers folder to .\Drivers.zip

@Echo Run DriverTool InstallDriverPacakge
Set DriverToolExe=%~dp0DriverTool\DriverTool.exe
@Echo DriverToolExe=%DriverToolExe%
"%DriverToolExe%" CompressDriverPackage /driverPackagePath="%~dp0"
@Set ExitCode=%errorlevel%

@popd
@Echo ExitCode=%ExitCode%
@Exit /B %ExitCode%