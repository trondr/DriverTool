Set ExitCode=0
pushd "%~dp0"

REM dpinst.exe /s /sa
Set ExitCode=1073741826
REM Set ExitCode=3221291009

Set DpInstExitCode=%ExitCode%
%~dp0..\DpInstExitCode2ExitCode.exe %DpInstExitCode%

Set ExitCode=%errorlevel%
popd
EXIT /B %ExitCode%
