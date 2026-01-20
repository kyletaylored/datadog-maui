# IIS Express Datadog APM Setup

This guide explains how to enable Datadog APM instrumentation for IIS Express (used by Visual Studio for local development).

## ✅ Recommended Approach: launchSettings.json

The **ApiFramework** project is already configured with `Properties/launchSettings.json` that sets all required Datadog environment variables. This is the standard Visual Studio approach and requires no additional setup.

### Quick Start

1. **Install Datadog .NET Tracer MSI** (if not already installed):
   ```powershell
   # Download from GitHub releases
   # https://github.com/DataDog/dd-trace-dotnet/releases/latest
   # Run the installer: datadog-dotnet-apm-{version}-x64.msi
   ```

2. **Open the project in Visual Studio**

3. **Select "IIS Express" in the launch profile dropdown** (next to the Run button)

4. **Press F5 to run** - Datadog APM will be automatically enabled!

5. **Verify it's working:**
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

## Why This Works

IIS Express is a separate process launched by Visual Studio. For Datadog automatic instrumentation to work, the IIS Express process needs specific environment variables set before it starts.

Visual Studio reads `Properties/launchSettings.json` when you press F5 and automatically sets those environment variables for the IIS Express process.

## Required Environment Variables

The launchSettings.json file includes these variables:

### .NET Framework Applications
```json
"COR_ENABLE_PROFILING": "1",
"COR_PROFILER": "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}",
"COR_PROFILER_PATH_32": "C:\\Program Files\\Datadog\\.NET Tracer\\win-x86\\Datadog.Trace.ClrProfiler.Native.dll",
"COR_PROFILER_PATH_64": "C:\\Program Files\\Datadog\\.NET Tracer\\win-x64\\Datadog.Trace.ClrProfiler.Native.dll"
```

### .NET Core Applications (for compatibility)
```json
"CORECLR_ENABLE_PROFILING": "1",
"CORECLR_PROFILER": "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}",
"CORECLR_PROFILER_PATH_32": "C:\\Program Files\\Datadog\\.NET Tracer\\win-x86\\Datadog.Trace.ClrProfiler.Native.dll",
"CORECLR_PROFILER_PATH_64": "C:\\Program Files\\Datadog\\.NET Tracer\\win-x64\\Datadog.Trace.ClrProfiler.Native.dll"
```

### Common
```json
"DD_DOTNET_TRACER_HOME": "C:\\Program Files\\Datadog\\.NET Tracer",
"DD_SERVICE": "datadog-maui-api-framework",
"DD_ENV": "local",
"DD_VERSION": "1.0.0"
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

## Customizing launchSettings.json

You can customize the environment variables in `ApiFramework/Properties/launchSettings.json`:

```json
{
  "iisSettings": {
    "iisExpress": {
      "applicationUrl": "http://localhost:50000",
      "sslPort": 44300
    }
  },
  "profiles": {
    "IIS Express": {
      "commandName": "IISExpress",
      "launchBrowser": true,
      "environmentVariables": {
        // Add or modify variables here
        "DD_ENV": "dev",
        "DD_TRACE_DEBUG": "true"  // Enable debug logging
      }
    }
  }
}
```

## Troubleshooting

### Variables Not Set After Running

**Problem:** `dd-dotnet check` shows variables are not set

**Solutions:**
1. Verify you're running the "IIS Express" profile (check dropdown in Visual Studio)
2. Make sure launchSettings.json exists at `ApiFramework/Properties/launchSettings.json`
3. Close Visual Studio completely and reopen
4. Check that the Datadog .NET Tracer MSI is installed

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

**Solution:** Check for syntax errors in launchSettings.json:
- Make sure all JSON is valid
- Paths must use double backslashes: `C:\\Program Files\\...`
- All strings must be properly quoted

### Tracer Loaded But No Traces

**Problem:** dd-dotnet check shows success but no traces in Datadog

**Check:**
1. Verify API key is set in Web.config or environment
2. Check network connectivity to Datadog agent
3. Review application logs for Datadog errors
4. Ensure endpoints are actually being hit (make test requests)

## Alternative: Global Environment Variables

If launchSettings.json doesn't work for your setup (rare), you can set system-wide environment variables. This affects ALL .NET processes on your machine:

```powershell
# Run PowerShell as Administrator
[System.Environment]::SetEnvironmentVariable("COR_ENABLE_PROFILING", "1", "Machine")
[System.Environment]::SetEnvironmentVariable("COR_PROFILER", "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}", "Machine")
[System.Environment]::SetEnvironmentVariable("COR_PROFILER_PATH_32", "C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll", "Machine")
[System.Environment]::SetEnvironmentVariable("COR_PROFILER_PATH_64", "C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll", "Machine")
[System.Environment]::SetEnvironmentVariable("DD_DOTNET_TRACER_HOME", "C:\Program Files\Datadog\.NET Tracer", "Machine")
```

**After setting:**
- Restart your computer
- Run the project

**Warning:** This instruments every .NET Framework app on your computer. To remove:
```powershell
# Run as Administrator
[System.Environment]::SetEnvironmentVariable("COR_ENABLE_PROFILING", $null, "Machine")
[System.Environment]::SetEnvironmentVariable("COR_PROFILER", $null, "Machine")
# ... etc
```

## Benefits of launchSettings.json Approach

✅ **No scripts to run** - Configuration is automatic
✅ **Version controlled** - Can commit to git for team consistency
✅ **Standard approach** - How Visual Studio is designed to work
✅ **Per-project** - Doesn't affect other applications
✅ **Easy to customize** - Just edit JSON file
✅ **Works immediately** - No need to restart Visual Studio
✅ **Team-friendly** - Everyone gets the same configuration

## Additional Resources

- [Datadog .NET Tracer Documentation](https://docs.datadoghq.com/tracing/trace_collection/dd_libraries/dotnet-framework/)
- [Datadog .NET Tracer GitHub Releases](https://github.com/DataDog/dd-trace-dotnet/releases)
- [Visual Studio launchSettings.json Reference](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments)
