# IIS Deployment Guide for Windows

Complete guide for deploying both .NET Core and .NET Framework APIs to IIS on Windows.

## Prerequisites

- Windows Server 2016+ or Windows 10/11
- IIS 10+ installed
- Administrator access

### For .NET Core 9.0 API
- [.NET 9.0 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/9.0) (includes ASP.NET Core Module)
- Or install via: `dotnet-hosting-9.0.x-win.exe`

### For .NET Framework 4.8 API
- .NET Framework 4.8 Runtime (usually pre-installed on Windows Server 2019+)
- ASP.NET registered with IIS

## Installation Steps

### 1. Install IIS Features

Run PowerShell as Administrator:

```powershell
# Install IIS with required features
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpRedirect
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ApplicationDevelopment
Enable-WindowsOptionalFeature -Online -FeatureName IIS-NetFxExtensibility45
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HealthAndDiagnostics
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging
Enable-WindowsOptionalFeature -Online -FeatureName IIS-LoggingLibraries
Enable-WindowsOptionalFeature -Online -FeatureName IIS-RequestMonitor
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpTracing
Enable-WindowsOptionalFeature -Online -FeatureName IIS-Security
Enable-WindowsOptionalFeature -Online -FeatureName IIS-RequestFiltering
Enable-WindowsOptionalFeature -Online -FeatureName IIS-Performance
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerManagementTools
Enable-WindowsOptionalFeature -Online -FeatureName IIS-IIS6ManagementCompatibility
Enable-WindowsOptionalFeature -Online -FeatureName IIS-Metabase
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-BasicAuthentication
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WindowsAuthentication
Enable-WindowsOptionalFeature -Online -FeatureName IIS-StaticContent
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DefaultDocument
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebSockets
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ApplicationInit
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIExtensions
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIFilter
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpCompressionStatic
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45

# Or use DISM (alternative method)
DISM /Online /Enable-Feature /FeatureName:IIS-WebServerRole /All
DISM /Online /Enable-Feature /FeatureName:IIS-ASPNET45 /All
```

### 2. Install .NET Runtimes

#### For .NET Core 9.0 API

Download and install the ASP.NET Core Hosting Bundle:
```powershell
# Download
Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/some-guid/dotnet-hosting-9.0.x-win.exe" -OutFile "$env:TEMP\dotnet-hosting.exe"

# Install
Start-Process -FilePath "$env:TEMP\dotnet-hosting.exe" -ArgumentList "/quiet /norestart" -Wait

# Restart IIS to load the module
net stop was /y
net start w3svc
```

Verify installation:
```powershell
dotnet --list-runtimes
# Should show: Microsoft.AspNetCore.App 9.0.x
```

#### For .NET Framework 4.8 API

Usually pre-installed. Verify:
```powershell
(Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full").Release -ge 528040
# Should return True for .NET 4.8+
```

## Deploying .NET Core 9.0 API to IIS

### Method 1: PowerShell Script (Automated)

Save as `deploy-iis-core.ps1`:

