@echo off
REM Launch Visual Studio with Datadog environment variables for IIS Express

echo Setting Datadog environment variables...
set COR_ENABLE_PROFILING=1
set COR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
set COR_PROFILER_PATH=C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll
set DD_DOTNET_TRACER_HOME=C:\Program Files\Datadog\.NET Tracer

echo Starting Visual Studio...
REM Adjust the path to your Visual Studio installation
start "" "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe" DatadogMauiApi.Framework.sln

echo Visual Studio launched with Datadog profiling enabled.
echo IIS Express will inherit these environment variables when you run the project.
