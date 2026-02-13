#!/bin/bash
set -e

# Azure App Service Deployment Script for Datadog MAUI API
# This script deploys the .NET API to Azure App Service (Windows)

echo "🚀 Datadog MAUI API - Azure Deployment Script"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "❌ Azure CLI is not installed. Please install it first:"
    echo "   https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    echo "❌ Not logged in to Azure. Please run: az login"
    exit 1
fi

# Configuration (you can override these with environment variables)
RESOURCE_GROUP="${AZURE_RESOURCE_GROUP:-datadog-maui-rg}"
APP_NAME="${AZURE_APP_NAME:-datadog-maui-api}"
LOCATION="${AZURE_LOCATION:-eastus}"
APP_SERVICE_PLAN="${AZURE_APP_SERVICE_PLAN:-datadog-maui-plan}"
SKU="${AZURE_SKU:-B1}"  # Basic tier - can upgrade to S1, P1V2, etc.

echo "📋 Deployment Configuration:"
echo "   Resource Group: $RESOURCE_GROUP"
echo "   App Service: $APP_NAME"
echo "   Location: $LOCATION"
echo "   App Service Plan: $APP_SERVICE_PLAN"
echo "   SKU: $SKU"
echo ""

# Check if resource group exists
if ! az group show --name "$RESOURCE_GROUP" &> /dev/null; then
    echo "📦 Creating resource group: $RESOURCE_GROUP"
    az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
else
    echo "✅ Resource group exists: $RESOURCE_GROUP"
fi

# Check if App Service plan exists
if ! az appservice plan show --name "$APP_SERVICE_PLAN" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
    echo "📦 Creating App Service plan: $APP_SERVICE_PLAN"
    az appservice plan create \
        --name "$APP_SERVICE_PLAN" \
        --resource-group "$RESOURCE_GROUP" \
        --location "$LOCATION" \
        --sku "$SKU" \
        --is-linux false
else
    echo "✅ App Service plan exists: $APP_SERVICE_PLAN"
fi

# Check if App Service exists
if ! az webapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
    echo "📦 Creating App Service: $APP_NAME"
    az webapp create \
        --name "$APP_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --plan "$APP_SERVICE_PLAN" \
        --runtime "DOTNET|9.0"
else
    echo "✅ App Service exists: $APP_NAME"
fi

# Configure App Settings (Datadog configuration)
echo "⚙️  Configuring App Settings..."

# Prompt for Datadog API key if not set
if [ -z "$DD_API_KEY" ]; then
    echo ""
    echo "⚠️  DD_API_KEY environment variable not set"
    read -p "Enter your Datadog API key: " DD_API_KEY
fi

# Set application settings
az webapp config appsettings set \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings \
        DD_API_KEY="$DD_API_KEY" \
        DD_SITE="${DD_SITE:-datadoghq.com}" \
        DD_ENV="${DD_ENV:-production}" \
        DD_SERVICE="datadog-maui-api" \
        DD_VERSION="1.0.0" \
        DD_TRACE_ENABLED="true" \
        DD_RUNTIME_METRICS_ENABLED="true" \
        DD_LOGS_INJECTION="true" \
        DD_TRACE_SAMPLE_RATE="1.0" \
        DD_TRACE_PROPAGATION_STYLE="datadog,tracecontext" \
        DD_PROFILING_ENABLED="false" \
        DD_RUM_WEB_CLIENT_TOKEN="${DD_RUM_WEB_CLIENT_TOKEN:-}" \
        DD_RUM_WEB_APPLICATION_ID="${DD_RUM_WEB_APPLICATION_ID:-}" \
        DD_RUM_WEB_SERVICE="${DD_RUM_WEB_SERVICE:-datadog-maui-api}" \
        ASPNETCORE_ENVIRONMENT="Production" \
        WEBSITE_RUN_FROM_PACKAGE="1" \
    --output none

echo "✅ App Settings configured"

# Enable Application Insights (optional but recommended)
echo "⚙️  Configuring Application Insights..."
az webapp config appsettings set \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings \
        APPLICATIONINSIGHTS_CONNECTION_STRING="" \
    --output none

# Configure CORS (optional)
echo "⚙️  Configuring CORS..."
az webapp cors add \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --allowed-origins "*" \
    --output none || true

# Build and publish the app
echo ""
echo "🔨 Building and publishing the API..."
cd "$(dirname "$0")/../Api"

# Clean previous builds
dotnet clean --configuration Release

# Restore dependencies
dotnet restore

# Publish to folder
dotnet publish --configuration Release --output ./publish

# Create deployment package
echo "📦 Creating deployment package..."
cd publish
zip -r ../deploy.zip . > /dev/null
cd ..

echo "✅ Deployment package created: Api/deploy.zip"

# Deploy to Azure
echo ""
echo "🚀 Deploying to Azure App Service..."
az webapp deployment source config-zip \
    --name "$APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --src deploy.zip

echo ""
echo "✅ Deployment complete!"
echo ""
echo "📊 App Service URL: https://$APP_NAME.azurewebsites.net"
echo "🏥 Health Check: https://$APP_NAME.azurewebsites.net/health"
echo "🌐 Web Portal: https://$APP_NAME.azurewebsites.net"
echo ""
echo "📋 Next steps:"
echo "   1. Test the API: curl https://$APP_NAME.azurewebsites.net/health"
echo "   2. View logs: az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP"
echo "   3. View in Azure Portal: https://portal.azure.com"
echo "   4. View in Datadog: https://app.datadoghq.com/apm/services"
echo ""

# Clean up deployment package
rm -f deploy.zip
echo "🧹 Cleaned up deployment package"
