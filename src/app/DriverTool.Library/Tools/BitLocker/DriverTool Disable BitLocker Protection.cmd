@Echo Off
@pushd %~dp0

title DriverTool Resume BitLocker Protection

:: RunInNativeMode=True: Run script in 64 bit process on 64 bit OS
:: RunInNativeMode=False: Run script in 32 bit process on 64 bit OS
:: RunInNativeMode has no effect on 32 bit OS
@Set RunInNativeMode=True

IF %RunInNativeMode%==True goto RunInNativeMode
IF %RunInNativeMode%==False goto RunInNonNativeMode

:RunInNativeMode
@Echo Run script in native mode (64 bit process on 64 bit OS and 32 bit process on 32 bit OS)
IF EXIST "%windir%\sysnative\cmd.exe" (Set WinSysDir=%windir%\sysnative) ELSE (Set WinSysDir=%windir%\System32)
goto RunScript

:RunInNonNativeMode
@Echo Run script in non-native mode (32 bit process on 64 bit OS and 32 bit process on 32 bit OS)
IF EXIST "%windir%\SysWOW64\cmd.exe" (Set WinSysDir=%windir%\SysWOW64) ELSE (Set WinSysDir=%windir%\System32)
goto RunScript

:RunScript
Set ManageBdeExe=%WinSysDir%\manage-bde.exe
@Echo ManageBdeExe=%ManageBdeExe%
"%ManageBdeExe%" -protectors -disable C:
@Set ExitCode=%errorlevel%
@Echo ExitCode=%ExitCode%

Set SchTasksExe=%WinSysDir%\schtasks.exe
@Echo SchTasksExe=%SchTasksExe%
"%SchTasksExe%" /Create /tn "DriverTool Resume BitLocker Protection" /XML "%~dp0DriverTool Resume BitLocker Protection.xml"
@Set ExitCode=%errorlevel%
@Echo ExitCode=%ExitCode%

@popd
@Echo ExitCode=%ExitCode%
@Exit /B %ExitCode%