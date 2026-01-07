# Azure Functions - Quick Start Guide

## TL;DR - What You Need to Know

### Current Setup (Development)
- **Dockerfile**: `Dockerfile` (standard ASP.NET Core)
- **Runs on**: Docker, Container Apps, Kubernetes
- **Perfect for**: Local dev, testing, container-based deployments

### Production Setup (Azure Functions)
- **Dockerfile**: `Dockerfile.azurefunctions`
- **Runs on**: Azure Functions (Premium/Dedicated with custom containers)
- **Perfect for**: Serverless, auto-scaling, pay-per-use

## Key Differences in 30 Seconds

| What | Standard | Azure Functions |
|------|----------|-----------------|
| **Base Image** | `aspnet:9.0` | `azure-functions/dotnet-isolated:4` |
| **Work Dir** | `/app` | `/home/site/wwwroot` |
| **Entry** | `dotnet DatadogMauiApi.dll` | Functions host (built-in) |
| **Code Type** | Minimal API (`app.MapGet()`) | HTTP Triggers (`[HttpTrigger]`) |

## Can I Deploy the Current API to Azure Functions?

**No** - not directly. The current API uses ASP.NET Core Minimal APIs, which need conversion to Azure Functions.

### What Needs to Change?

1. **Project File**: `Microsoft.NET.Sdk.Web` → `Microsoft.NET.Sdk`
2. **NuGet Packages**: Add `Microsoft.Azure.Functions.Worker` packages
3. **Code**: Convert `MapGet()`/`MapPost()` to Functions with `[HttpTrigger]`
4. **Dockerfile**: Use `Dockerfile.azurefunctions`

**Estimate**: 2-4 hours of work for conversion + testing

## Deployment Options

### Option 1: Deploy Current API (No Code Changes)

Deploy standard container to:
- ✅ **Azure Container Apps** (recommended)
- ✅ **Azure App Service (Container)**
- ✅ **Azure Kubernetes Service**

```bash
# Build
docker build -f Dockerfile -t datadog-maui-api:latest .

# Push to Azure Container Registry
az acr login --name myregistry
docker tag datadog-maui-api:latest myregistry.azurecr.io/datadog-maui-api:latest
docker push myregistry.azurecr.io/datadog-maui-api:latest

# Deploy to Container Apps
az containerapp create \
  --name datadog-maui-api \
  --resource-group myResourceGroup \
  --image myregistry.azurecr.io/datadog-maui-api:latest \
  --target-port 8080 \
  --ingress external \
  --env-vars \
    DD_ENV=production \
    DD_SERVICE=datadog-maui-api \
    DD_AGENT_HOST=<datadog-agent-url> \
    DD_TRACE_AGENT_PORT=8126
```

**Cost**: ~$30-50/month (always-on) or ~$5-10/month (scale to zero)

### Option 2: Convert to Azure Functions (Code Changes Required)

See [`AZURE_FUNCTIONS_MIGRATION.md`](./AZURE_FUNCTIONS_MIGRATION.md) for full guide.

**Benefits**:
- Pay per execution (cheaper for low traffic)
- Auto-scaling (no configuration)
- Integrated with Azure ecosystem

**Cost**: ~$1-2/month (100k requests) or ~$145/month (Premium always-on)

## Datadog Configuration (Both Options)

### Required Environment Variables

```bash
# Datadog Agent
DD_AGENT_HOST=<your-agent-host-or-use-extension>
DD_TRACE_AGENT_PORT=8126
DD_SITE=datadoghq.com

# Service Tags
DD_ENV=production
DD_SERVICE=datadog-maui-api
DD_VERSION=1.0.0

# Tracing
DD_TRACE_ENABLED=true
DD_LOGS_INJECTION=true
DD_TRACE_PROPAGATION_STYLE=datadog,tracecontext
```

### Using Datadog Azure Extension (Recommended)

In Azure Portal:
1. Go to Function App or Container App
2. Extensions → Add → Search "Datadog"
3. Install Datadog APM extension
4. It automatically configures everything!

## Quick Decision Tree

```
Do you need Azure Functions features?
├─ Yes (Event Grid, Service Bus, Durable Functions)
│  └─ Convert to Functions → Use Dockerfile.azurefunctions
│
└─ No (just HTTP API)
   │
   ├─ Traffic is sporadic/unpredictable
   │  └─ Deploy to Container Apps (scale to zero)
   │     → Use current Dockerfile
   │
   └─ Traffic is consistent
      └─ Deploy to Container Apps (always-on)
         → Use current Dockerfile
```

## My Recommendation for Your Use Case

Based on your MAUI mobile app API:

### Phase 1: Now (Development & Testing)
✅ Use **current setup** with `Dockerfile`
- Works with docker-compose
- Easy local testing
- No code changes needed

### Phase 2: Production Deployment
✅ Deploy to **Azure Container Apps**
- Use current `Dockerfile` (no changes)
- Set min instances = 1 (avoid cold starts)
- Configure Datadog extension
- Cost: ~$30-40/month

**Why not Azure Functions?**
- Mobile APIs benefit from consistent latency (no cold starts)
- You're serving a web portal (static files) - Functions complicates this
- Current code works perfectly as-is
- Container Apps gives you Functions-like scaling without code changes

### Phase 3: Scale Up (If Needed)
If traffic grows significantly:
- Keep Container Apps, increase CPU/memory
- OR move to AKS for ultimate control
- Functions Conversion only if you need event-driven features

## Files Created

1. **`Dockerfile.azurefunctions`** - Azure Functions compatible Dockerfile
2. **`AZURE_FUNCTIONS_MIGRATION.md`** - Complete migration guide (detailed)
3. **`DOCKERFILE_COMPARISON.md`** - Side-by-side comparison
4. **`AZURE_QUICK_START.md`** - This file (quick reference)

## Next Steps

### To Deploy Current API (No Changes)
1. Create Azure Container Registry
2. Build and push current Dockerfile
3. Create Container App
4. Configure Datadog environment variables
5. Update mobile app to use Azure URL

### To Convert to Azure Functions (Optional)
1. Read [`AZURE_FUNCTIONS_MIGRATION.md`](./AZURE_FUNCTIONS_MIGRATION.md)
2. Create new branch for conversion
3. Update project file and code
4. Test locally with `func start`
5. Deploy to Azure Functions

## Questions?

- **Can I use both?** Yes! Standard container for dev/staging, Functions for production
- **Is Datadog the same?** Yes, identical APM and tracing capabilities
- **Which is cheaper?** Functions (Consumption) for low traffic, Container Apps for consistent traffic
- **Which is faster?** Container Apps (no cold starts), Functions Premium (if always-on)
- **Can I change my mind?** Yes, Docker images are portable

## Resources

- [Azure Container Apps Docs](https://learn.microsoft.com/azure/container-apps/)
- [Azure Functions Custom Containers](https://learn.microsoft.com/azure/azure-functions/functions-create-function-linux-custom-image)
- [Datadog Azure Integration](https://docs.datadoghq.com/integrations/azure/)
- [Datadog .NET APM](https://docs.datadoghq.com/tracing/trace_collection/dd_libraries/dotnet-core/)
