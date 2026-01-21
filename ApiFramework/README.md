# Datadog MAUI API - .NET Framework 4.8

This is the .NET Framework 4.8 version of the Datadog MAUI API, designed for customers using traditional ASP.NET Web API on Windows.

## Key Differences from .NET Core Version

| Feature | .NET Core 9.0 (`Api/`) | .NET Framework 4.8 (`ApiFramework/`) |
|---------|------------------------|-------------------------------------|
| **API Style** | Minimal APIs | Web API Controllers |
| **Hosting** | Kestrel (cross-platform) | IIS (Windows only) |
| **Configuration** | appsettings.json | Web.config |
| **Dependency Injection** | Built-in | Manual (no DI container) |
| **Datadog Module** | Automatic via environment | HTTP Module in Web.config |
| **Deployment** | Docker, Azure App Service (Linux/Windows) | IIS, Azure App Service (Windows only) |

## Project Structure

```
ApiFramework/
├── Controllers/          # Web API 2 Controllers
│   ├── AuthController.cs
│   ├── ConfigController.cs
│   ├── DataController.cs
│   ├── HealthController.cs
│   └── ProfileController.cs
├── Models/              # DTOs and data models
│   ├── ConfigResponse.cs
│   ├── DataSubmission.cs
│   ├── LoginRequest.cs
│   ├── LoginResponse.cs
│   └── UserProfile.cs
├── Services/            # Business logic
│   └── SessionManager.cs
├── App_Start/           # Configuration
│   └── WebApiConfig.cs
├── Properties/
│   └── AssemblyInfo.cs
├── Global.asax         # Application entry point
├── Web.config          # Configuration + Datadog setup
└── packages.config     # NuGet dependencies
```

## Prerequisites

- **Windows OS** (required for .NET Framework)
- **Visual Studio 2022+** or **Rider**
- **.NET Framework 4.8 SDK**
- **IIS Express** or **IIS** for hosting

## Building the Project

### Using Visual Studio

1. Open `DatadogMauiApi.Framework.csproj` in Visual Studio
2. Restore NuGet packages (right-click solution → Restore NuGet Packages)
3. Build → Build Solution (Ctrl+Shift+B)
4. Run → Start Debugging (F5)

### Using MSBuild (Command Line)

```powershell
# Restore NuGet packages
nuget restore DatadogMauiApi.Framework.csproj

# Build the project
msbuild DatadogMauiApi.Framework.csproj /p:Configuration=Release

# Output will be in bin/Release/
```

## Configuration

### Datadog RUM (Web Dashboard)

RUM configuration is automatically generated from the root `.env` file during build:

1. Edit `.env` in the project root:
   ```bash
   DD_RUM_WEB_CLIENT_TOKEN=pub37e71f97f5364780bccc640fd9bcf94a
   DD_RUM_WEB_APPLICATION_ID=6af6b082-8fc8-4bba-a5e1-587342360624
   DD_SITE=datadoghq.com
   DD_ENV=local
   ```

2. Build the project in Visual Studio - `rum-config.js` is generated automatically
3. The dashboard at `http://localhost:50000` will use these credentials

**How it works:**
- MSBuild pre-build event (line 126-129 in `.csproj`) runs `generate-rum-config.ps1`
- PowerShell script reads `../.env` and generates `rum-config.js`
- `index.html` loads this config file for RUM initialization
- Config file is gitignored (regenerated on each build)

### Web.config Settings (APM)

Edit `Web.config` to configure Datadog backend tracing:

```xml
<appSettings>
  <add key="DD_API_KEY" value="your-datadog-api-key" />
  <add key="DD_SITE" value="datadoghq.com" />
  <add key="DD_ENV" value="production" />
  <add key="DD_SERVICE" value="datadog-maui-api-framework" />
  <add key="DD_VERSION" value="1.0.0" />
  <add key="DD_TRACE_ENABLED" value="true" />
</appSettings>
```

