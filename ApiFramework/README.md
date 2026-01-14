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
- **Visual Studio 2019+** or **Rider**
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

### Web.config Settings

Edit `Web.config` to configure Datadog:

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

### Datadog HTTP Module

The `Web.config` includes the Datadog HTTP Module for automatic instrumentation:

```xml
<system.webServer>
  <modules>
    <add name="DatadogHttpModule"
         type="Datadog.Trace.AspNet.TracingHttpModule, Datadog.Trace.AspNet" />
  </modules>
</system.webServer>
```

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
