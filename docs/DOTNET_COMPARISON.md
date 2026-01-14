# .NET Core vs .NET Framework Comparison

This document compares the two implementations of the Datadog MAUI API for customer demos.

## Project Overview

| Aspect | .NET Core 9.0 | .NET Framework 4.8 |
|--------|---------------|-------------------|
| **Location** | `Api/` | `ApiFramework/` |
| **Platform** | Cross-platform | Windows only |
| **API Style** | Minimal APIs | Web API Controllers |
| **Lines of Code** | ~400 | ~800 |
| **Complexity** | Lower | Higher |

## Architecture Comparison

### .NET Core 9.0 (Minimal APIs)

```csharp
// Program.cs - Single file with all endpoints
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", () =>
    Results.Ok(new { status = "healthy" }));

app.Run();
```

**Characteristics:**
- ✅ Concise and modern
- ✅ Less boilerplate
- ✅ Functional style
- ✅ Built-in DI

### .NET Framework 4.8 (Controllers)

```csharp
// HealthController.cs
public class HealthController : ApiController
{
    [HttpGet]
    [Route("health")]
    public IHttpActionResult GetHealth()
    {
        return Ok(new { status = "healthy" });
    }
}

// Global.asax.cs
public class WebApiApplication : HttpApplication
{
    protected void Application_Start()
    {
        GlobalConfiguration.Configure(WebApiConfig.Register);
    }
}

// WebApiConfig.cs
public static class WebApiConfig
{
    public static void Register(HttpConfiguration config)
    {
        config.MapHttpAttributeRoutes();
    }
}
```

**Characteristics:**
- ✅ More structured
- ✅ Familiar to enterprise developers
- ⚠️ More files and configuration
- ⚠️ No built-in DI

## Code Examples Side-by-Side

### Endpoint Definition

#### .NET Core
```csharp
app.MapPost("/data", (DataSubmission submission) =>
{
    dataStore.Add(submission);
    return Results.Ok(new { success = true });
});
```

#### .NET Framework
```csharp
[HttpPost]
[Route("data")]
public IHttpActionResult SubmitData([FromBody] DataSubmission submission)
{
    DataStore.Add(submission);
    return Ok(new { success = true });
}
```

### Datadog Manual Tracing

#### .NET Core
```csharp
var activeScope = Tracer.Instance.ActiveScope;
if (activeScope != null)
{
    activeScope.Span.SetTag("custom.user.id", userId);
}
```

#### .NET Framework
```csharp
var activeScope = Tracer.Instance.ActiveScope;
if (activeScope != null)
{
    activeScope.Span.SetTag("custom.user.id", userId);
}
```

**Note:** Manual tracing API is identical!

### Configuration

#### .NET Core
```json
// appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}

// Environment variables for Datadog
DD_API_KEY=your-key
DD_SERVICE=datadog-maui-api
```

#### .NET Framework
```xml
<!-- Web.config -->
<appSettings>
  <add key="DD_API_KEY" value="your-key" />
  <add key="DD_SERVICE" value="datadog-maui-api-framework" />
</appSettings>

<system.webServer>
  <modules>
    <add name="DatadogHttpModule"
         type="Datadog.Trace.AspNet.TracingHttpModule, Datadog.Trace.AspNet" />
  </modules>
</system.webServer>
```

## Datadog Integration Differences

### .NET Core

**Setup:**
1. Add NuGet package: `Datadog.Trace`
2. Set environment variables
3. Done! Automatic instrumentation works

**Configuration:**
```bash
# Docker or environment variables
DD_TRACE_ENABLED=true
DD_SERVICE=datadog-maui-api
DD_ENV=production
```

**Tracer Installation:**
- Linux: Installed via .deb package in Docker
- Windows: MSI installer or NuGet package

### .NET Framework

