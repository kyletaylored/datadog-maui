# IIS Express Datadog APM Setup

This guide explains how to enable Datadog APM instrumentation for IIS Express (used by Visual Studio for local development).

## ✅ Recommended Approach: Launch Script

The **ApiFramework** project includes a batch file that launches Visual Studio with Datadog environment variables pre-configured.

**The Challenge:** .NET Framework projects with IIS Express don't reliably pick up environment variables from `launchSettings.json` or `.csproj.user` files due to how Visual Studio spawns the IIS Express process.

**The Solution:** Launch Visual Studio itself with environment variables already set. When VS launches IIS Express, it inherits those variables.

### Quick Start

1. **Install Datadog .NET Tracer MSI** (if not already installed):
   ```powershell
   # Download from GitHub releases
   # https://github.com/DataDog/dd-trace-dotnet/releases/latest
   # Run the installer: datadog-dotnet-apm-{version}-x64.msi
   ```

   The MSI installs the tracer to:
   - `C:\Program Files\Datadog\.NET Tracer\`

2. **Launch Visual Studio with the batch file:**
   ```powershell
   .\ApiFramework\launch-vs-with-datadog.bat
   ```

   The script will:
   - Auto-detect your Visual Studio installation (any version: 2019, 2022, 2026, etc.)
   - Set all required Datadog environment variables
   - Launch Visual Studio with the solution

3. **Press F5 to debug** - Datadog APM will work automatically!

4. **Verify it's working:**
   ```powershell
   # Get the PID from Task Manager (iisexpress.exe)
   dd-dotnet check process <PID>
   ```

   You should see:
   ```
   [SUCCESS]: The environment variable COR_ENABLE_PROFILING is set to the correct value of 1
   [SUCCESS]: The environment variable COR_PROFILER is set to the correct value of {846F5F1C-F9AE-4B07-969E-05C26BC060D8}
   [SUCCESS]: The tracer version X.X.X is loaded into the process
   ```

### How It Works

The batch file (`launch-vs-with-datadog.bat`) does three things:

1. **Uses `vswhere.exe`** to automatically locate your Visual Studio installation (works with any version/edition)
2. **Sets environment variables** in the batch session:
   - `COR_ENABLE_PROFILING=1`
   - `COR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}`
   - Profiler DLL paths for 32-bit and 64-bit
   - Datadog service tags (DD_SERVICE, DD_ENV, DD_VERSION)
3. **Launches Visual Studio** which inherits these variables, and subsequently IIS Express inherits them too

This approach is simple, version-agnostic, and doesn't require any project file modifications or extensions.

## Required Environment Variables

The batch file sets these variables:

### .NET Framework Applications
```
COR_ENABLE_PROFILING=1
COR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
COR_PROFILER_PATH_32=C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll
COR_PROFILER_PATH_64=C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll
```

### .NET Core Applications (for compatibility)
```
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
CORECLR_PROFILER_PATH_32=C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll
CORECLR_PROFILER_PATH_64=C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll
```

### Common
```
DD_DOTNET_TRACER_HOME=C:\Program Files\Datadog\.NET Tracer
DD_SERVICE=datadog-maui-api-framework
DD_ENV=local
DD_VERSION=1.0.0
```

## What Each Variable Does

| Variable | Purpose |
|----------|---------|
| `COR_ENABLE_PROFILING` | Tells .NET Framework runtime to enable CLR profiling |
| `COR_PROFILER` | GUID of the COM profiler to load (Datadog's profiler) |
| `COR_PROFILER_PATH_32` | Full path to 32-bit Datadog native profiler DLL |
| `COR_PROFILER_PATH_64` | Full path to 64-bit Datadog native profiler DLL |
| `CORECLR_ENABLE_PROFILING` | Same as COR_ENABLE_PROFILING but for .NET Core |
| `CORECLR_PROFILER` | Same as COR_PROFILER but for .NET Core |
| `CORECLR_PROFILER_PATH_32/64` | Paths to Datadog profiler DLLs for .NET Core |
| `DD_DOTNET_TRACER_HOME` | Base directory where Datadog tracer is installed |
| `DD_SERVICE` | Service name for APM traces |
| `DD_ENV` | Environment name (local, dev, staging, prod) |
| `DD_VERSION` | Application version for tracking deployments |

## Customizing the Batch File

You can edit `ApiFramework/launch-vs-with-datadog.bat` to customize:

- **Service name**: Change `DD_SERVICE` value
- **Environment**: Change `DD_ENV` value (local, dev, etc.)
- **Version**: Change `DD_VERSION` value

Example:
```batch
SET DD_SERVICE=my-custom-service
SET DD_ENV=dev
SET DD_VERSION=2.0.0
```

## Troubleshooting

### vswhere.exe Not Found

**Problem:** Script can't find `vswhere.exe`

**Solution:** Install Visual Studio 2019 or later. `vswhere.exe` is included with all modern VS versions at:
```
C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe
```

### Variables Not Set After Running

**Problem:** `dd-dotnet check` shows variables are not set

**Solutions:**
1. Make sure you launched VS using the batch file (not by double-clicking the .sln)
2. Close all Visual Studio instances and run the batch file again
3. Check that the Datadog .NET Tracer MSI is installed

### Wrong Paths

**Problem:** Profiler DLL not found

**Solution:** Verify Datadog .NET Tracer is installed:
```powershell
Test-Path "C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll"
```

If it returns `False`, install the Datadog .NET Tracer MSI from:
https://github.com/DataDog/dd-trace-dotnet/releases/latest

### IIS Express Won't Start

**Problem:** Connection refused or IIS Express crashes

**Solution:**
1. Check for port conflicts (default is 50000)
2. Review IIS Express logs in `%USERPROFILE%\Documents\IISExpress\Logs`
3. Try resetting IIS Express configuration:
   ```powershell
   # Delete IIS Express config
   Remove-Item "$env:USERPROFILE\Documents\IISExpress\config" -Recurse -Force
   # Restart Visual Studio
   ```

### Tracer Loaded But No Traces

**Problem:** dd-dotnet check shows success but no traces in Datadog

**Check:**
1. Verify API key is set in Web.config or environment
2. Check network connectivity to Datadog agent
3. Review application logs for Datadog errors
4. Ensure endpoints are actually being hit (make test requests)

## Why Not Use launchSettings.json or .csproj.user?

We tried several approaches:

1. **`launchSettings.json`**: Doesn't work for .NET Framework projects - Visual Studio ignores environment variables when using `commandName: IISExpress`
2. **`.csproj.user` EnvironmentVariables property**: Not reliably applied to IIS Express process for .NET Framework projects
3. **Global system environment variables**: Works but affects ALL .NET apps on the machine (not project-specific)

The batch file approach is:
- ✅ Project-specific (doesn't affect other apps)
- ✅ Version-controlled (can be committed to git)
- ✅ Simple and maintainable
- ✅ Works across all VS versions automatically
- ✅ No installation or extensions required

## Additional Resources

- [Datadog .NET Tracer Documentation](https://docs.datadoghq.com/tracing/trace_collection/dd_libraries/dotnet-framework/)
- [Datadog .NET Tracer GitHub Releases](https://github.com/DataDog/dd-trace-dotnet/releases)
- [vswhere Documentation](https://github.com/microsoft/vswhere)
