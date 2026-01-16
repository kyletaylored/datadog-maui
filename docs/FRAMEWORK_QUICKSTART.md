# .NET Framework 4.8 Quick Start

Quick reference for the .NET Framework 4.8 implementation.

## Project Location

```
ApiFramework/  (NEW - .NET Framework 4.8)
Api/           (Existing - .NET Core 9.0)
```

## Building (Windows Required)

### Using Visual Studio
```
1. Open ApiFramework/DatadogMauiApi.Framework.csproj
2. Restore NuGet Packages
3. Build → Build Solution (Ctrl+Shift+B)
4. Run → Start Debugging (F5)
```

### Using Command Line
```powershell
cd ApiFramework
nuget restore
msbuild /p:Configuration=Release
```

## Running Locally

**IIS Express** (automatic with Visual Studio):
- URL: `http://localhost:50000`

**IIS** (manual setup):
```powershell
# Create App Pool
New-WebAppPool -Name "DatadogMauiFramework"

# Create Website
New-Website -Name "DatadogMauiFramework" `
  -PhysicalPath "C:\path\to\ApiFramework\bin\Release" `
  -ApplicationPool "DatadogMauiFramework"
```

## Configuration

### Datadog RUM & APM

Configuration is split between:
- **RUM (Web Dashboard)**: Auto-generated from `.env` during build
- **APM (Backend Tracing)**: Configured in `Web.config`

See [ApiFramework/README.md](ApiFramework/README.md#configuration) for full details.

## Testing

```powershell
# Health check
Invoke-RestMethod http://localhost:50000/health

# Login
$body = '{"username":"demo","password":"password"}'
$response = Invoke-RestMethod http://localhost:50000/auth/login `
  -Method POST -Body $body -ContentType "application/json"

echo $response.token
```

## Key Files

| File | Purpose |
|------|---------|
| `Controllers/*.cs` | API endpoints |
| `Models/*.cs` | Data models |
| `Services/SessionManager.cs` | Business logic |
| `Global.asax.cs` | Application startup |
| `App_Start/WebApiConfig.cs` | Routing config |
| `Web.config` | Configuration + Datadog |

## Datadog Configuration

Automatic instrumentation is configured via CLR Profiler environment variables in `Web.config`:
```xml
<appSettings>
  <add key="DD_API_KEY" value="your-api-key" />
  <add key="DD_ENV" value="production" />
  <add key="DD_SERVICE" value="datadog-maui-api-framework" />
  <add key="DD_TRACE_ENABLED" value="true" />
</appSettings>
```

**Note**: Datadog.Trace 3.35.0+ uses CLR Profiler (no HTTP Module needed)

## Deployment to Azure

```bash
# Create Windows App Service
az webapp create \
  --name my-api-framework \
  --resource-group my-rg \
  --plan my-plan \
  --runtime "DOTNETFRAMEWORK|4.8"

# Configure Datadog
az webapp config appsettings set \
  --name my-api-framework \
  --resource-group my-rg \
  --settings DD_API_KEY="your-key" DD_ENV="production"

# Deploy (use Visual Studio Publish or ZIP deploy)
```

## Comparison with .NET Core

✅ **Same functionality** - all endpoints identical
✅ **Same Datadog features** - full APM support
⚠️ **Different code structure** - controllers vs minimal APIs
⚠️ **Windows only** - requires IIS or Azure Windows

## When to Use

Use this .NET Framework version when:
- Customer requires .NET Framework 4.8
- Existing .NET Framework infrastructure
- Windows-only environment
- Team prefers Web API Controllers

Otherwise, use the .NET Core version in `Api/`

## Resources

- [Full README](ApiFramework/README.md)
- [Comparison Guide](docs/DOTNET_COMPARISON.md)
- [Deployment Guide](docs/AZURE_DEPLOYMENT.md)
