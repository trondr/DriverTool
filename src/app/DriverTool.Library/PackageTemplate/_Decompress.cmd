@Echo Off
@pushd %~dp0

title Decompress .\Drivers.zip to .\Drivers folder

@Echo Run DriverTool InstallDriverPacakge
Set DriverToolExe=%~dp0DriverTool\DriverTool.exe
@Echo DriverToolExe=%DriverToolExe%
"%DriverToolExe%" DecompressDriverPackage /driverPackagePath="%~dp0"
@Set ExitCode=%errorlevel%

@popd
@Echo ExitCode=%ExitCode%
@Exit /B %ExitCode%