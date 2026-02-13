# ApiFramework Azure Deployment Guide

This guide covers deploying the **ApiFramework** (.NET Framework 4.8) to Azure App Service using GitHub Actions with federated identity (OpenID Connect).

**Note:** For deploying the **Api** directory (.NET 9.0 container app), see [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md).

## Overview

The deployment workflow automatically deploys the `ApiFramework` directory to Azure App Service when changes are pushed to the `main` branch.

**Important:** ApiFramework is a .NET Framework 4.8 application. The workflow uses MSBuild (not `dotnet` CLI) for building and publishing.

**Key Features:**

- ✅ Monorepo-aware: Only deploys when ApiFramework changes
- ✅ Uses federated identity (no stored credentials)
- ✅ Separate build and deploy jobs
- ✅ Includes health check verification
- ✅ Manual deployment via workflow_dispatch

## Quick Setup Checklist

- [ ] Create Azure App Service
- [ ] Create Service Principal with federated credentials
- [ ] Add GitHub secrets (AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID)
- [ ] Configure Azure App Service environment variables (DD_API_KEY, etc.)
- [ ] Test deployment
- [ ] Verify health check

## GitHub Secrets Required

Add these in: GitHub repo → Settings → Secrets and variables → Actions

| Secret Name             | Value                   | Source                              |
| ----------------------- | ----------------------- | ----------------------------------- |
| `AZURE_CLIENT_ID`       | Application (client) ID | Service principal appId             |
| `AZURE_TENANT_ID`       | Directory (tenant) ID   | Service principal tenant            |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID   | `az account show --query id -o tsv` |

## Azure App Service Environment Variables

Configure these in Azure Portal → App Service → Environment variables:

| Name                     | Example Value      | Required |
| ------------------------ | ------------------ | -------- |
| `ASPNETCORE_ENVIRONMENT` | `Production`       | Yes      |
| `DD_API_KEY`             | `<your-key>`       | Yes      |
| `DD_SERVICE`             | `datadog-maui-api` | Yes      |
| `DD_ENV`                 | `production`       | Yes      |
| `DD_VERSION`             | `1.0.0`            | Yes      |
| `DD_SITE`                | `datadoghq.com`    | Yes      |

## Workflow Changes for Monorepo

The workflow has been updated to:

1. **Path filtering**: Only runs on ApiFramework changes

   ```yaml
   paths:
     - "ApiFramework/**"
     - ".github/workflows/azure-aas-deploy.yml"
   ```

2. **Working directory**: All build steps use `./ApiFramework`

   ```yaml
   working-directory: ./ApiFramework
   ```

3. **Artifact path**: Only includes published ApiFramework output
   ```yaml
   path: ${{ github.workspace }}/published/**
   ```

## Testing Deployment

### Test Locally First

**Note:** ApiFramework is a .NET Framework 4.8 application and requires MSBuild (not `dotnet` CLI).

```bash
# Build ApiFramework using MSBuild
cd ApiFramework
msbuild /t:Build /p:Configuration=Release

# Or build and publish in one command
msbuild /t:Build /t:pipelinePreDeployCopyAllFilesToOneFolder /p:_PackageTempDir="C:\temp\published\" /p:Configuration=Release

# Test locally by running the published output
# The app needs to be hosted in IIS or IIS Express for testing
```

### Deploy to Azure

```bash
# Push changes
git add .
git commit -m "Deploy ApiFramework"
git push origin main

# Or trigger manually via GitHub Actions UI
```

### Verify Deployment

```bash
# Health check
curl https://datadog-maui-framework-api.azurewebsites.net/health

# Test endpoint
curl https://datadog-maui-framework-api.azurewebsites.net/config
```

## Troubleshooting

### Build Fails

- Verify ApiFramework builds locally: `cd ApiFramework && msbuild /t:Build /p:Configuration=Release`
- Check NuGet packages are restored: `cd ApiFramework && nuget restore`
- Ensure you have .NET Framework 4.8 SDK installed
- Review build logs in GitHub Actions

### Authentication Fails

- Verify service principal federated credentials match repository
- Check GitHub secrets are set correctly
- Ensure subscription ID is correct

### Deployment Succeeds but App Doesn't Start

- Check Azure App Service logs
- Verify environment variables are set
- Check DD_API_KEY is valid

### Health Check Fails

- App may still be starting (wait 60 seconds)
- Check logs: Azure Portal → App Service → Log stream
- Verify `/health` endpoint works locally

## Related Files

- [Workflow](.github/workflows/azure-aas-deploy.yml) - GitHub Actions workflow for Azure Web App
- [Container Workflow](.github/workflows/azure-aca-container-deploy.yml) - GitHub Actions workflow for Container App
- [API Documentation](API.md) - ApiFramework endpoints
- [Local Development](../README.md) - Running locally

For detailed setup instructions, see: https://learn.microsoft.com/en-us/azure/app-service/deploy-github-actions
