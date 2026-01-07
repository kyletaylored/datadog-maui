# Azure Functions Migration Guide

This guide explains how to migrate the Datadog MAUI API from a standard ASP.NET Core container to Azure Functions.

## Current Architecture vs Azure Functions

### Current Setup (ASP.NET Core Minimal API)
- **Base Image**: `mcr.microsoft.com/dotnet/aspnet:9.0`
- **SDK**: `Microsoft.NET.Sdk.Web`
- **Entry Point**: ASP.NET Core Kestrel server
- **Routing**: Minimal API with `app.MapGet()`, `app.MapPost()`
- **Port**: Explicitly set to 8080
- **Dockerfile**: `Dockerfile` (standard)

### Azure Functions Setup
- **Base Image**: `mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0`
- **SDK**: `Microsoft.NET.Sdk` (not Web)
- **Entry Point**: Azure Functions runtime
- **Routing**: Functions with HTTP triggers and attributes
- **Port**: Managed by Functions runtime (default 80/443)
- **Dockerfile**: `Dockerfile.azurefunctions` (Functions-compatible)

## Key Differences

### 1. **Base Image**
```dockerfile
# Current (ASP.NET Core)
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Azure Functions
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0
```

The Azure Functions base image includes:
- Azure Functions runtime (v4)
- Functions host
- Isolated .NET 9.0 worker process
- Built-in Application Insights integration

### 2. **Working Directory**
```dockerfile
# Current
WORKDIR /app

# Azure Functions
WORKDIR /home/site/wwwroot
```

Azure Functions expects code in `/home/site/wwwroot` for compatibility with the Functions runtime.

### 3. **Environment Variables**
```dockerfile
# Current
ENV ASPNETCORE_URLS=http://+:8080

# Azure Functions
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
```

Azure Functions uses different environment variables for configuration.

### 4. **No ENTRYPOINT Override**
The Azure Functions base image has its own entrypoint that starts the Functions host. You should NOT override it.

## Code Changes Required

### Step 1: Convert Project File

**Current `DatadogMauiApi.csproj`:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.8" />
  </ItemGroup>
</Project>
```

**Azure Functions `DatadogMauiApi.csproj`:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <AzureFunctionsVersion>v4</AzureFunctionsVersion>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.23.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Http" Version="3.2.0" />
    <PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="1.17.4" />
  </ItemGroup>
  <ItemGroup>
    <None Update="host.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="local.settings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <CopyToPublishDirectory>Never</CopyToPublishDirectory>
    </None>
  </ItemGroup>
</Project>
```

### Step 2: Create `host.json`

```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      }
    },
    "logLevel": {
      "default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "extensions": {
    "http": {
      "routePrefix": ""
    }
  }
}
```

### Step 3: Convert Minimal API to Functions

**Current `Program.cs` (Minimal API):**
```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/health", (ILogger<Program> logger) =>
{
    logger.LogInformation("[Health Check] Service is healthy");
    return Results.Ok(new { status = "healthy" });
});

app.Run();
```

**Azure Functions Equivalent:**

Create `Functions/HealthFunction.cs`:
```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace DatadogMauiApi.Functions;

public class HealthFunction
{
    private readonly ILogger<HealthFunction> _logger;

    public HealthFunction(ILogger<HealthFunction> logger)
    {
        _logger = logger;
    }

    [Function("Health")]
    public HttpResponseData Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")]
        HttpRequestData req)
    {
        _logger.LogInformation("[Health Check] Service is healthy");

        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString("{\"status\":\"healthy\",\"timestamp\":\"" +
            DateTime.UtcNow.ToString("o") + "\"}");

        return response;
    }
}
```

### Step 4: Create Functions Program.cs

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Register your services here
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .ConfigureLogging(logging =>
    {
        logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });
    })
    .Build();

