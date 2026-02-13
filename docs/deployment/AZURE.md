# Azure Deployment Guide

Complete guide for deploying the Datadog MAUI API to Azure using various hosting options.

## Quick Decision Guide

Choose your Azure deployment option:

| Option             | Best For                   | Cost       | Complexity | Code Changes |
| ------------------ | -------------------------- | ---------- | ---------- | ------------ |
| **Container Apps** | Modern apps, microservices | $5-50/mo   | Low        | None         |
| **App Service**    | Traditional web apps       | $30-100/mo | Low        | None         |
| **Functions**      | Serverless, event-driven   | $1-145/mo  | Medium     | Required     |
| **AKS**            | Enterprise, multi-service  | $100+/mo   | High       | None         |

**Recommended**: Start with **Azure Container Apps** for best balance of features, cost, and simplicity.

## Prerequisites

- **Azure Account**: Active subscription
- **Azure CLI**: [Install](https://docs.microsoft.com/cli/azure/install-azure-cli)
- **Datadog Account**: API key and credentials
- **.NET 9.0 SDK**: For local building

## Option 1: Azure Container Apps (Recommended)

Deploy the current API as-is with no code changes.

### Benefits

- Scale to zero (save money)
- Automatic HTTPS
- Built-in load balancing
- Managed containers

### Quick Deploy

```bash
# Login to Azure
az login

# Create resource group
az group create --name datadog-maui-rg --location eastus

# Create container registry
az acr create --resource-group datadog-maui-rg \
  --name datadogmauiacr --sku Basic

# Build and push image
az acr build --registry datadogmauiacr \
  --image datadog-maui-api:latest \
  --file Api/Dockerfile .

# Create container app environment
az containerapp env create \
  --name datadog-maui-env \
  --resource-group datadog-maui-rg \
  --location eastus

# Deploy container app
az containerapp create \
  --name datadog-maui-api \
  --resource-group datadog-maui-rg \
  --environment datadog-maui-env \
  --image datadogmauiacr.azurecr.io/datadog-maui-api:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 0 \
  --max-replicas 5 \
  --registry-server datadogmauiacr.azurecr.io \
  --env-vars \
    DD_API_KEY=secretref:dd-api-key \
    DD_SITE=datadoghq.com \
    DD_ENV=production \
    DD_SERVICE=datadog-maui-api \
    DD_VERSION=1.0.0 \
    DD_TRACE_ENABLED=true \
    DD_RUNTIME_METRICS_ENABLED=true
```

### Cost Estimate

- Dev/Test: ~$5-10/month (scale to zero)
- Production: ~$30-50/month (always available)

## Option 2: Azure App Service

Traditional app hosting with Windows or Linux support.

### Benefits

- Simple deployment model
- Easy configuration
- Built-in monitoring
- Familiar for IIS users

### Automated Deployment

Using the included script:

```bash
# Set environment variables
export DD_API_KEY="your-datadog-api-key"
export AZURE_RESOURCE_GROUP="datadog-maui-rg"
export AZURE_APP_NAME="datadog-maui-api"
export AZURE_LOCATION="eastus"

# Run deployment script
./scripts/deploy-azure.sh
```

### Manual Deployment

```bash
# Create App Service Plan
az appservice plan create \
  --name datadog-maui-plan \
  --resource-group datadog-maui-rg \
  --location eastus \
  --sku B1 \
  --is-linux false

# Create App Service
az webapp create \
  --name datadog-maui-api \
  --resource-group datadog-maui-rg \
  --plan datadog-maui-plan \
  --runtime "DOTNET|9.0"

# Configure Datadog settings
az webapp config appsettings set \
  --name datadog-maui-api \
  --resource-group datadog-maui-rg \
  --settings \
    DD_API_KEY="your-api-key" \
    DD_SITE="datadoghq.com" \
    DD_ENV="production" \
    DD_SERVICE="datadog-maui-api" \
    DD_VERSION="1.0.0" \
    DD_TRACE_ENABLED="true" \
    DD_RUNTIME_METRICS_ENABLED="true" \
    DD_LOGS_INJECTION="true" \
    DD_TRACE_SAMPLE_RATE="1.0"

# Build and deploy
cd Api
dotnet publish --configuration Release --output ./publish
cd publish && zip -r ../deploy.zip . && cd ..
az webapp deployment source config-zip \
  --name datadog-maui-api \
  --resource-group datadog-maui-rg \
  --src deploy.zip
```

### Cost Estimate

- Basic (B1): ~$13/month
- Standard (S1): ~$70/month

## Option 3: Azure Functions

Serverless deployment with per-execution billing. **Requires code changes**.

See [AZURE_FUNCTIONS.md](AZURE_FUNCTIONS.md) for full migration guide.

### Benefits

- Pay per execution
- Automatic scaling
- Integrated triggers
- Event-driven architecture

### When to Use

- Low-traffic APIs
- Event-driven workloads
- Cost-sensitive projects
- Batch processing

### Cost Estimate

- Consumption: ~$1-2/month (100k requests)
- Premium: ~$145/month (always-on)

## Datadog Configuration

### Required Environment Variables

All deployment options need these Datadog settings:

```bash
# Core Configuration
DD_API_KEY=<your-api-key>
DD_SITE=datadoghq.com
DD_ENV=production
DD_SERVICE=datadog-maui-api
DD_VERSION=1.0.0

# APM Tracing
DD_TRACE_ENABLED=true
DD_RUNTIME_METRICS_ENABLED=true
DD_LOGS_INJECTION=true
DD_TRACE_SAMPLE_RATE=1.0
DD_TRACE_PROPAGATION_STYLE=datadog,tracecontext

# RUM (Browser Monitoring)
DD_RUM_WEB_CLIENT_TOKEN=<your-rum-token>
DD_RUM_WEB_APPLICATION_ID=<your-app-id>
DD_RUM_WEB_SERVICE=<your-service-name>

```

### Agent Configuration

For Container Apps and AKS, you can:

1. Use Datadog's serverless monitoring (agentless)
2. Deploy sidecar agent
3. Use DaemonSet (AKS only)

For App Service:

- Use Datadog Extension (Windows)
- Use Docker agent sidecar (Linux)

## Post-Deployment

### Verify Deployment

```bash
# Get app URL
az containerapp show \
  --name datadog-maui-api \
  --resource-group datadog-maui-rg \
  --query properties.configuration.ingress.fqdn

# Test endpoints
curl https://your-app.azurecontainerapps.io/health
curl https://your-app.azurecontainerapps.io/config
```

### Monitor in Datadog

1. Open Datadog APM
2. Find service: `datadog-maui-api`
3. Verify traces appear
4. Check RUM for browser sessions

### Update Mobile App

Update the mobile app base URL to point to your Azure deployment:

```csharp
// In ApiService.cs
private const string BaseUrl = "https://your-app.azurecontainerapps.io";
```

## Troubleshooting

### No Traces in Datadog

- Verify `DD_API_KEY` is set correctly
- Check `DD_TRACE_ENABLED=true`
- Ensure app has internet access
- Verify `DD_SITE` matches your Datadog region

### App Won't Start

- Check application logs: `az containerapp logs show`
- Verify environment variables
- Check container health probes
- Review resource limits

### CORS Errors

- Configure CORS in app settings
- Add allowed origins
- Check OPTIONS preflight handling

## Related Guides

- [Azure Functions Migration](AZURE_FUNCTIONS.md) - Convert to serverless
- [Dockerfile Comparison](DOCKERFILE_COMPARISON.md) - Standard vs Functions
- [IIS Deployment](../backend/IIS_DEPLOYMENT.md) - Windows Server alternative

## Cost Optimization

1. **Container Apps**: Enable scale-to-zero for dev/test
2. **App Service**: Use Basic tier for low traffic
3. **Functions**: Use consumption plan for sporadic traffic
4. **All**: Use reserved instances for predictable workloads

## Next Steps

1. Set up staging environment
2. Configure custom domains
3. Enable SSL certificates
4. Set up CI/CD pipeline
5. Configure auto-scaling rules
