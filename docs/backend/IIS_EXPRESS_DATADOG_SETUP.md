# IIS Express Datadog APM Setup

This guide explains how to enable Datadog APM instrumentation for IIS Express (used by Visual Studio for local development).

## ⚠️ Known Issue: COR_ENABLE_PROFILING

Even after running the configuration script, `COR_ENABLE_PROFILING` may not be picked up by the IIS Express process. This is a known limitation of how IIS Express loads environment variables from applicationHost.config.

**The updated script (v2) now sets variables in TWO locations:**
1. **Application Pool Defaults** (`<applicationPoolDefaults>`) - Primary location
2. **system.webServer** - Fallback global location

If this still doesn't work, use **Method 3: System Environment Variable** below (last resort).

## Why IIS Express Needs Configuration

IIS Express is a separate process launched by Visual Studio. For Datadog automatic instrumentation to work, the IIS Express process needs specific environment variables set before it starts. These environment variables tell the .NET runtime to load the Datadog profiler.

## Required Environment Variables

### .NET Framework Applications (like ApiFramework)
```
COR_ENABLE_PROFILING=1
COR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
COR_PROFILER_PATH=C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll
DD_DOTNET_TRACER_HOME=C:\Program Files\Datadog\.NET Tracer
```

### .NET Core Applications (for compatibility)
```
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
CORECLR_PROFILER_PATH_64=C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll
CORECLR_PROFILER_PATH_32=C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll
DD_DOTNET_TRACER_HOME=C:\Program Files\Datadog\.NET Tracer
```

## What Each Variable Does

