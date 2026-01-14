# Azure App Service Deployment Guide

This guide explains how to deploy the Datadog MAUI API to Azure App Service on Windows.

## Prerequisites

1. **Azure Account**: Active Azure subscription
2. **Azure CLI**: Install from [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
3. **Datadog Account**: API key and RUM credentials
4. **.NET 9.0 SDK**: For local building

## Deployment Options

### Option 1: Automated Deployment with Script (Recommended)

The easiest way to deploy is using our automated deployment script:

```bash
# Set your Datadog credentials
export DD_API_KEY="your-datadog-api-key"
export DD_RUM_WEB_CLIENT_TOKEN="your-rum-client-token"
export DD_RUM_WEB_APPLICATION_ID="your-rum-application-id"

# Optional: Customize deployment settings
export AZURE_RESOURCE_GROUP="datadog-maui-rg"
export AZURE_APP_NAME="datadog-maui-api"
export AZURE_LOCATION="eastus"
export AZURE_SKU="B1"  # Basic tier

# Run the deployment script
./scripts/deploy-azure.sh
```

The script will:
- Create Azure resources (resource group, app service plan, app service)
- Configure Datadog environment variables
- Build and publish the .NET API
- Deploy to Azure App Service
- Configure CORS and other settings

### Option 2: Manual Deployment via Azure CLI

1. **Login to Azure**:
   ```bash
   az login
   ```

2. **Create Resource Group**:
   ```bash
   az group create \
     --name datadog-maui-rg \
     --location eastus
   ```

3. **Create App Service Plan** (Windows):
   ```bash
   az appservice plan create \
     --name datadog-maui-plan \
     --resource-group datadog-maui-rg \
     --location eastus \
     --sku B1 \
     --is-linux false
   ```

4. **Create App Service**:
   ```bash
   az webapp create \
     --name datadog-maui-api \
     --resource-group datadog-maui-rg \
     --plan datadog-maui-plan \
     --runtime "DOTNET|9.0"
   ```

5. **Configure App Settings**:
   ```bash
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
       DD_TRACE_SAMPLE_RATE="1.0" \
       ASPNETCORE_ENVIRONMENT="Production"
   ```

6. **Build and Deploy**:
   ```bash
   cd Api
   dotnet publish --configuration Release --output ./publish
   cd publish
   zip -r ../deploy.zip .
   cd ..
   az webapp deployment source config-zip \
     --name datadog-maui-api \
     --resource-group datadog-maui-rg \
     --src deploy.zip
   ```

### Option 3: GitHub Actions (CI/CD)

For automated deployments on every push to main:

1. **Get Publish Profile**:
   ```bash
   az webapp deployment list-publishing-profiles \
     --name datadog-maui-api \
     --resource-group datadog-maui-rg \
     --xml
   ```

2. **Add GitHub Secret**:
   - Go to your GitHub repo → Settings → Secrets → Actions
   - Create a new secret named `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Paste the XML content from step 1

3. **Update Workflow**:
   Edit `.github/workflows/azure-deploy.yml` and set:
   ```yaml
   env:
     AZURE_WEBAPP_NAME: your-app-service-name
   ```

4. **Push to GitHub**:
   ```bash
   git add .
   git commit -m "Configure Azure deployment"
   git push origin main
   ```

The workflow will automatically build and deploy on every push to main.

## Datadog APM on Windows

### Automatic Instrumentation Setup

For Windows App Service, Datadog APM requires additional setup:

1. **Use Azure App Service Extension** (Easiest):
   - Go to Azure Portal → Your App Service → Extensions
   - Add "Datadog APM" extension
   - This automatically installs and configures the tracer

2. **Manual Installation** (If extension not available):
   - Run the PowerShell script during deployment:
     ```bash
     # In Kudu console or deployment script
     powershell -File scripts/install-datadog-windows.ps1
     ```

3. **Configure Environment Variables**:
   ```bash
   az webapp config appsettings set \
     --name datadog-maui-api \
     --resource-group datadog-maui-rg \
     --settings \
       CORECLR_ENABLE_PROFILING="1" \
       CORECLR_PROFILER="{846F5F1C-F9AE-4B07-969E-05C26BC060D8}" \
       CORECLR_PROFILER_PATH="D:\home\site\wwwroot\datadog\win-x64\Datadog.Trace.ClrProfiler.Native.dll" \
       DD_DOTNET_TRACER_HOME="D:\home\site\wwwroot\datadog" \
       DD_INTEGRATIONS="D:\home\site\wwwroot\datadog\integrations.json"
   ```

### Using Datadog Azure Extension (Recommended)

The easiest way is to use Datadog's native Azure integration:

1. Install Datadog extension from Azure Marketplace
2. Configure via Azure Portal or ARM template
3. Automatic instrumentation with no code changes needed

See: [Datadog Azure App Service Extension](https://docs.datadoghq.com/serverless/azure_app_services)

## Environment Variables

Required environment variables for Azure App Service:

| Variable | Description | Example |
|----------|-------------|---------|
| `DD_API_KEY` | Datadog API key | `abc123...` |
| `DD_SITE` | Datadog site | `datadoghq.com` |
| `DD_ENV` | Environment name | `production` |
| `DD_SERVICE` | Service name | `datadog-maui-api` |
| `DD_VERSION` | Service version | `1.0.0` |
| `DD_TRACE_ENABLED` | Enable APM tracing | `true` |
| `DD_RUNTIME_METRICS_ENABLED` | Enable runtime metrics | `true` |
| `DD_LOGS_INJECTION` | Enable log correlation | `true` |
| `DD_TRACE_SAMPLE_RATE` | Trace sample rate (0-1) | `1.0` |
| `DD_RUM_WEB_CLIENT_TOKEN` | RUM client token (public) | `pub123...` |
| `DD_RUM_WEB_APPLICATION_ID` | RUM application ID | `abc-123...` |

## Post-Deployment Verification

1. **Health Check**:
   ```bash
   curl https://your-app-name.azurewebsites.net/health
   ```

2. **View Logs**:
   ```bash
   az webapp log tail \
     --name datadog-maui-api \
     --resource-group datadog-maui-rg
   ```

3. **Check Datadog**:
   - APM: https://app.datadoghq.com/apm/services
   - RUM: https://app.datadoghq.com/rum/applications
   - Logs: https://app.datadoghq.com/logs

4. **Test Endpoints**:
   ```bash
   # Health check
   curl https://your-app-name.azurewebsites.net/health

   # Config
   curl https://your-app-name.azurewebsites.net/config

   # Web portal
   open https://your-app-name.azurewebsites.net
   ```

## Troubleshooting

### Traces Not Appearing in Datadog

1. Check environment variables are set correctly
2. Verify Datadog extension is installed (Windows)
3. Check application logs for tracer initialization
4. Ensure `DD_TRACE_ENABLED=true`

### Application Not Starting

1. Check application logs in Azure Portal
2. Verify .NET 9.0 runtime is selected
3. Check for missing environment variables
4. Review deployment logs

### CORS Issues

Configure CORS in Azure App Service:
```bash
az webapp cors add \
  --name datadog-maui-api \
  --resource-group datadog-maui-rg \
  --allowed-origins "https://yourdomain.com"
```

## Scaling and Performance

### Vertical Scaling (Scale Up)

Change to a more powerful tier:
```bash
az appservice plan update \
  --name datadog-maui-plan \
  --resource-group datadog-maui-rg \
  --sku P1V2
```

### Horizontal Scaling (Scale Out)

Add more instances:
```bash
az appservice plan update \
  --name datadog-maui-plan \
  --resource-group datadog-maui-rg \
  --number-of-workers 3
```

## Cost Optimization

- **Development**: B1 tier (~$13/month)
- **Production**: S1 tier (~$70/month) or P1V2 (~$80/month)
- **Enterprise**: P2V2+ with auto-scaling

## Security Best Practices

1. **Use Azure Key Vault** for secrets:
   ```bash
   # Store DD_API_KEY in Key Vault
   az keyvault secret set \
     --vault-name your-vault \
     --name DD-API-KEY \
     --value "your-api-key"
   ```

2. **Enable HTTPS only**:
   ```bash
   az webapp update \
     --name datadog-maui-api \
     --resource-group datadog-maui-rg \
     --https-only true
   ```

3. **Configure custom domain** with SSL certificate

4. **Enable managed identity** for accessing Azure resources

## Next Steps

- [ ] Configure custom domain
- [ ] Set up Azure Key Vault for secrets
- [ ] Configure Application Insights (complementary to Datadog)
- [ ] Set up auto-scaling rules
- [ ] Configure Azure Front Door or CDN
- [ ] Implement slot deployments (staging/production)

## Resources

- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [Datadog Azure Integration](https://docs.datadoghq.com/integrations/azure/)
- [Datadog .NET Tracer](https://docs.datadoghq.com/tracing/setup_overview/setup/dotnet-core/)
- [Azure App Service Extension for Datadog](https://docs.datadoghq.com/serverless/azure_app_services)