```powershell
#Requires -RunAsAdministrator

param(
    [string]$SiteName = "DatadogMauiApi",
    [string]$AppPoolName = "DatadogMauiApiPool",
    [string]$Port = 5000,
    [string]$PhysicalPath = "C:\inetpub\wwwroot\datadog-maui-api"
)

Write-Host "🚀 Deploying .NET Core API to IIS" -ForegroundColor Green

# Import IIS module
Import-Module WebAdministration

# Build the application
Write-Host "📦 Building application..." -ForegroundColor Yellow
Push-Location "$PSScriptRoot\..\Api"
dotnet publish -c Release -o publish
Pop-Location

# Create physical directory
Write-Host "📁 Creating deployment directory..." -ForegroundColor Yellow
if (!(Test-Path $PhysicalPath)) {
    New-Item -ItemType Directory -Path $PhysicalPath -Force
}

# Copy published files
Write-Host "📋 Copying files..." -ForegroundColor Yellow
Copy-Item -Path "$PSScriptRoot\..\Api\publish\*" -Destination $PhysicalPath -Recurse -Force

# Create Application Pool
Write-Host "🏊 Creating Application Pool..." -ForegroundColor Yellow
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Remove-WebAppPool -Name $AppPoolName
}

New-WebAppPool -Name $AppPoolName
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "enable32BitAppOnWin64" -Value $false
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"

# Create Website
Write-Host "🌐 Creating Website..." -ForegroundColor Yellow
if (Test-Path "IIS:\Sites\$SiteName") {
    Remove-Website -Name $SiteName
}

New-Website -Name $SiteName `
    -Port $Port `
    -PhysicalPath $PhysicalPath `
    -ApplicationPool $AppPoolName `
    -Force

# Set permissions
Write-Host "🔐 Setting permissions..." -ForegroundColor Yellow
$acl = Get-Acl $PhysicalPath
$permission = "IIS AppPool\$AppPoolName","Read,ReadAndExecute","ContainerInherit,ObjectInherit","None","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $PhysicalPath $acl

# Configure Application Settings (Environment Variables)
Write-Host "⚙️  Configuring Datadog..." -ForegroundColor Yellow

# Set environment variables in web.config or use appsettings
# For Datadog, you can use Azure App Settings or IIS Manager

Write-Host ""
Write-Host "✅ Deployment complete!" -ForegroundColor Green
Write-Host "   URL: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "   Physical Path: $PhysicalPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Configure Datadog API key in IIS Manager → Configuration Editor"
Write-Host "   2. Test: Invoke-RestMethod http://localhost:$Port/health"
Write-Host "   3. View logs in: $PhysicalPath\logs"
```

Run it:
```powershell
.\scripts\deploy-iis-core.ps1
```

### Method 2: Manual IIS Manager

1. **Build the Application**:
   ```powershell
   cd Api
   dotnet publish -c Release -o C:\inetpub\wwwroot\datadog-maui-api
   ```

2. **Open IIS Manager** (`inetmgr.exe`)

3. **Create Application Pool**:
   - Right-click "Application Pools" → Add Application Pool
   - Name: `DatadogMauiApiPool`
   - .NET CLR version: **No Managed Code**
   - Managed pipeline mode: Integrated
   - Click OK

4. **Create Website**:
   - Right-click "Sites" → Add Website
   - Site name: `DatadogMauiApi`
   - Physical path: `C:\inetpub\wwwroot\datadog-maui-api`
   - Application pool: `DatadogMauiApiPool`
   - Port: `5000`
   - Click OK

5. **Configure Datadog Environment Variables**:
   - Select your site in IIS Manager
   - Open "Configuration Editor"
   - Section: `system.webServer/aspNetCore`
   - environmentVariables → Click "..." to add:
     ```
     DD_API_KEY = your-api-key
     DD_SITE = datadoghq.com
     DD_ENV = production
     DD_SERVICE = datadog-maui-api
     DD_TRACE_ENABLED = true
     ```

6. **Start the Website**:
   - Right-click site → Manage Website → Start

## Deploying .NET Framework 4.8 API to IIS

### Method 1: PowerShell Script (Automated)

Save as `deploy-iis-framework.ps1`:

```powershell
#Requires -RunAsAdministrator

param(
    [string]$SiteName = "DatadogMauiApiFramework",
    [string]$AppPoolName = "DatadogMauiApiFrameworkPool",
    [string]$Port = 5001,
    [string]$PhysicalPath = "C:\inetpub\wwwroot\datadog-maui-api-framework"
)

Write-Host "🚀 Deploying .NET Framework API to IIS" -ForegroundColor Green

# Import IIS module
Import-Module WebAdministration

# Build the application
Write-Host "📦 Building application..." -ForegroundColor Yellow
Push-Location "$PSScriptRoot\..\ApiFramework"
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" `
    /p:Configuration=Release `
    /p:DeployOnBuild=true `
    /p:PublishProfile=FolderProfile
Pop-Location

# Create physical directory
Write-Host "📁 Creating deployment directory..." -ForegroundColor Yellow
if (!(Test-Path $PhysicalPath)) {
    New-Item -ItemType Directory -Path $PhysicalPath -Force
}

# Copy published files
Write-Host "📋 Copying files..." -ForegroundColor Yellow
Copy-Item -Path "$PSScriptRoot\..\ApiFramework\bin\Release\*" -Destination $PhysicalPath -Recurse -Force

# Create Application Pool
Write-Host "🏊 Creating Application Pool..." -ForegroundColor Yellow
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Remove-WebAppPool -Name $AppPoolName
}