| Variable | Purpose |
|----------|---------|
| `COR_ENABLE_PROFILING` | Tells .NET Framework runtime to enable CLR profiling |
| `COR_PROFILER` | GUID of the COM profiler to load (Datadog's profiler) |
| `COR_PROFILER_PATH` | Full path to Datadog's native profiler DLL |
| `CORECLR_ENABLE_PROFILING` | Same as COR_ENABLE_PROFILING but for .NET Core |
| `CORECLR_PROFILER` | Same as COR_PROFILER but for .NET Core |
| `CORECLR_PROFILER_PATH_64` | Path to 64-bit profiler for .NET Core |
| `CORECLR_PROFILER_PATH_32` | Path to 32-bit profiler for .NET Core |
| `DD_DOTNET_TRACER_HOME` | Base directory where Datadog tracer is installed |

## Setup Methods

### Method 1: Automated Script (Recommended)

Run the PowerShell script that modifies IIS Express configuration:

```powershell
cd ApiFramework
.\configure-iis-express.ps1
```

**What this script does:**
1. Backs up your IIS Express `applicationHost.config`
2. Adds environment variables to the `<system.webServer>/<environmentVariables>` section
3. Saves the modified config

**Config location:**
```
%USERPROFILE%\Documents\IISExpress\config\applicationHost.config
```

**After running:**
- Close Visual Studio completely
- Reopen Visual Studio
- Run project (F5)
- IIS Express will inherit the environment variables

### Method 2: Manual Configuration

If you prefer to manually edit the config:

1. **Close Visual Studio completely**

2. **Open the IIS Express config file:**
   ```
   %USERPROFILE%\Documents\IISExpress\config\applicationHost.config
   ```

3. **Find the `<system.webServer>` section** (around line 150-200)

4. **Add or update the `<environmentVariables>` section:**
   ```xml
   <system.webServer>
       <!-- Other sections... -->

       <environmentVariables>
           <!-- .NET Framework -->
           <add name="COR_ENABLE_PROFILING" value="1" />
           <add name="COR_PROFILER" value="{846F5F1C-F9AE-4B07-969E-05C26BC060D8}" />
           <add name="COR_PROFILER_PATH" value="C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll" />

           <!-- .NET Core -->
           <add name="CORECLR_ENABLE_PROFILING" value="1" />
           <add name="CORECLR_PROFILER" value="{846F5F1C-F9AE-4B07-969E-05C26BC060D8}" />
           <add name="CORECLR_PROFILER_PATH_64" value="C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll" />
           <add name="CORECLR_PROFILER_PATH_32" value="C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll" />

           <!-- Common -->
           <add name="DD_DOTNET_TRACER_HOME" value="C:\Program Files\Datadog\.NET Tracer" />
       </environmentVariables>
   </system.webServer>
   ```

5. **Save the file**

6. **Reopen Visual Studio and run the project**

### Method 3: System Environment Variable (Last Resort)

If the automated script and manual config still don't set `COR_ENABLE_PROFILING`, set it as a system environment variable. This affects ALL .NET processes on your machine but is sometimes the only way to make IIS Express pick it up:

```powershell
# Run PowerShell as Administrator
[System.Environment]::SetEnvironmentVariable("COR_ENABLE_PROFILING", "1", "Machine")
[System.Environment]::SetEnvironmentVariable("COR_PROFILER", "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}", "Machine")
[System.Environment]::SetEnvironmentVariable("COR_PROFILER_PATH_32", "C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll", "Machine")
[System.Environment]::SetEnvironmentVariable("COR_PROFILER_PATH_64", "C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll", "Machine")
[System.Environment]::SetEnvironmentVariable("DD_DOTNET_TRACER_HOME", "C:\Program Files\Datadog\.NET Tracer", "Machine")
```

**After setting:**
- Restart Visual Studio completely
- Restart your computer (recommended)
- Run the project

**Warning:** This instruments every .NET Framework app on your computer, which can cause performance overhead. To remove:
```powershell
# Run as Administrator
[System.Environment]::SetEnvironmentVariable("COR_ENABLE_PROFILING", $null, "Machine")
[System.Environment]::SetEnvironmentVariable("COR_PROFILER", $null, "Machine")
# ... etc
```

## Verification

After configuration, verify it's working:

1. **Start your application in Visual Studio (F5)**

2. **Get the IIS Express process ID:**
   - Check Visual Studio Output window
   - Or use Task Manager → Details tab → iisexpress.exe

3. **Run the Datadog diagnostic tool:**
   ```powershell
   dd-trace check process <process-id>
   ```

4. **Look for SUCCESS messages:**
   ```
   [SUCCESS]: The environment variable COR_ENABLE_PROFILING is set to the correct value of 1
   [SUCCESS]: The environment variable COR_PROFILER is set to the correct value of {846F5F1C-F9AE-4B07-969E-05C26BC060D8}
   [SUCCESS]: The tracer version X.X.X is loaded into the process
   ```

## Troubleshooting

### Variables Not Set After Configuration

**Problem:** `dd-trace check` shows variables are not set

**Solutions:**
1. Make sure you completely closed and reopened Visual Studio
2. Check the config file was actually modified: `%USERPROFILE%\Documents\IISExpress\config\applicationHost.config`
3. Ensure the `<environmentVariables>` section is under `<system.webServer>`, not elsewhere
4. Try restarting your computer (clears any cached configs)

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

**Solution:** Restore the backup config:
```powershell
.\restore-iis-express.ps1
```

Or manually restore from:
```
%USERPROFILE%\Documents\IISExpress\config\applicationHost.config.backup.<timestamp>
```

### Only Some Endpoints Traced

**Problem:** Auto-instrumentation works inconsistently

**Cause:** This is usually NOT an IIS Express config issue - it's code-level. Check:
- SessionManager isn't creating manual spans with `StartActive()` (use `ActiveScope` instead)
- Controllers are adding tags to active spans, not creating new ones
- No middleware interfering with the profiler

## How the Automated Script Works

The `configure-iis-express.ps1` script does the following:

```powershell
# 1. Finds your IIS Express config
$configPath = "$env:USERPROFILE\Documents\IISExpress\config\applicationHost.config"

# 2. Creates a timestamped backup
$backup = "$configPath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
Copy-Item $configPath $backup

# 3. Loads the XML
[xml]$config = Get-Content $configPath

# 4. Finds or creates <environmentVariables> section under <system.webServer>
$envVarsSection = $config.configuration.'system.webServer'.SelectSingleNode("//environmentVariables")

# 5. Adds each environment variable as <add name="..." value="..." />
foreach ($var in $ddVars) {
    $addElement = $config.CreateElement("add")
    $addElement.SetAttribute("name", $var.Key)
    $addElement.SetAttribute("value", $var.Value)
    $envVarsSection.AppendChild($addElement)
}

# 6. Saves the modified config
$config.Save($configPath)
```

## Alternative: Per-Project Configuration

If you don't want to modify global IIS Express config, you can create a project-specific `applicationhost.config` in your `.vs` folder, but this is more complex and not recommended for most scenarios.

## Best Practice

For **production IIS** (not IIS Express), set environment variables on the Application Pool instead:
1. Open IIS Manager
2. Application Pools → Select your pool
3. Advanced Settings → Environment Variables
4. Add the same variables

Or use the Datadog MSI installer which configures IIS globally.

## Why We Don't Use Web.config

You might wonder why we can't just put these in `Web.config`. The reason is:

- `Web.config` is read by the **application** after it starts
- The profiler needs to be loaded by the **.NET runtime** before the application starts
- Environment variables are the only way to tell the runtime to load the profiler at startup

Think of it this way:
- Environment variables → Tell the .NET runtime what to do when starting the process
- Web.config → Tell your application what to do after it's already running
