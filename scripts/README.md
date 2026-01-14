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

## Troubleshooting

### Error: "The output stream for this command is already redirected"

**Problem:** Running the script from an RDP redirected drive (`\\tsclient\...`) causes PowerShell parsing errors.

**Solution:** Copy the entire project folder to a local drive on the Windows machine:

```powershell
# From the Windows machine, copy from the RDP share to local disk
Copy-Item -Path "\\tsclient\C\path\to\datadog-maui" -Destination "C:\temp\datadog-maui" -Recurse

# Navigate to the local copy
cd C:\temp\datadog-maui

# Run the script from the local copy
.\scripts\deploy-iis-framework.ps1
```

### Error: Script execution disabled

**Problem:** PowerShell execution policy prevents running scripts.

**Solution:**
```powershell
# Check current policy
Get-ExecutionPolicy

# Allow scripts for current user (recommended)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Or bypass for single script
PowerShell -ExecutionPolicy Bypass -File .\scripts\deploy-iis-framework.ps1
```

### Error: MSBuild not found

**Problem:** Visual Studio Build Tools not installed.

**Solution:**
```powershell
# Download and install Visual Studio Build Tools
# https://visualstudio.microsoft.com/downloads/

# Or via chocolatey
choco install visualstudio2022buildtools --params "--add Microsoft.VisualStudio.Workload.WebBuildTools"
```

## See Also

- [IIS Deployment Guide](../docs/IIS_DEPLOYMENT.md)
- [.NET Comparison Guide](../docs/DOTNET_COMPARISON.md)
- [Framework Quick Start](../FRAMEWORK_QUICKSTART.md)
- [Windows Server Testing Guide](../docs/WINDOWS_SERVER_TESTING.md)