host.Run();
```

## Azure Function HTTP Triggers - Complete Examples

### GET Endpoint Example

```csharp
[Function("GetConfig")]
public async Task<HttpResponseData> GetConfig(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "config")]
    HttpRequestData req,
    FunctionContext context)
{
    var logger = context.GetLogger("GetConfig");

    // Extract headers
    var correlationId = req.Headers.TryGetValues("X-Correlation-ID", out var values)
        ? values.FirstOrDefault()
        : null;

    logger.LogInformation("[Config] Configuration requested with CorrelationId: {CorrelationId}",
        correlationId);

    var config = new ConfigResponse(
        WebViewUrl: "https://your-azure-function.azurewebsites.net",
        FeatureFlags: new Dictionary<string, bool>
        {
            { "EnableTelemetry", true },
            { "EnableAdvancedFeatures", false }
        }
    );

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(config);

    return response;
}
```

### POST Endpoint Example

```csharp
[Function("SubmitData")]
public async Task<HttpResponseData> SubmitData(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "data")]
    HttpRequestData req,
    FunctionContext context)
{
    var logger = context.GetLogger("SubmitData");

    // Read request body
    var submission = await req.ReadFromJsonAsync<DataSubmission>();

    if (submission == null)
    {
        var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        await badResponse.WriteStringAsync("Invalid request body");
        return badResponse;
    }

    logger.LogInformation(
        "[Data Submission] CorrelationId: {CorrelationId}, SessionName: {SessionName}",
        submission.CorrelationId,
        submission.SessionName);

    // Process the submission...

    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(new
    {
        isSuccessful = true,
        message = "Data received successfully",
        correlationId = submission.CorrelationId,
        timestamp = DateTime.UtcNow
    });

    return response;
}
```

## Datadog Configuration for Azure Functions

### Azure App Settings (Environment Variables)

In Azure Portal → Function App → Configuration → Application Settings:

```bash
# Datadog Agent connection (use Datadog Extension or external agent)
DD_AGENT_HOST=<your-datadog-agent-host>
DD_TRACE_AGENT_PORT=8126
DD_SITE=datadoghq.com

# Datadog service tagging
DD_ENV=production
DD_SERVICE=datadog-maui-api
DD_VERSION=1.0.0

# Datadog tracing configuration
DD_TRACE_ENABLED=true
DD_RUNTIME_METRICS_ENABLED=true
DD_LOGS_INJECTION=true

# Distributed tracing configuration
DD_TRACE_PROPAGATION_STYLE=datadog,tracecontext
DD_TRACE_PROPAGATION_EXTRACT_FIRST=true

# CLR Profiler settings (for Datadog APM)
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
DD_DOTNET_TRACER_HOME=/opt/datadog
DD_INTEGRATIONS=/opt/datadog/integrations.json
```

### Using Datadog Azure Functions Extension

Azure has a Datadog extension that simplifies setup:

1. Install the Datadog extension in Azure Portal:
   - Go to Function App → Extensions → Add
   - Search for "Datadog"
   - Install the Datadog APM extension

2. The extension automatically configures:
   - APM instrumentation
   - Log collection
   - Metrics collection
   - Connection to Datadog Agent

## Deployment Options

### Option 1: Deploy Docker Container to Azure Container Apps

```bash
# Build the image
docker build -f Dockerfile.azurefunctions -t datadog-maui-api:azure .

# Tag for Azure Container Registry
docker tag datadog-maui-api:azure myregistry.azurecr.io/datadog-maui-api:latest

# Push to ACR
docker push myregistry.azurecr.io/datadog-maui-api:latest

# Deploy to Azure Container Apps
az containerapp create \
  --name datadog-maui-api \
  --resource-group myResourceGroup \
  --image myregistry.azurecr.io/datadog-maui-api:latest \
  --environment myEnvironment \
  --ingress external \
  --target-port 80
```

### Option 2: Deploy to Azure Functions (Consumption Plan)

```bash
# Create Function App
az functionapp create \
  --name datadog-maui-api \
  --resource-group myResourceGroup \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --functions-version 4 \
  --os-type Linux

# Deploy using Azure Functions Core Tools
func azure functionapp publish datadog-maui-api
```

### Option 3: Deploy Custom Container to Azure Functions (Premium/Dedicated)

```bash
# Create Function App with custom container
az functionapp create \
  --name datadog-maui-api \
  --resource-group myResourceGroup \
  --plan myPremiumPlan \
  --deployment-container-image-name myregistry.azurecr.io/datadog-maui-api:latest \
  --functions-version 4

