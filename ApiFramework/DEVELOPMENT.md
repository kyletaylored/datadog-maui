# Development Guide

Quick reference for developing the ApiFramework project with Datadog APM.

## Running with Datadog APM (IIS Express)

**Important:** Always launch Visual Studio using the provided batch file to enable Datadog tracing:

```powershell
.\launch-vs-with-datadog.bat
```

**Do NOT** open the solution by double-clicking `DatadogMauiApi.Framework.sln` - this will launch Visual Studio without Datadog environment variables.

### Why?

.NET Framework projects with IIS Express don't reliably inherit environment variables from project configuration files. The batch file:
- Auto-detects your Visual Studio installation (any version/edition)
- Sets all required Datadog environment variables
- Launches Visual Studio with these variables
- IIS Express inherits the variables when launched from VS

### Verify It's Working

After pressing F5 in Visual Studio:

1. Open Task Manager and find the `iisexpress.exe` process
2. Note its Process ID (PID)
3. Run:
   ```powershell
   dd-dotnet check process <PID>
   ```

You should see:
```
[SUCCESS]: The environment variable COR_ENABLE_PROFILING is set to the correct value of 1
[SUCCESS]: The tracer version X.X.X is loaded into the process
```

### Customizing Datadog Tags

Edit `launch-vs-with-datadog.bat` to change:
- `DD_SERVICE` - Service name in Datadog APM
- `DD_ENV` - Environment tag (local, dev, staging, prod)
- `DD_VERSION` - Application version

## Running WITHOUT Datadog APM

If you need to run without Datadog tracing, just open Visual Studio normally (double-click the .sln file).

## More Information

See [../docs/backend/IIS_EXPRESS_DATADOG_SETUP.md](../docs/backend/IIS_EXPRESS_DATADOG_SETUP.md) for detailed setup instructions and troubleshooting.
