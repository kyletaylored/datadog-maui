# Dockerfile Comparison: Standard vs Azure Functions

## Overview

This document compares the two Dockerfiles for the Datadog MAUI API:

1. **`Dockerfile`** - Standard ASP.NET Core container (current)
2. **`Dockerfile.azurefunctions`** - Azure Functions compatible container (for production)

## Side-by-Side Comparison

### Base Images

| Aspect | Standard (`Dockerfile`) | Azure Functions (`Dockerfile.azurefunctions`) |
|--------|-------------------------|-----------------------------------------------|
| **Build Image** | `mcr.microsoft.com/dotnet/sdk:9.0` | `mcr.microsoft.com/dotnet/sdk:9.0` ✅ Same |
| **Runtime Image** | `mcr.microsoft.com/dotnet/aspnet:9.0` | `mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0` |
| **Runtime Type** | ASP.NET Core Kestrel | Azure Functions Runtime v4 + Isolated Worker |

### Working Directory

| Standard | Azure Functions |
|----------|-----------------|
| `/app` | `/home/site/wwwroot` |

**Why different?** Azure Functions expects code at `/home/site/wwwroot` to be compatible with the Functions runtime and Azure App Service conventions.

### Port Exposure

| Standard | Azure Functions |
|----------|-----------------|
| `EXPOSE 8080` | No explicit EXPOSE (managed by Functions runtime) |
| `ASPNETCORE_URLS=http://+:8080` | Functions runtime manages binding |

**Why different?** Azure Functions handles port binding internally, typically using port 80 (HTTP) and 443 (HTTPS) when deployed.

### Entry Point

| Standard | Azure Functions |
|----------|-----------------|
| `ENTRYPOINT ["dotnet", "DatadogMauiApi.dll"]` | No ENTRYPOINT (uses base image default) |

**Why different?** The Azure Functions base image has a built-in entrypoint that starts the Functions host, which discovers and runs your functions.

### Environment Variables

#### Standard Dockerfile
```dockerfile
ENV ASPNETCORE_URLS=http://+:8080
ENV DD_TRACE_SAMPLE_RATE=1.0
```

#### Azure Functions Dockerfile
```dockerfile
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true
ENV DD_TRACE_SAMPLE_RATE=1.0
```

**Additional Variables for Azure Functions:**
- `AzureWebJobsScriptRoot` - Tells Functions runtime where to find functions
- `AzureFunctionsJobHost__Logging__Console__IsEnabled` - Enables console logging

## What's the Same?

Both Dockerfiles share these identical elements:

✅ **Build Stage**
- Same build process using .NET SDK 9.0
- Same restore and publish commands
- Same multi-stage build pattern

✅ **Datadog APM Installation**
- Same Datadog tracer version (3.34.0)
- Same installation process using `.deb` package
- Same architecture detection (`${TARGETARCH}`)
- Same CLR profiler environment variables

✅ **RUM Injection**
- Same process for injecting RUM credentials into `wwwroot/index.html`
- Same security approach (credentials are public client tokens)

✅ **Git Metadata**
- Same build args for Git repository, commit SHA, and tag
- Same environment variables for Datadog Git integration

✅ **Docker Labels**
- Same OCI and Datadog labels for metadata
- Same tagging strategy

## Detailed Differences

### 1. Runtime Image Features

**Standard ASP.NET Core Image:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
```
- Minimal runtime for ASP.NET Core apps
- Kestrel web server
- ~200MB base image
- Optimized for containerized web apps

**Azure Functions Image:**
```dockerfile
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0
```
- Includes Azure Functions Runtime v4
- Isolated .NET 9.0 worker process model
- Functions host and job host
- Built-in integration with Azure services
- Application Insights SDK included
- ~400MB base image (includes Functions runtime)

### 2. Application Structure

**Standard:**
```
/app/
├── DatadogMauiApi.dll
├── appsettings.json
├── wwwroot/
│   └── index.html
└── ...
```

**Azure Functions:**
```
/home/site/wwwroot/
├── DatadogMauiApi.dll
├── host.json                  ← Functions config
├── local.settings.json        ← Local dev config
├── Functions/                 ← Functions directory
│   ├── HealthFunction.dll
│   └── ...
└── wwwroot/
    └── index.html