**Setup:**
1. Add NuGet packages: `Datadog.Trace` + `Datadog.Trace.AspNet`
2. Add HTTP Module to Web.config
3. Configure via Web.config or environment variables

**Configuration:**
```xml
<system.webServer>
  <modules>
    <add name="DatadogHttpModule"
         type="Datadog.Trace.AspNet.TracingHttpModule, Datadog.Trace.AspNet" />
  </modules>
</system.webServer>
```

**Tracer Installation:**
- MSI installer for Windows
- NuGet package includes necessary DLLs

## Performance Comparison

### Request Processing Time

| Metric | .NET Core 9.0 | .NET Framework 4.8 | Difference |
|--------|---------------|-------------------|------------|
| Cold start | ~500ms | ~1500ms | 3x faster |
| Warm requests | ~5ms | ~15ms | 3x faster |
| Memory usage | ~50MB | ~150MB | 3x less |
| Throughput | ~100k req/s | ~30k req/s | 3x higher |

*Based on typical ASP.NET benchmarks*

### Datadog Overhead

| Metric | .NET Core | .NET Framework | Notes |
|--------|-----------|----------------|-------|
| Tracing overhead | <5% | <5% | Similar |
| Memory for traces | ~10MB | ~15MB | Slightly higher |
| Startup time | +100ms | +200ms | Slightly higher |

## Deployment Comparison

### .NET Core

**Options:**
1. Docker (Linux or Windows containers)
2. Azure App Service (Linux or Windows)
3. Kubernetes
4. AWS ECS/EKS
5. Self-hosted on any OS

**Example (Docker):**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
COPY publish/ /app
ENTRYPOINT ["dotnet", "DatadogMauiApi.dll"]
```

**Example (Azure CLI):**
```bash
az webapp create --runtime "DOTNET|9.0"
```

### .NET Framework

**Options:**
1. IIS on Windows Server
2. Azure App Service (Windows only)
3. Windows containers (large ~4GB)

**Example (IIS):**
```powershell
# Build
msbuild /p:Configuration=Release

# Copy to IIS
Copy-Item bin\Release\* C:\inetpub\wwwroot\api\

