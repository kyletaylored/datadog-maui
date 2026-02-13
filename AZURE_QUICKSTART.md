# Azure Deployment Quick Start

## Overview

Your Datadog MAUI API is now ready to deploy to Azure App Service on Windows! Here's everything you need to know.

## Files Created

### Deployment Scripts

- `scripts/deploy-azure.sh` - Automated deployment script (Bash)
- `scripts/install-datadog-windows.ps1` - Datadog tracer installation for Windows
- `.github/workflows/azure-aca-container-deploy.yml` - GitHub Actions CI/CD workflow
- `.deployment` - Azure project configuration

### Configuration Files

- `Api/web.config` - IIS/ASP.NET Core configuration for Windows
- `docs/AZURE_DEPLOYMENT.md` - Comprehensive deployment guide

### Makefile Commands

```bash
make azure-deploy    # Deploy to Azure
make azure-logs      # View logs
make azure-status    # Check status
make azure-health    # Test deployment
```

## Quick Deployment (3 Steps)

### 1. Install Azure CLI

```bash
# macOS
brew install azure-cli

# Or download from: https://aka.ms/installazurecliwindows
```

### 2. Set Environment Variables

```bash
export DD_API_KEY="your-datadog-api-key"
export DD_RUM_WEB_CLIENT_TOKEN="your-rum-client-token"
export DD_RUM_WEB_APPLICATION_ID="your-rum-app-id"
export DD_RUM_WEB_SERVICE="your-rum-service-name"

# Optional customization
export AZURE_APP_NAME="my-custom-name"
export AZURE_RESOURCE_GROUP="my-rg"
export AZURE_LOCATION="centralus"
```

### 3. Deploy

```bash
# Login to Azure (first time only)
az login

# Deploy
make azure-deploy

# Or run the script directly
./scripts/deploy-azure.sh
```

That's it! Your API will be available at:
`https://your-app-name.azurewebsites.net`

## What the Deployment Does

1. ✅ Creates Azure Resource Group (if needed)
2. ✅ Creates App Service Plan (Windows, B1 tier)
3. ✅ Creates App Service (.NET 9.0)
4. ✅ Configures Datadog environment variables
5. ✅ Builds and publishes the API
6. ✅ Deploys to Azure
7. ✅ Configures CORS
8. ✅ Sets up Application Insights

## Datadog APM on Windows

The deployment includes automatic APM configuration, but for full functionality on Windows, you have two options:

### Option A: Azure Extension (Recommended - Easiest)

1. Go to Azure Portal → Your App Service → Extensions
2. Click "Add" → Search for "Datadog APM"
3. Install the extension
4. Done! Automatic instrumentation enabled

### Option B: Manual Installation

The deployment script sets up the base configuration. For Windows-specific tracer:

```bash
# SSH into your app service (via Kudu)
https://your-app-name.scm.azurewebsites.net/webssh/host

# Run the installation script
cd site/wwwroot
powershell -File scripts/install-datadog-windows.ps1
```

## Environment Variables

The deployment automatically configures these Datadog variables:

```bash
DD_API_KEY                    # Your Datadog API key
DD_SITE                       # datadoghq.com (or your site)
DD_ENV                        # production
DD_SERVICE                    # datadog-maui-api
DD_VERSION                    # 1.0.0
DD_TRACE_ENABLED              # true
DD_RUNTIME_METRICS_ENABLED    # true
DD_LOGS_INJECTION             # true
DD_TRACE_SAMPLE_RATE          # 1.0 (100%)
DD_TRACE_PROPAGATION_STYLE    # datadog,tracecontext
DD_RUM_WEB_CLIENT_TOKEN       # Your RUM token
DD_RUM_WEB_APPLICATION_ID     # Your RUM app ID
DD_RUM_WEB_SERVICE            # Your RUM service name
```

## Testing Your Deployment

```bash
# Test health endpoint
curl https://your-app-name.azurewebsites.net/health

# Or use the Makefile command
make azure-health

# View logs
make azure-logs

# Check Datadog for traces
# https://app.datadoghq.com/apm/services/datadog-maui-api
```

## CI/CD with GitHub Actions

To enable automated deployments:

1. **Get Publish Profile**:

   ```bash
   az webapp deployment list-publishing-profiles \
     --name your-app-name \
     --resource-group your-rg \
     --xml > publish-profile.xml
   ```

2. **Add GitHub Secret**:
   - Go to GitHub repo → Settings → Secrets → Actions
   - New secret: `AZURE_WEBAPP_PUBLISH_PROFILE`
   - Paste contents of `publish-profile.xml`

3. **Update workflow file**:
   Edit `.github/workflows/azure-aca-container-deploy.yml`:

   ```yaml
   env:
     AZURE_WEBAPP_NAME: your-app-name
   ```

4. **Push to main branch** - Automatic deployment!

## Pricing

Default configuration uses **B1 tier** (~$13/month):

- 1 core, 1.75 GB RAM
- Perfect for development/staging

For production, consider:

- **S1**: ~$70/month (1 core, 1.75 GB, custom domains, auto-scale)
- **P1V2**: ~$80/month (1 core, 3.5 GB, better performance)

Change tier:

```bash
az appservice plan update \
  --name datadog-maui-plan \
  --resource-group datadog-maui-rg \
  --sku S1
```

## Monitoring in Datadog

After deployment, check:

1. **APM Traces**: https://app.datadoghq.com/apm/services
   - Look for `datadog-maui-api` service
   - View traces with custom attributes under `custom.*`

2. **RUM Sessions**: https://app.datadoghq.com/rum/applications
   - Web portal user sessions
   - Frontend-backend correlation

3. **Logs**: https://app.datadoghq.com/logs
   - Application logs with trace correlation
   - Filter by `service:datadog-maui-api`

## Troubleshooting

### "Resource group not found"

```bash
# Create manually
az group create --name datadog-maui-rg --location eastus
```

### "App name already taken"

```bash
# Use custom name
export AZURE_APP_NAME="datadog-maui-api-$(whoami)"
make azure-deploy
```

### "Not logged in to Azure"

```bash
az login
az account show
```

### "Deployment failed"

```bash
# Check logs
make azure-logs

# Or in Azure Portal
# App Service → Deployment Center → Logs
```

### "No traces in Datadog"

1. Check `DD_API_KEY` is set correctly
2. Verify Datadog extension installed (Azure Portal → Extensions)
3. Check logs for tracer initialization
4. Ensure `DD_TRACE_ENABLED=true`

## Next Steps

- [ ] Deploy to Azure: `make azure-deploy`
- [ ] Test health endpoint: `make azure-health`
- [ ] Configure custom domain
- [ ] Set up GitHub Actions CI/CD
- [ ] Add Azure Key Vault for secrets
- [ ] Configure auto-scaling
- [ ] Set up staging slot

## Resources

- 📖 [Full Deployment Guide](docs/AZURE_DEPLOYMENT.md)
- 🌐 [Azure Portal](https://portal.azure.com)
- 🐕 [Datadog APM](https://app.datadoghq.com/apm)
- 💬 [GitHub Issues](https://github.com/yourusername/datadog-maui/issues)

## Support

Need help? Check:

1. [Azure App Service docs](https://docs.microsoft.com/en-us/azure/app-service/)
2. [Datadog Azure integration](https://docs.datadoghq.com/integrations/azure/)
3. Project documentation in `docs/` folder
