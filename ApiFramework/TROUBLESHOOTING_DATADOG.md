# Troubleshooting Datadog APM in IIS Express

This guide helps diagnose and fix issues when Datadog APM tracing isn't working in IIS Express.

## Quick Diagnostic Checklist

When you see `[Datadog OWIN] WARNING: No active span found`, work through this checklist:

- [ ] Datadog .NET Tracer is installed
- [ ] Visual Studio was launched with `launch-vs-with-datadog.bat`
- [ ] IIS Express process shows environment variables
- [ ] Profiler DLLs exist at specified paths
- [ ] Application is using OWIN mode (has `USE_OWIN` compilation symbol)

## Understanding the Diagnostic Output

### On First Request

The OWIN middleware logs comprehensive diagnostics on the first request. Here's what to look for:

#### ✅ Good Output (Everything Working)

```
========================================
[Datadog Diagnostics] Tracer Status
========================================
[Datadog] Tracer.Instance: True
[Datadog] Tracer.DefaultServiceName: datadog-maui-api-framework
[Datadog] Settings.Environment: local
[Datadog] Settings.ServiceName: datadog-maui-api-framework
[Datadog] Settings.TraceEnabled: True
----------------------------------------
[Datadog Diagnostics] Environment Variables
----------------------------------------
[Datadog] COR_ENABLE_PROFILING = 1
[Datadog] COR_PROFILER = {846F5F1C-F9AE-4B07-969E-05C26BC060D8}
[Datadog] COR_PROFILER_PATH_64 = C:\Program Files\Datadog\.NET Tracer\win-x64\Datado...
[Datadog] DD_SERVICE = datadog-maui-api-framework
[Datadog] DD_ENV = local
[Datadog] DD_TRACE_ENABLED = (not set)
[Datadog] DD_TRACE_DEBUG = true
========================================
[Datadog OWIN] Request: GET /health
[Datadog OWIN] ✅ Captured span: aspnet.request (SpanId: 12345, TraceId: 67890)
```

#### ❌ Bad Output (Tracer Not Working)

```
========================================
[Datadog Diagnostics] Tracer Status
========================================
[Datadog] Tracer.Instance: True
[Datadog] Tracer.DefaultServiceName: (null)
[Datadog] Could not read tracer settings: Object reference not set to an instance of an object
----------------------------------------
[Datadog Diagnostics] Environment Variables
----------------------------------------
[Datadog] COR_ENABLE_PROFILING = (not set)  ⚠️ PROBLEM!
[Datadog] COR_PROFILER = (not set)
[Datadog] COR_PROFILER_PATH_64 = (not set)
[Datadog] DD_SERVICE = (not set)
========================================
[Datadog OWIN] Request: GET /health
[Datadog OWIN] ❌ WARNING: No active span found
[Datadog OWIN] This usually means:
[Datadog OWIN]   1. Datadog .NET Tracer is not installed
[Datadog OWIN]   2. COR_ENABLE_PROFILING environment variable is not set to 1
[Datadog OWIN]   3. IIS Express was not started with Datadog environment variables
[Datadog OWIN]   4. The .NET Tracer profiler DLL failed to attach
```

## Common Issues and Solutions

### Issue 1: `COR_ENABLE_PROFILING = (not set)`

**Symptom:** Environment variables show `(not set)` for all Datadog variables.

**Cause:** Visual Studio was not launched with the batch file, or environment variables didn't inherit to IIS Express.

**Solution:**

1. **Close Visual Studio completely** (important!)

2. **Run the batch file:**
   ```cmd
   ApiFramework\launch-vs-with-datadog.bat
   ```

3. **Verify batch file output:**
   ```
   [OK] Environment variables set
   [OK] Found Datadog .NET Tracer at: C:\Program Files\Datadog\.NET Tracer
   [OK] Datadog profiler DLLs verified
   [OK] Found Visual Studio at: C:\Program Files\Microsoft Visual Studio\2022\...
   [OK] Visual Studio launch command executed successfully!
   ```

4. **Press F5 in Visual Studio** to start debugging

5. **Verify in Task Manager:**
   - Open Task Manager
   - Find "IIS Express" process
   - Note the PID (Process ID)

6. **Check with dd-dotnet:**
   ```cmd
   dd-dotnet check process <PID>
   ```

   You should see:
   ```
   Checks for process <PID>
   ✓ COR_ENABLE_PROFILING: 1
   ✓ COR_PROFILER: {846F5F1C-F9AE-4B07-969E-05C26BC060D8}
   ✓ Profiler attached: true
   ```