### Datadog Automatic Instrumentation

**IIS Express (Visual Studio):**

**Quick Setup:**

1. **Install Datadog .NET Tracer MSI** (if not already installed):
   - Download from https://github.com/DataDog/dd-trace-dotnet/releases/latest
   - Run `datadog-dotnet-apm-{version}-x64.msi`

2. **Launch Visual Studio with Datadog environment:**
   ```powershell
   .\ApiFramework\launch-vs-with-datadog.bat
   ```
   This automatically detects your Visual Studio installation and launches it with all Datadog environment variables pre-configured.

3. **Press F5 to debug** - Datadog APM will work automatically!

4. **Verify configuration:**
   ```powershell
   # After running the app (F5), get the process ID and check:
   dd-dotnet check process <process-id>
   ```

**How it works:** The batch file uses `vswhere.exe` to auto-detect your Visual Studio installation (works with VS 2019, 2022, 2026, any edition), sets Datadog environment variables, and launches Visual Studio. Any IIS Express process launched from VS inherits these variables.

**Note:** Due to limitations in .NET Framework project debugging, environment variables must be set before launching Visual Studio. Always use the batch file instead of opening the solution directly.

For troubleshooting, see [../docs/backend/IIS_EXPRESS_DATADOG_SETUP.md](../docs/backend/IIS_EXPRESS_DATADOG_SETUP.md)

**Full IIS (Production):**

For production IIS, set environment variables on the Application Pool:
```powershell
# In IIS Manager → Application Pools → [YourAppPool] → Advanced Settings → Environment Variables
COR_ENABLE_PROFILING=1
COR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
```

Or use the Datadog MSI installer which configures IIS globally.

### OWIN vs Global.asax Pipeline

This project supports **both** traditional ASP.NET pipeline (Global.asax) and OWIN middleware pipeline for testing different customer environments.

**Default**: Uses Global.asax (traditional ASP.NET pipeline)

**To enable OWIN mode** (for replicating OWIN-based customer issues):
1. Add `USE_OWIN` to **Project Properties → Build → Conditional compilation symbols**
2. Rebuild the project
3. OWIN middleware pipeline will handle requests instead

See [OWIN_SETUP.md](OWIN_SETUP.md) for detailed configuration and troubleshooting.

## Endpoints

All endpoints match the .NET Core version:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Health check |
| GET | `/config` | Configuration |
| POST | `/auth/login` | User login |
| POST | `/auth/logout` | User logout |
| GET | `/profile` | Get user profile (requires auth) |
| PUT | `/profile` | Update user profile (requires auth) |
| POST | `/data` | Submit data |
| GET | `/data` | Get all data |

## Running Locally

### IIS Express

1. Open project in Visual Studio
2. Press F5 to run
3. API will be available at: `http://localhost:50000`

### IIS

1. Build the project in Release mode
2. Copy `bin/Release/` contents to IIS website folder
3. Create IIS Application Pool (.NET Framework v4.0)
4. Create IIS Website pointing to your folder
5. Set App Pool to your application pool
6. API will be available at your IIS site URL

**If you encounter permissions errors (500.19):**
Run the troubleshooting script from the project root:
```powershell
.\scripts\troubleshoot-iis.ps1 -FixAll
```

This will automatically fix common IIS issues including permissions, physical paths, and app pool configuration.

## Deployment to Azure App Service

### Using Azure Portal

1. **Create App Service**:
   - Runtime: `.NET Framework 4.8`
   - OS: Windows

2. **Configure Application Settings**:
   ```
   DD_API_KEY = your-api-key
   DD_SITE = datadoghq.com
   DD_ENV = production
   DD_SERVICE = datadog-maui-api-framework
   DD_TRACE_ENABLED = true
   ```

3. **Deploy**:
   - Use Visual Studio "Publish to Azure"
   - Or use FTP deployment
   - Or use GitHub Actions

### Using Visual Studio