New-WebAppPool -Name $AppPoolName
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "enable32BitAppOnWin64" -Value $false
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"

# Create Website
Write-Host "🌐 Creating Website..." -ForegroundColor Yellow
if (Test-Path "IIS:\Sites\$SiteName") {
    Remove-Website -Name $SiteName
}

New-Website -Name $SiteName `
    -Port $Port `
    -PhysicalPath $PhysicalPath `
    -ApplicationPool $AppPoolName `
    -Force

# Set permissions
Write-Host "🔐 Setting permissions..." -ForegroundColor Yellow
$acl = Get-Acl $PhysicalPath
$permission = "IIS AppPool\$AppPoolName","Read,ReadAndExecute","ContainerInherit,ObjectInherit","None","Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $PhysicalPath $acl

Write-Host ""
Write-Host "✅ Deployment complete!" -ForegroundColor Green
Write-Host "   URL: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "   Physical Path: $PhysicalPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Edit Web.config to add DD_API_KEY"
Write-Host "   2. Test: Invoke-RestMethod http://localhost:$Port/health"
Write-Host "   3. Install Datadog .NET APM MSI if not already installed"
```

### Method 2: Manual IIS Manager

1. **Build the Application**:
   ```powershell
   cd ApiFramework
   msbuild /p:Configuration=Release
   ```

2. **Open IIS Manager** (`inetmgr.exe`)

3. **Create Application Pool**:
   - Right-click "Application Pools" → Add Application Pool
   - Name: `DatadogMauiApiFrameworkPool`
   - .NET CLR version: **.NET CLR Version v4.0.30319**
   - Managed pipeline mode: Integrated
   - Click OK

4. **Create Website**:
   - Right-click "Sites" → Add Website
   - Site name: `DatadogMauiApiFramework`
   - Physical path: `C:\path\to\ApiFramework\bin\Release`
   - Application pool: `DatadogMauiApiFrameworkPool`
   - Port: `5001`
   - Click OK

5. **Configure Datadog in Web.config**:
   - Edit `Web.config` in the physical path
   - Update `<add key="DD_API_KEY" value="your-api-key" />`

6. **Install Datadog .NET Tracer** (if not already):
   ```powershell
   # Download MSI
   Invoke-WebRequest -Uri "https://github.com/DataDog/dd-trace-dotnet/releases/download/v3.8.0/datadog-dotnet-apm-3.8.0-x64.msi" -OutFile "$env:TEMP\datadog-apm.msi"

   # Install
   msiexec /i "$env:TEMP\datadog-apm.msi" /quiet

   # Restart IIS
   iisreset
   ```

7. **Start the Website**:
   - Right-click site → Manage Website → Start

## Azure App Service (Windows) Deployment

Both APIs work on Azure App Service Windows. The process is simpler:

### .NET Core 9.0 on Azure Windows

```bash
# Create App Service
az webapp create \
  --name datadog-maui-api \
  --resource-group my-rg \
  --plan my-plan \
  --runtime "DOTNET|9.0"

# Configure Datadog
az webapp config appsettings set \
  --name datadog-maui-api \
  --resource-group my-rg \
  --settings \
    DD_API_KEY="your-key" \
    DD_SITE="datadoghq.com" \
    DD_ENV="production" \
    DD_SERVICE="datadog-maui-api" \
    DD_TRACE_ENABLED="true"

# Deploy
cd Api
dotnet publish -c Release
cd bin/Release/net9.0/publish
zip -r deploy.zip .
az webapp deployment source config-zip \
  --name datadog-maui-api \
  --resource-group my-rg \
  --src deploy.zip
```

### .NET Framework 4.8 on Azure Windows

```bash
# Create App Service
az webapp create \
  --name datadog-maui-api-framework \
  --resource-group my-rg \
  --plan my-plan \
  --runtime "DOTNETFRAMEWORK|4.8"

# Configure Datadog via Portal or CLI
az webapp config appsettings set \
  --name datadog-maui-api-framework \
  --resource-group my-rg \
  --settings \
    DD_API_KEY="your-key" \
    DD_SITE="datadoghq.com" \
    DD_ENV="production" \
    DD_SERVICE="datadog-maui-api-framework" \
    DD_TRACE_ENABLED="true"