### Issue 2: Datadog .NET Tracer Not Installed

**Symptom:** Batch file shows error:
```
ERROR: Datadog .NET Tracer not found at: C:\Program Files\Datadog\.NET Tracer
```

**Solution:**

1. **Download the Datadog .NET Tracer:**
   - Go to: https://github.com/DataDog/dd-trace-dotnet/releases
   - Download the latest MSI installer: `datadog-dotnet-apm-<version>-x64.msi`

2. **Install the MSI:**
   - Run the installer
   - Accept default installation path: `C:\Program Files\Datadog\.NET Tracer`
   - Complete installation

3. **Verify installation:**
   ```cmd
   dir "C:\Program Files\Datadog\.NET Tracer"
   ```

   You should see directories:
   - `net461`
   - `netcoreapp3.1`
   - `win-x64`
   - `win-x86`

4. **Run the batch file again**

### Issue 3: `Tracer.DefaultServiceName: (null)`

**Symptom:** Tracer.Instance exists but DefaultServiceName is `(null)` and tracer settings can't be read.

**Cause:** The Datadog profiler DLL failed to attach to the process, so the tracer is not fully initialized.

**Solution:**

1. **Verify profiler DLLs exist:**
   ```cmd
   dir "C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll"
   dir "C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll"
   ```

2. **Check Windows Event Viewer for profiler errors:**
   - Open Event Viewer
   - Navigate to: Windows Logs → Application
   - Filter for Source: `.NET Runtime`
   - Look for errors related to profiler attachment

3. **Enable Datadog debug logging:**

   In `launch-vs-with-datadog.bat`, ensure:
   ```cmd
   SET DD_TRACE_DEBUG=true
   SET DD_TRACE_STARTUP_LOGS=true
   ```

4. **Check debug logs:**
   - Look in Visual Studio Output window → Debug
   - Look for Datadog profiler initialization messages

5. **Restart everything:**
   - Close Visual Studio
   - Run batch file again
   - Start debugging

### Issue 4: Profiler DLL Not Found

**Symptom:** Batch file shows:
```
WARNING: 64-bit profiler DLL not found at: C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll
```

**Cause:** Incomplete or corrupted Datadog installation.

**Solution:**

1. **Uninstall Datadog .NET Tracer:**
   - Control Panel → Programs → Uninstall a program
   - Find "Datadog .NET Tracer"
   - Uninstall

2. **Delete leftover files:**
   ```cmd
   rmdir /s /q "C:\Program Files\Datadog\.NET Tracer"
   ```

3. **Reinstall from latest MSI**

4. **Verify installation again**

### Issue 5: IIS Express Running as Wrong Architecture

**Symptom:** Environment variables are set, but no spans are created. `dd-dotnet check process` shows profiler not attached.

**Cause:** IIS Express is running as 32-bit, but you only have 64-bit profiler DLL set, or vice versa.

**Solution:**

1. **Check IIS Express architecture:**
   - Task Manager → Details tab
   - Find "iisexpress.exe"
   - Look at the "Platform" column
   - 32-bit processes show as "32-bit", 64-bit show nothing

2. **Ensure both architectures are set:**

   In `launch-vs-with-datadog.bat`:
   ```cmd
   REM Both 32-bit and 64-bit paths
   SET COR_PROFILER_PATH_32=C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll
   SET COR_PROFILER_PATH_64=C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll
   SET CORECLR_PROFILER_PATH_32=C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll
   SET CORECLR_PROFILER_PATH_64=C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll
   ```

3. **Restart Visual Studio via batch file**

### Issue 6: OWIN Mode Not Enabled

**Symptom:** No OWIN diagnostic output, Global.asax seems to be running instead.

**Cause:** `USE_OWIN` compilation symbol not set.

**Solution:**

1. **Check project properties:**
   - Right-click ApiFramework project → Properties
   - Build tab
   - Check "Conditional compilation symbols"
   - Should include `USE_OWIN`

2. **Add if missing:**
   - Example: `DEBUG;TRACE;USE_OWIN`

3. **Rebuild solution:**
   ```
   Build → Rebuild Solution
   ```

4. **Verify OWIN is active:**
   - Look for this in debug output:
   ```
   [OWIN] Startup configured - OWIN pipeline is active
   ```

## Verification Steps

After fixing issues, verify everything works:

### 1. Check Batch File Launch

```cmd
ApiFramework\launch-vs-with-datadog.bat
```