1. Right-click project → Publish
2. Target: Azure App Service (Windows)
3. Select your App Service
4. Publish

## Datadog Integration

### Automatic Instrumentation

The Datadog HTTP Module (`Datadog.Trace.AspNet`) automatically:
- ✅ Captures HTTP requests and responses
- ✅ Creates spans for each request
- ✅ Adds distributed tracing headers
- ✅ Captures exceptions and errors

### Manual Instrumentation

Controllers include manual span tagging for custom attributes:

```csharp
var activeScope = Tracer.Instance.ActiveScope;
if (activeScope != null)
{
    activeScope.Span.ResourceName = "GET /health";
    activeScope.Span.SetTag("custom.operation.type", "health_check");
    activeScope.Span.SetTag("custom.user.id", userId);
}
```

### Custom Span Attributes

All custom attributes are prefixed with `custom.` for easy filtering:
- `custom.user.id`
- `custom.authenticated`
- `custom.operation.type`
- `custom.correlation.id`
- `custom.data.*`

## Testing

```powershell
# Health check
Invoke-RestMethod -Uri http://localhost:50000/health

# Login
$loginBody = @{
    username = "demo"
    password = "password"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri http://localhost:50000/auth/login `
    -Method POST -Body $loginBody -ContentType "application/json"

$token = $response.token

# Submit data (authenticated)
$dataBody = @{
    correlationId = "test-$(Get-Date -Format 'yyyyMMddHHmmss')"
    sessionName = "Test Session"
    notes = "Framework test"
    numericValue = 42.5
} | ConvertTo-Json

Invoke-RestMethod -Uri http://localhost:50000/data `
    -Method POST -Body $dataBody -ContentType "application/json" `
    -Headers @{ Authorization = "Bearer $token" }
```

## Troubleshooting

### Module not found errors

Install Datadog NuGet packages:
```powershell
Install-Package Datadog.Trace
Install-Package Datadog.Trace.AspNet
```

### CORS issues

CORS is configured in `WebApiConfig.cs`. Modify as needed:
```csharp
var cors = new EnableCorsAttribute("https://yourdomain.com", "*", "*");
config.EnableCors(cors);
```

### Traces not appearing

1. Verify `DD_API_KEY` is set in Web.config
2. Check that HTTP Module is loaded (Event Viewer → Application logs)
3. Ensure `DD_TRACE_ENABLED=true`
4. Check Datadog logs in `C:\ProgramData\Datadog .NET Tracer\logs\`

### IIS Permissions or Configuration Issues

Run the comprehensive troubleshooting script:
```powershell
.\scripts\troubleshoot-iis.ps1 -FixAll
```

This will check and fix:
- Physical path configuration
- File permissions (IIS_IUSRS, app pool identity)
- Missing files
- Application pool state
- Website state

## Comparison with .NET Core

### Advantages of .NET Framework 4.8
- ✅ Familiar to enterprise .NET developers
- ✅ Compatible with legacy .NET Framework libraries
- ✅ Established in many enterprises
- ✅ Full Windows integration

### Advantages of .NET Core 9.0
- ✅ Cross-platform (Linux, macOS, Windows)
- ✅ Better performance (2-3x faster)
- ✅ Modern C# features
- ✅ Minimal APIs (less code)
- ✅ Built-in dependency injection
- ✅ Easier Docker deployment

## Resources

- [ASP.NET Web API Documentation](https://docs.microsoft.com/en-us/aspnet/web-api/)
- [Datadog .NET Framework Tracing](https://docs.datadoghq.com/tracing/setup_overview/setup/dotnet-framework/)
- [.NET Framework 4.8 on Azure](https://docs.microsoft.com/en-us/azure/app-service/configure-language-dotnet-framework)

## Support

For issues specific to this .NET Framework implementation, please check:
- Project documentation in `docs/`
- Compare with .NET Core version in `Api/`
- Azure deployment guide in `docs/AZURE_DEPLOYMENT.md`
