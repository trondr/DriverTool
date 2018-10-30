Set ExitCode=0
pushd "%~dp0"

Set ExitCode=360

Set DpInstExitCode=%ExitCode%
%~dp0..\DpInstExitCode2ExitCode.exe %DpInstExitCode%

Set ExitCode=%errorlevel%
popd
EXIT /B %ExitCode%
