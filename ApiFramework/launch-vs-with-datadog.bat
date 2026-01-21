@echo off
REM Launch Visual Studio with Datadog environment variables pre-set
REM This ensures IIS Express inherits the environment variables

echo ========================================
echo Datadog Visual Studio Launcher
echo ========================================
echo.
echo Setting Datadog environment variables for this session...

SET COR_ENABLE_PROFILING=1
SET COR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
SET COR_PROFILER_PATH_32=C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll
SET COR_PROFILER_PATH_64=C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll
SET CORECLR_ENABLE_PROFILING=1
SET CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
SET CORECLR_PROFILER_PATH_32=C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll
SET CORECLR_PROFILER_PATH_64=C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll
SET DD_DOTNET_TRACER_HOME=C:\Program Files\Datadog\.NET Tracer
SET DD_SERVICE=datadog-maui-api-framework
SET DD_ENV=local
SET DD_VERSION=1.0.0

echo [OK] Environment variables set
echo.

REM Use vswhere.exe to find Visual Studio installation
echo Locating Visual Studio installation...
SET "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

IF NOT EXIST "%VSWHERE%" (
    echo ERROR: vswhere.exe not found at: %VSWHERE%
    echo Please install Visual Studio 2019 or later
    echo.
    pause
    exit /b 1
)

REM Find the latest Visual Studio installation
FOR /F "usebackq tokens=*" %%i IN (`"%VSWHERE%" -latest -property installationPath`) DO (
    SET "VS_PATH=%%i"
)

IF "%VS_PATH%"=="" (
    echo ERROR: No Visual Studio installation found
    echo.
    pause
    exit /b 1
)

SET "DEVENV=%VS_PATH%\Common7\IDE\devenv.exe"

IF NOT EXIST "%DEVENV%" (
    echo ERROR: devenv.exe not found at: %DEVENV%
    echo.
    pause
    exit /b 1
)

echo [OK] Found Visual Studio at: %VS_PATH%
echo.

echo Launching Visual Studio with Datadog environment...
echo Solution: %~dp0DatadogMauiApi.Framework.sln
echo.
echo Visual Studio will inherit these environment variables.
echo Any IIS Express processes launched from VS will also inherit them.
echo.

REM Launch Visual Studio with the solution file
START "" "%DEVENV%" "%~dp0DatadogMauiApi.Framework.sln"

IF ERRORLEVEL 1 (
    echo ERROR: Failed to launch Visual Studio
    echo.
    pause
    exit /b 1
)

echo.
echo [OK] Visual Studio launched successfully!
echo.
echo Next steps:
echo 1. Press F5 in Visual Studio to start debugging
echo 2. Get the IIS Express process ID from Task Manager
echo 3. Verify: dd-dotnet check process [PID]
echo.
pause
