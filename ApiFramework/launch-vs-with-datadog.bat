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
echo.

REM Try Program Files (x86) first (default location)
SET "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
echo Checking Program Files ^(x86^) location...

IF NOT EXIST "%VSWHERE%" (
    echo Not found, checking Program Files location...
    SET "VSWHERE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
)

IF NOT EXIST "%VSWHERE%" (
    echo.
    echo ERROR: vswhere.exe not found in either location
    echo.
    echo Please install Visual Studio 2019 or later
    echo.
    pause
    exit /b 1
)

echo [OK] Found vswhere.exe
echo.

REM Find the latest Visual Studio installation
echo Running vswhere to find VS installation...
FOR /F "usebackq tokens=*" %%i IN (`"%VSWHERE%" -latest -property installationPath`) DO (
    SET "VS_PATH=%%i"
)

echo VS_PATH = %VS_PATH%
echo.

IF "%VS_PATH%"=="" (
    echo ERROR: No Visual Studio installation found
    echo.
    echo Running vswhere diagnostics...
    echo.
    echo All installed versions:
    "%VSWHERE%" -all -property installationPath
    echo.
    pause
    exit /b 1
)

SET "DEVENV=%VS_PATH%\Common7\IDE\devenv.exe"
echo Looking for devenv.exe at: %DEVENV%
echo.

IF NOT EXIST "%DEVENV%" (
    echo ERROR: devenv.exe not found at: %DEVENV%
    echo.
    echo Checking if path exists...
    IF EXIST "%VS_PATH%" (
        echo VS_PATH directory exists, but devenv.exe is missing
        echo Directory contents:
        dir "%VS_PATH%\Common7\IDE\devenv.exe"
    ) ELSE (
        echo VS_PATH directory does not exist
    )
    echo.
    pause
    exit /b 1
)

echo [OK] Found Visual Studio at: %VS_PATH%
echo [OK] Found devenv.exe
echo.

echo Launching Visual Studio with Datadog environment...
SET "SOLUTION_FILE=%~dp0DatadogMauiApi.Framework.sln"
echo Solution: %SOLUTION_FILE%
echo.

REM Verify solution file exists
IF NOT EXIST "%SOLUTION_FILE%" (
    echo ERROR: Solution file not found at: %SOLUTION_FILE%
    echo.
    echo Current directory: %CD%
    echo Script directory: %~dp0
    echo.
    pause
    exit /b 1
)

echo [OK] Solution file exists
echo.
echo Visual Studio will inherit these environment variables.
echo Any IIS Express processes launched from VS will also inherit them.
echo.
echo Executing: START "" "%DEVENV%" "%SOLUTION_FILE%"
echo.

REM Launch Visual Studio with the solution file
START "" "%DEVENV%" "%SOLUTION_FILE%"

IF ERRORLEVEL 1 (
    echo ERROR: Failed to launch Visual Studio (error level: %ERRORLEVEL%)
    echo.
    pause
    exit /b 1
)

echo.
echo [OK] Visual Studio launch command executed successfully!
echo.
echo The Visual Studio window should open in a few seconds.
echo.
echo Next steps:
echo 1. Press F5 in Visual Studio to start debugging
echo 2. Get the IIS Express process ID from Task Manager
echo 3. Verify: dd-dotnet check process [PID]
echo.
echo Press any key to close this window...
pause >nul