```

### 3. Configuration Files

**Standard requires:**
- `appsettings.json` (optional)
- Environment variables

**Azure Functions requires:**
- `host.json` (required) - Functions runtime configuration
- `local.settings.json` (local dev only) - Local environment variables
- Environment variables (Azure App Settings)

### 4. Logging Configuration

**Standard:**
- JSON console logging configured in `Program.cs`
- Full control over logging pipeline
- Logs go to stdout/stderr

**Azure Functions:**
- Logging configured via `host.json`
- Integrated with Application Insights
- Logs go to Azure Monitor, Application Insights, and stdout
- Built-in correlation with Functions invocations

### 5. Deployment Targets

**Standard Dockerfile can deploy to:**
- Docker / Podman
- Azure Container Instances
- Azure Container Apps
- Azure App Service (Container)
- Azure Kubernetes Service (AKS)
- Any Kubernetes cluster
- VM with Docker

**Azure Functions Dockerfile can deploy to:**
- Azure Functions (Premium Plan)
- Azure Functions (Dedicated Plan)
- Azure Container Apps (with Functions support)
- Local with Azure Functions Core Tools

## Cost Comparison

### Running Standard ASP.NET Core Container

**Azure Container Apps:**
- Always-on: ~$30-50/month (0.5 vCPU, 1GB RAM)
- Scale to zero: ~$5-10/month (with low traffic)

**Azure App Service (Container):**
- Basic B1: ~$13/month (1 vCPU, 1.75GB RAM)
- Standard S1: ~$70/month (1 vCPU, 1.75GB RAM, auto-scale)

### Running Azure Functions

**Consumption Plan:**
- First 1 million executions: FREE
- After: $0.20 per million executions
- $0.000016/GB-sec memory consumption
- Example: 100k requests/month with 512MB, 200ms avg = ~$1-2/month

**Premium Plan:**
- EP1 (1 vCPU, 3.5GB): ~$145/month
- Includes VNet integration, always-on, better performance
- No cold start

## Performance Comparison

| Metric | Standard Container | Azure Functions (Premium) | Azure Functions (Consumption) |
|--------|-------------------|---------------------------|-------------------------------|
| **Cold Start** | None (always-on) | None (always-on) | 1-3 seconds (first request) |
| **Latency** | ~50-100ms | ~50-100ms | ~100-200ms (warm) |
| **Throughput** | High (always-on) | Very High (scales instantly) | Medium (scales gradually) |
| **Scaling** | Manual/configured | Automatic | Automatic |
| **Max Instances** | Configured limit | Up to 100 | Up to 200 |

## Use Case Recommendations

### Use Standard Dockerfile (`Dockerfile`) When:

✅ You need full control over the web server and middleware
✅ You're serving static files or complex web content
✅ You want consistent, predictable latency (no cold starts)
✅ You need WebSockets, SignalR, or Server-Sent Events
✅ You're running in non-Azure environments (on-prem, other clouds)
✅ You have background services or long-running processes
✅ Development and testing (simpler local setup)

### Use Azure Functions Dockerfile (`Dockerfile.azurefunctions`) When:

✅ You want serverless, pay-per-use pricing
✅ Traffic is sporadic or unpredictable (scales to zero)
✅ You want automatic scaling without configuration
✅ You're integrating with Azure services (Event Grid, Service Bus, etc.)
✅ You want built-in Application Insights integration
✅ APIs are stateless with short execution times (<10 minutes)
✅ Production deployment to Azure

## Migration Path

### Phase 1: Development (Current)
Use **standard Dockerfile**:
- Faster local development
- Simpler debugging
- Full ASP.NET Core features

### Phase 2: Staging
Deploy **both versions**:
- Standard container to Azure Container Apps (staging)
- Azure Functions to staging Function App
- Compare performance, cost, and observability

### Phase 3: Production
Choose based on requirements:
- **Low traffic, cost-sensitive**: Azure Functions (Consumption)
- **High traffic, need performance**: Standard container on Container Apps
- **Hybrid**: Functions for API, Static Web Apps for web portal

## Datadog Observability

Both Dockerfiles provide **identical Datadog capabilities**:

✅ APM (Application Performance Monitoring) with automatic instrumentation
✅ Distributed tracing from mobile app to backend
✅ Log injection (correlate logs with traces)
✅ Runtime metrics (GC, thread pool, exceptions)
✅ Custom metrics via DogStatsD
✅ RUM-to-APM correlation

**Key difference**: Azure Functions adds Application Insights telemetry in addition to Datadog (can disable if desired).

## Build Commands

### Build Standard Container
```bash
docker build -f Dockerfile \
  --build-arg DD_GIT_TAG=v1.0.0 \
  --build-arg DD_GIT_COMMIT_SHA=$(git rev-parse HEAD) \
  --build-arg DD_RUM_WEB_CLIENT_TOKEN=$DD_RUM_WEB_CLIENT_TOKEN \
  --build-arg DD_RUM_WEB_APPLICATION_ID=$DD_RUM_WEB_APPLICATION_ID \
  -t datadog-maui-api:latest .
```

### Build Azure Functions Container
```bash
docker build -f Dockerfile.azurefunctions \
  --build-arg DD_GIT_TAG=v1.0.0 \
  --build-arg DD_GIT_COMMIT_SHA=$(git rev-parse HEAD) \
  --build-arg DD_RUM_WEB_CLIENT_TOKEN=$DD_RUM_WEB_CLIENT_TOKEN \
  --build-arg DD_RUM_WEB_APPLICATION_ID=$DD_RUM_WEB_APPLICATION_ID \
  -t datadog-maui-api:azure-functions .
```

## Testing

### Test Standard Container
```bash
# Run
docker run -p 5000:8080 \
  -e DD_ENV=local \
  -e DD_SERVICE=datadog-maui-api \
  datadog-maui-api:latest

# Test
curl http://localhost:5000/health
```

### Test Azure Functions Container
```bash
# Run
docker run -p 8080:80 \
  -e DD_ENV=local \
  -e DD_SERVICE=datadog-maui-api \
  -e AzureWebJobsScriptRoot=/home/site/wwwroot \
  datadog-maui-api:azure-functions

# Test (note: Functions might take 5-10 seconds to start)
curl http://localhost:8080/health
```

## Conclusion

**Current Dockerfile** is production-ready for:
- Azure Container Apps
- Azure App Service (Container)
- Kubernetes clusters
- Any Docker-based hosting

**Azure Functions Dockerfile** is production-ready for:
- Azure Functions (Premium/Dedicated plans with custom containers)
- Serverless deployments with cost optimization

**Recommendation**: Keep both Dockerfiles:
1. Use `Dockerfile` for development, staging, and non-Azure deployments
2. Use `Dockerfile.azurefunctions` for Azure production deployments where you want serverless benefits

Both provide **identical Datadog observability** and **identical application behavior** - the choice is about hosting model and cost optimization.
