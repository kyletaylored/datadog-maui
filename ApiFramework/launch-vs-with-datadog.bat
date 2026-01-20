@echo off
REM Launch Visual Studio with Datadog environment variables pre-set
REM This ensures IIS Express inherits the environment variables

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

echo.
echo Environment variables set for this session:
echo   COR_ENABLE_PROFILING=%COR_ENABLE_PROFILING%
echo   DD_SERVICE=%DD_SERVICE%
echo   DD_ENV=%DD_ENV%
echo.

echo Launching Visual Studio...
echo Visual Studio will inherit these environment variables.
echo Any IIS Express processes launched from VS will also inherit them.
echo.

REM Launch Visual Studio with the solution file
START "" "%ProgramFiles%\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe" "%~dp0DatadogMauiApi.Framework.sln"

REM If VS 2022 Community not found, try Professional
IF ERRORLEVEL 1 (
    START "" "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\Common7\IDE\devenv.exe" "%~dp0DatadogMauiApi.Framework.sln"
)

REM If VS 2022 not found, try VS 2019
IF ERRORLEVEL 1 (
    START "" "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\Common7\IDE\devenv.exe" "%~dp0DatadogMauiApi.Framework.sln"
)

echo.
echo Visual Studio launched with Datadog environment variables!
echo.
pause
