@echo on
REM Configures the environment variables required to build KubernetesDebugger projects.
REM 
REM 	buildenv [ <source folder> ]
REM
REM Note that <source folder> defaults to the folder holding this
REM batch file.
REM
REM This must be [RUN AS ADMINISTRATOR].

REM Default KDBG_ROOT to the folder holding this batch file after stripping
REM off the trailing backslash.

set KDBG_ROOT=%~dp0 
set KDBG_ROOT=%KDBG_ROOT:~0,-2%

if not [%1]==[] set KDBG_ROOT=%1

if exist %KDBG_ROOT%\KubernetesDebugger.sln goto goodPath
echo The [%KDBG_ROOT%\KubernetesDebugger.sln] file does not exist.  Please pass the path
echo to the KubernetesDebugger solution folder.
goto done

:goodPath 

REM Configure the environment variables.

set KDBG_TOOLBIN=%KDBG_ROOT%\ToolBin
set KDBG_BUILD=%KDBG_ROOT%\Build
set KDBG_TEMP=C:\Temp

REM Persist the environment variables.

setx KDBG_ROOT "%KDBG_ROOT%" /M
setx KDBG_TOOLBIN "%KDBG_TOOLBIN%" /M
setx KDBG_BUILD "%KDBG_BUILD%" /M
setx KDBG_TEMP "%KDBG_TEMP%" /M
setx DOTNET_CLI_TELEMETRY_OPTOUT 1 /M

REM Make sure required folders exist.

if not exist "%KDBG_TEMP%" mkdir "%KDBG_TEMP%"
if not exist "%KDBG_TOOLBIN%" mkdir "%KDBG_TOOLBIN%"
if not exist "%KDBG_BUILD%" mkdir "%KDBG_BUILD%"

:done
@echo "============================================================================================"
@echo "* Be sure to close and reopen Visual Studio and any command windows to pick up the changes *"
@echo "============================================================================================"
pause
