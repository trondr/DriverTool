@Echo Off
@pushd %~dp0

title Driver Package Installl

:: RunInNativeMode=True: Run PowerShell script in 64 bit process on 64 bit OS
:: RunInNativeMode=False: Run PowerShell script in 32 bit process on 64 bit OS
:: RunInNativeMode has no effect on 32 bit OS
@Set RunInNativeMode=True

IF %RunInNativeMode%==True goto RunInNativeMode
IF %RunInNativeMode%==False goto RunInNonNativeMode

:RunInNativeMode
@Echo Run PowerShell script in native mode (64 bit process on 64 bit OS and 32 bit process on 32 bit OS)
IF EXIST "%windir%\sysnative\cmd.exe" (Set WinSysDir=%windir%\sysnative) ELSE (Set WinSysDir=%windir%\System32)
goto RunPowerShellScript

:RunInNonNativeMode
@Echo Run PowerShell script in non-native mode (32 bit process on 64 bit OS and 32 bit process on 32 bit OS)
IF EXIST "%windir%\SysWOW64\cmd.exe" (Set WinSysDir=%windir%\SysWOW64) ELSE (Set WinSysDir=%windir%\System32)
goto RunPowerShellScript

:RunPowerShellScript
Set PowerShellExe=%WinSysDir%\WindowsPowershell\v1.0\PowerShell.exe
@Echo PowerShellExe=%PowerShellExe%
"%PowerShellExe%" -ExecutionPolicy Unrestricted -Command "& { . \"%~dp0Install.ps1\" %1 %2 %3 %4 %5 %6 %7 %8 %9; exit $LASTEXITCODE }"
@Set ExitCode=%errorlevel%

@popd
@Echo ExitCode=%ExitCode%
@Exit /B %ExitCode%