# Deploy via Visual Studio Publish or ZIP
# (Use Visual Studio: Right-click project → Publish → Azure)
```

## Verification & Testing

### Test .NET Core API
```powershell
# Health check
Invoke-RestMethod http://localhost:5000/health

# Login
$body = @{username="demo"; password="password"} | ConvertTo-Json
$response = Invoke-RestMethod http://localhost:5000/auth/login -Method POST -Body $body -ContentType "application/json"
$response.token
```

### Test .NET Framework API
```powershell
# Health check
Invoke-RestMethod http://localhost:5001/health

# Login
$body = @{username="demo"; password="password"} | ConvertTo-Json
$response = Invoke-RestMethod http://localhost:5001/auth/login -Method POST -Body $body -ContentType "application/json"
$response.token
```

## Troubleshooting

### .NET Core Issues

**Error: "HTTP Error 500.31 - Failed to load ASP.NET Core runtime"**
- Install ASP.NET Core Hosting Bundle
- Restart IIS: `iisreset`
- Check event logs

**Error: "HTTP Error 500.19 - Internal Server Error"**
- Check web.config exists
- Verify ASP.NET Core Module is installed
- Check file permissions

**Datadog traces not appearing:**
```powershell
# Check if tracer DLL exists
Test-Path "D:\home\site\wwwroot\datadog\win-x64\Datadog.Trace.ClrProfiler.Native.dll"

# Check environment variables in web.config
Get-Content "C:\path\to\web.config" | Select-String -Pattern "CORECLR"
```

### .NET Framework Issues

**Error: "HTTP Error 500.0 - Internal Server Error"**
- Check .NET Framework 4.8 is installed
- Verify application pool is v4.0
- Check Web.config for errors

**Error: "Could not load file or assembly"**
- Run `aspnet_regiis -i` to register ASP.NET
- Check bin folder has all DLLs
- Verify NuGet packages were restored

**Datadog HTTP Module not loading:**
```powershell
# Check if Datadog.Trace.AspNet.dll exists
Test-Path "C:\path\to\bin\Datadog.Trace.AspNet.dll"

# Check Web.config has HTTP Module
Get-Content "C:\path\to\Web.config" | Select-String -Pattern "DatadogHttpModule"

# Check IIS modules
Get-WebConfigurationProperty -PSPath 'MACHINE/WEBROOT/APPHOST' -Filter "system.webServer/modules/add[@name='DatadogHttpModule']" -Name .
```

## Security Best Practices

1. **Use HTTPS**:
   ```powershell
   # Add HTTPS binding
   New-WebBinding -Name "DatadogMauiApi" -IP "*" -Port 443 -Protocol https
   ```

2. **Store secrets securely**:
   - Use Azure Key Vault for API keys
   - Don't commit Web.config with real keys

3. **Set proper permissions**:
   ```powershell
   # Minimum required permissions for IIS App Pool
   icacls "C:\path\to\site" /grant "IIS AppPool\YourAppPool:(OI)(CI)R"
   ```

4. **Enable Windows Firewall rules**:
   ```powershell
   New-NetFirewallRule -DisplayName "Datadog API" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
   ```

## Monitoring & Logs

### IIS Logs
```powershell
# View IIS logs
Get-Content "C:\inetpub\logs\LogFiles\W3SVC1\u_ex$(Get-Date -Format yyMMdd).log" -Tail 50
```

### Application Logs
```powershell
# .NET Core
Get-Content "C:\inetpub\wwwroot\datadog-maui-api\logs\stdout_$(Get-Date -Format yyyyMMdd).log" -Tail 50

# .NET Framework (Event Viewer)
Get-EventLog -LogName Application -Source "ASP.NET" -Newest 50
```

### Datadog Logs
```powershell
# Check Datadog tracer logs
Get-Content "C:\ProgramData\Datadog .NET Tracer\logs\dotnet-tracer.log" -Tail 50
```

## Resources

- [ASP.NET Core on IIS](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/)
- [Datadog .NET Tracer for IIS](https://docs.datadoghq.com/tracing/setup_overview/setup/dotnet-core/)
- [IIS Administration PowerShell](https://docs.microsoft.com/en-us/powershell/module/iisadministration/)