# Create IIS site
New-WebAppPool -Name "DatadogMauiApi"
New-Website -Name "DatadogMauiApi" -PhysicalPath "C:\inetpub\wwwroot\api"
```

**Example (Azure):**
```bash
az webapp create --runtime "DOTNETFRAMEWORK|4.8"
```

## When to Use Each Version

### Use .NET Core 9.0 When:
- ✅ Building new applications
- ✅ Need cross-platform support
- ✅ Want best performance
- ✅ Prefer modern C# features
- ✅ Using Docker/Kubernetes
- ✅ Want minimal code

### Use .NET Framework 4.8 When:
- ✅ Customer has .NET Framework mandate
- ✅ Legacy system integration required
- ✅ Existing .NET Framework infrastructure
- ✅ Corporate policy requires it
- ✅ Team only knows Web API Controllers
- ✅ Windows-only environment

## Migration Path

### From .NET Framework to .NET Core

**Effort:** Medium to High

**Steps:**
1. Assess dependencies (some may not support .NET Core)
2. Convert controllers to minimal APIs (or keep controllers)
3. Update configuration (Web.config → appsettings.json)
4. Update NuGet packages
5. Test thoroughly

**Time:** 1-3 weeks for typical API

### From .NET Core to .NET Framework

**Effort:** Medium

**Steps:**
1. Convert minimal APIs to controllers
2. Add Global.asax and WebApiConfig
3. Update configuration (appsettings.json → Web.config)
4. Add HTTP Module for Datadog
5. Target .NET Framework 4.8

**Time:** 1-2 weeks for typical API

**Recommendation:** Don't do this unless absolutely necessary!

## Feature Parity Matrix

| Feature | .NET Core | .NET Framework | Notes |
|---------|-----------|----------------|-------|
| **API Endpoints** | ✅ | ✅ | Identical functionality |
| **Authentication** | ✅ | ✅ | Same JWT/Bearer approach |
| **Session Management** | ✅ | ✅ | Same implementation |
| **Datadog APM** | ✅ | ✅ | Full parity |
| **Custom Span Tags** | ✅ | ✅ | Identical API |
| **Distributed Tracing** | ✅ | ✅ | Both support |
| **RUM Integration** | ✅ | ✅ | Framework agnostic |
| **JSON Serialization** | System.Text.Json | Newtonsoft.Json | Different defaults |
| **CORS** | Built-in | Microsoft.AspNet.WebApi.Cors | Different packages |
| **Dependency Injection** | Built-in | Manual/Unity | Core has advantage |

## Customer Demo Talking Points

### For .NET Core Advocates

1. **"Show me the performance"**
   - Run load tests: 3x faster response times
   - Show Docker deployment simplicity
   - Demonstrate cross-platform capability

2. **"Prove it's production-ready"**
   - Show Microsoft's investment in .NET Core
   - Highlight major companies using it
   - Demonstrate Datadog's full support

3. **"What about our .NET Framework code?"**
   - Show side-by-side comparison
   - Explain migration path
   - Highlight that both can coexist

### For .NET Framework Requirements

1. **"We can only use .NET Framework"**
   - Show ApiFramework/ project
   - Demonstrate identical Datadog functionality
   - Prove it works with their existing infrastructure

2. **"Will this work with our IIS setup?"**
   - Show IIS deployment
   - Demonstrate HTTP Module configuration
   - Show Windows Server compatibility

3. **"What about our existing libraries?"**
   - Show package compatibility
   - Demonstrate NuGet package usage
   - Explain namespace differences

## Cost Comparison

### Infrastructure Costs

| Platform | .NET Core | .NET Framework | Savings |
|----------|-----------|----------------|---------|
| **Azure App Service** | B1: $13/mo | B1: $13/mo | Equal |
| **VM Hosting** | Any OS | Windows only | ~40% cheaper |
| **Container Costs** | ~100MB image | ~4GB image | 97% smaller |
| **Licensing** | Free | Windows license | $500-1000/server |

### Operational Costs

| Aspect | .NET Core | .NET Framework |
|--------|-----------|----------------|
| **Developer Time** | Faster (less code) | Slower (more boilerplate) |
| **Maintenance** | Lower (modern stack) | Higher (legacy patterns) |
| **Training** | Required (new concepts) | None (familiar) |
| **Support** | Active (latest .NET) | Maintenance mode | |

## Recommendation for Customers

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│  NEW PROJECTS          → Use .NET Core 9.0                  │
│                                                             │
│  EXISTING FRAMEWORK    → Evaluate migration                 │
│                          Show both versions                 │
│                                                             │
│  HARD REQUIREMENT      → Use .NET Framework 4.8             │
│                          Plan future migration              │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Demo Script

### 1. Start with .NET Core
- Show minimal APIs
- Deploy to Docker
- Show Datadog traces

### 2. Show .NET Framework
- Show controller structure
- Deploy to IIS/Azure Windows
- Show identical Datadog traces

### 3. Compare Traces in Datadog
- Both show same custom attributes
- Both have distributed tracing
- Both integrate with RUM

### 4. Discuss Migration
- Show code differences
- Explain migration effort
- Provide timeline estimates

## Conclusion

Both implementations provide:
- ✅ Full Datadog APM integration
- ✅ Custom span attributes
- ✅ Distributed tracing
- ✅ RUM correlation
- ✅ Production-ready code

Choose based on:
- **Technical requirements** (platform, performance)
- **Business constraints** (existing infrastructure, team skills)
- **Future direction** (modernization vs. stability)

For most new projects: **Use .NET Core 9.0**

For legacy requirements: **Use .NET Framework 4.8**

For best results: **Show both, let customer decide!**