# Configure container settings
az functionapp config container set \
  --name datadog-maui-api \
  --resource-group myResourceGroup \
  --docker-custom-image-name myregistry.azurecr.io/datadog-maui-api:latest \
  --docker-registry-server-url https://myregistry.azurecr.io
```

## Testing Azure Functions Locally

1. Install Azure Functions Core Tools:
```bash
# macOS
brew tap azure/functions
brew install azure-functions-core-tools@4

# Windows
choco install azure-functions-core-tools-4
```

2. Run locally:
```bash
cd Api
func start
```

3. Test endpoints:
```bash
# Health check
curl http://localhost:7071/health

# Submit data
curl -X POST http://localhost:7071/data \
  -H "Content-Type: application/json" \
  -d '{"correlationId":"test","sessionName":"Test","notes":"Testing","numericValue":42}'
```

## Migration Checklist

- [ ] Convert `.csproj` to use `Microsoft.NET.Sdk` (not `.Web`)
- [ ] Add Azure Functions NuGet packages
- [ ] Create `host.json` configuration file
- [ ] Convert all `MapGet`/`MapPost` endpoints to Functions with `[HttpTrigger]`
- [ ] Update `Program.cs` to use Functions hosting model
- [ ] Test locally with Azure Functions Core Tools
- [ ] Update `Dockerfile` to use Azure Functions base image
- [ ] Configure Azure App Settings with Datadog environment variables
- [ ] Deploy to Azure (choose deployment method)
- [ ] Verify Datadog traces are appearing in APM
- [ ] Update mobile app base URL to Azure Functions URL
- [ ] Test distributed tracing from mobile → Azure Functions

## Comparison: ASP.NET Core vs Azure Functions

| Feature | ASP.NET Core | Azure Functions |
|---------|--------------|-----------------|
| **Hosting Model** | Always-on Kestrel server | Serverless/consumption or premium |
| **Cold Start** | None (always running) | Yes (consumption plan) |
| **Scaling** | Manual/VM scale sets | Automatic, per-function |
| **Cost** | Pay for VM/container | Pay per execution + memory |
| **Development** | Standard ASP.NET Core | Azure Functions SDK |
| **Local Testing** | `dotnet run` | `func start` |
| **Routing** | Minimal API / Controllers | HTTP Triggers with routes |
| **Middleware** | Full middleware pipeline | Limited (Functions middleware) |
| **Static Files** | Built-in support | Requires workaround |

## Recommendations

### When to Use ASP.NET Core Container
- Complex middleware requirements
- Serving static files (web portal)
- Need for WebSockets or SignalR
- Consistent latency required (no cold starts)
- Heavy background processing

### When to Use Azure Functions
- Event-driven workloads
- Sporadic traffic patterns
- Cost optimization (pay per use)
- Automatic scaling required
- Integration with Azure services (Event Grid, Service Bus, etc.)

## Hybrid Approach

For your scenario, consider a **hybrid approach**:

1. **Keep the current ASP.NET Core API** for development/staging
2. **Deploy Azure Functions** for production with these endpoints:
   - `/health` - Health check (lightweight)
   - `/config` - Configuration retrieval (lightweight)
   - `/data` - Data submission (event-driven)
3. **Serve static web portal** from Azure Static Web Apps or Azure Storage (separate from API)

This gives you:
- ✅ Cost efficiency (Functions scale to zero)
- ✅ Automatic scaling for spiky mobile traffic
- ✅ Keep current development workflow
- ✅ Separate static content hosting

## Next Steps

1. Review this guide and decide on deployment strategy
2. If proceeding with Azure Functions:
   - Create a new branch for Functions conversion
   - Follow the migration checklist
   - Test locally with `func start`
   - Deploy to Azure staging environment
3. Update CI/CD pipeline to support both container types
4. Monitor Datadog traces to ensure distributed tracing works across Azure Functions
