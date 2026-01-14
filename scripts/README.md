# Deployment Scripts

PowerShell scripts for deploying the Datadog MAUI demo APIs to IIS on Windows.

## Prerequisites

- Windows Server 2016+ or Windows 10/11
- PowerShell 5.1 or higher
- Administrator privileges

## Quick Start

### 1. Install IIS

```powershell
# Run as Administrator
.\scripts\install-iis.ps1
```

### 2. Install Runtime Dependencies

**For .NET Core 9.0 API:**
- Download and install [.NET 9.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0)

**For .NET Framework 4.8 API:**
- .NET Framework 4.8 is built into Windows 10/11 and Windows Server 2019+

**For Datadog Automatic Instrumentation:**
- Download and install [Datadog .NET Tracer](https://github.com/DataDog/dd-trace-dotnet/releases/latest)

### 3. Restart IIS

```powershell
iisreset
```

### 4. Deploy Applications

**Deploy .NET Core 9.0 API (Port 5000):**
```powershell
.\scripts\deploy-iis-core.ps1 -DdApiKey "your-datadog-api-key"
```

**Deploy .NET Framework 4.8 API (Port 5001):**
```powershell
.\scripts\deploy-iis-framework.ps1 -DdApiKey "your-datadog-api-key"
```

## Script Reference

### install-iis.ps1

Installs IIS with all required Windows features.

### deploy-iis-core.ps1

Deploys the .NET Core 9.0 API to IIS.

**Parameters:**
- `-SiteName` - IIS website name (default: `DatadogMauiApi`)
- `-AppPoolName` - Application pool name (default: `DatadogMauiApiPool`)
- `-Port` - HTTP port (default: `5000`)
- `-PhysicalPath` - Deployment directory
- `-DdApiKey` - Datadog API key
- `-DdEnv` - Datadog environment tag (default: `production`)

### deploy-iis-framework.ps1

Deploys the .NET Framework 4.8 API to IIS.

**Parameters:**
- `-SiteName` - IIS website name (default: `DatadogMauiApiFramework`)
- `-AppPoolName` - Application pool name (default: `DatadogMauiApiFrameworkPool`)
- `-Port` - HTTP port (default: `5001`)
- `-PhysicalPath` - Deployment directory
- `-DdApiKey` - Datadog API key
- `-DdEnv` - Datadog environment tag (default: `production`)

## Testing Deployments

### Health Check

```powershell
# Test .NET Core API
Invoke-RestMethod http://localhost:5000/health

# Test .NET Framework API
Invoke-RestMethod http://localhost:5001/health
```

## See Also

- [IIS Deployment Guide](../docs/IIS_DEPLOYMENT.md)
- [.NET Comparison Guide](../docs/DOTNET_COMPARISON.md)
- [Framework Quick Start](../FRAMEWORK_QUICKSTART.md)