Expected output:
```
[OK] Environment variables set
[OK] Found Datadog .NET Tracer at: ...
[OK] Datadog profiler DLLs verified
[OK] Found Visual Studio at: ...
[OK] Visual Studio launch command executed successfully!
```

### 2. Check Debug Output (First Request)

In Visual Studio Output window → Debug, make a request and look for:

```
========================================
[Datadog Diagnostics] Tracer Status
========================================
[Datadog] Tracer.Instance: True
[Datadog] Tracer.DefaultServiceName: datadog-maui-api-framework  ✅ Should have a value
[Datadog] Settings.TraceEnabled: True
[Datadog] COR_ENABLE_PROFILING = 1  ✅ Should be 1
[Datadog OWIN] ✅ Captured span: aspnet.request  ✅ Should see span
```

### 3. Check dd-dotnet

```cmd
REM Get IIS Express PID from Task Manager
dd-dotnet check process <PID>
```

Expected output:
```
✓ COR_ENABLE_PROFILING: 1
✓ COR_PROFILER: {846F5F1C-F9AE-4B07-969E-05C26BC060D8}
✓ Profiler attached: true
✓ Tracer initialized: true
```

### 4. Check Datadog UI

1. Go to: https://app.datadoghq.com/apm/traces
2. Filter by: `service:datadog-maui-api-framework`
3. You should see traces appearing
4. Click on a trace to verify custom tags:
   - `custom.pipeline: owin`
   - `custom.span.type: aspnet.request.parent`

## Additional Debugging Commands

### Check if Profiler DLL is Loaded

```cmd
REM Get IIS Express PID
tasklist | findstr iisexpress

REM Check loaded DLLs
tasklist /m Datadog.Trace.ClrProfiler.Native.dll
```

If the DLL is loaded, you should see the IIS Express process listed.

### Enable Verbose Datadog Logging

Add to `launch-vs-with-datadog.bat`:

```cmd
SET DD_TRACE_DEBUG=true
SET DD_TRACE_STARTUP_LOGS=true
SET DD_TRACE_LOG_DIRECTORY=C:\Temp\DatadogLogs
```

Check logs in `C:\Temp\DatadogLogs\dotnet-tracer-*.log`

### Check Process Environment Variables Directly

```powershell
# PowerShell
$processId = <PID>
$process = Get-Process -Id $processId
$process.StartInfo.EnvironmentVariables | Format-Table
```

## Known Issues

### Visual Studio Caches Launch Settings

**Issue:** Even after setting environment variables, IIS Express doesn't get them.

**Solution:**
1. Delete `.vs` folder in solution directory
2. Close Visual Studio
3. Run batch file again

### Antivirus Blocks Profiler DLL

**Issue:** Profiler DLL exists but fails to attach due to antivirus.

**Solution:**
1. Add exclusion in antivirus for: `C:\Program Files\Datadog\.NET Tracer\`
2. Restart Visual Studio

### Multiple IIS Express Processes

**Issue:** Multiple IIS Express processes running, only one has correct environment variables.

**Solution:**
1. Task Manager → Details
2. Kill all `iisexpress.exe` processes
3. Restart debugging

## Getting Help

If issues persist after following this guide:

1. **Collect diagnostic information:**
   ```
   - Batch file output (full text)
   - Debug output showing diagnostic logs
   - Output of: dd-dotnet check process <PID>
   - Visual Studio version
   - Windows version
   ```

2. **Share diagnostic output with team**

3. **Check Datadog documentation:**
   - https://docs.datadoghq.com/tracing/troubleshooting/
   - https://docs.datadoghq.com/tracing/setup_overview/setup/dotnet-framework/

## Summary

The diagnostic output helps identify exactly where the Datadog APM setup is failing:

| Diagnostic Check | What It Tells You | Action If Failed |
|-----------------|-------------------|------------------|
| `Tracer.Instance: True` | Datadog library is loaded | Check if Datadog.Trace NuGet is installed |
| `Tracer.DefaultServiceName` | Profiler is attached and working | Verify environment variables, restart VS via batch file |
| `COR_ENABLE_PROFILING = 1` | Profiling is enabled for process | Launch VS with batch file |
| `COR_PROFILER` set | Correct profiler GUID configured | Check batch file has correct GUID |
| `COR_PROFILER_PATH_64` exists | Profiler DLL path is set | Verify Datadog installation |
| Active span found | Tracer is creating spans | All above checks should pass |

Work through issues from top to bottom - each diagnostic check builds on the previous one.
