# Testing .NET Framework API on Windows Server

Complete guide for testing the .NET Framework 4.8 API on a regular Windows Server before deploying to Azure App Service.

## Prerequisites

- Windows Server 2016+ or Windows 10/11
- Administrator privileges
- PowerShell 5.1 or higher

## Quick Start (Automated)

The fastest way to test is using the provided PowerShell scripts:

```powershell
# 1. Install IIS (one-time setup)
.\scripts\install-iis.ps1

# 2. Deploy the .NET Framework API
.\scripts\deploy-iis-framework.ps1 -DdApiKey "your-api-key-or-leave-blank"

# 3. Test it
Invoke-RestMethod http://localhost:5001/health
```

**Expected output:**
```json
{
  "status": "healthy",
  "timestamp": "2026-01-14T12:34:56.789Z"
}
```

If you see this, your API is working! Continue with the [API Testing](#api-testing) section below.

## Manual Deployment (Step-by-Step)

If you want to understand the process or customize the deployment:

### Step 1: Verify Prerequisites

**Check .NET Framework 4.8:**
```powershell
$release = (Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full").Release
if ($release -ge 528040) {
    Write-Host ".NET Framework 4.8 or higher is installed" -ForegroundColor Green
} else {
    Write-Host ".NET Framework 4.8 is NOT installed - download from:" -ForegroundColor Red
    Write-Host "https://dotnet.microsoft.com/download/dotnet-framework/net48"
}
```

**Check IIS:**
```powershell
if (Get-Command "Get-Website" -ErrorAction SilentlyContinue) {
    Write-Host "IIS is installed" -ForegroundColor Green
} else {
    Write-Host "IIS is NOT installed - run: .\scripts\install-iis.ps1" -ForegroundColor Red
}
```

**Check MSBuild:**
```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe `
    -ErrorAction SilentlyContinue | Select-Object -First 1

if ($msbuild) {
    Write-Host "MSBuild found: $msbuild" -ForegroundColor Green
} else {
    Write-Host "MSBuild NOT found - install Visual Studio Build Tools" -ForegroundColor Red
    Write-Host "https://visualstudio.microsoft.com/downloads/"
}
```

### Step 2: Build the Application

```powershell
# Navigate to project directory
cd ApiFramework

# Restore NuGet packages
# If nuget.exe is not in PATH, download it:
if (-not (Get-Command "nuget.exe" -ErrorAction SilentlyContinue)) {
    Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile "nuget.exe"
    $nuget = ".\nuget.exe"
} else {
    $nuget = "nuget.exe"
}

& $nuget restore

# Build with MSBuild
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1

& $msbuild /p:Configuration=Release /verbosity:minimal

# Verify build output
if (Test-Path "bin\Release") {
    Write-Host "Build successful! Output in bin\Release" -ForegroundColor Green
} else {
    Write-Host "Build failed - check errors above" -ForegroundColor Red
}
```

### Step 3: Create IIS Site

**Using PowerShell:**
```powershell
# Import IIS module
Import-Module WebAdministration

# Configuration
$siteName = "DatadogMauiFramework"
$appPoolName = "DatadogMauiFrameworkPool"
$port = 5001
$physicalPath = "C:\inetpub\wwwroot\datadog-maui-api-framework"

# Create deployment directory
if (-not (Test-Path $physicalPath)) {
    New-Item -ItemType Directory -Path $physicalPath -Force
}

# Copy build output to deployment directory
Copy-Item -Path "ApiFramework\bin\Release\*" -Destination $physicalPath -Recurse -Force

# Create Application Pool
if (Test-Path "IIS:\AppPools\$appPoolName") {
    Remove-WebAppPool -Name $appPoolName
}
New-WebAppPool -Name $appPoolName
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedPipelineMode" -Value "Integrated"

# Create Website
if (Test-Path "IIS:\Sites\$siteName") {
    Remove-Website -Name $siteName
}
New-Website -Name $siteName `
    -PhysicalPath $physicalPath `
    -ApplicationPool $appPoolName `
    -Port $port

# Set permissions
$acl = Get-Acl $physicalPath
$appPoolIdentity = "IIS AppPool\$appPoolName"
$permission = $appPoolIdentity, "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $physicalPath $acl

# Start the site
Start-Website -Name $siteName

Write-Host "IIS site created and started on http://localhost:$port" -ForegroundColor Green
```

**Using IIS Manager GUI:**

1. Open IIS Manager: `Start → Run → inetmgr`

2. **Create Application Pool:**
   - Right-click "Application Pools" → "Add Application Pool"
   - Name: `DatadogMauiFrameworkPool`
   - .NET CLR Version: `.NET CLR Version v4.0.30319`
   - Managed Pipeline Mode: `Integrated`
   - Click OK

3. **Create Website:**
   - Right-click "Sites" → "Add Website"
   - Site name: `DatadogMauiFramework`
   - Application pool: `DatadogMauiFrameworkPool`
   - Physical path: `C:\path\to\ApiFramework\bin\Release`
   - Port: `5001`
   - Click OK

4. **Verify:**
   - The site should appear in the Sites list
   - Status should be "Started"

### Step 4: Configure Datadog (Optional)

Edit `Web.config` in the deployment directory (`C:\inetpub\wwwroot\datadog-maui-api-framework\Web.config`):

```xml
<appSettings>
  <!-- Datadog Configuration -->
  <add key="DD_API_KEY" value="your-datadog-api-key" />
  <add key="DD_SITE" value="datadoghq.com" />
  <add key="DD_ENV" value="windows-server-testing" />
  <add key="DD_SERVICE" value="datadog-maui-api-framework" />
  <add key="DD_VERSION" value="1.0.0" />
  <add key="DD_TRACE_ENABLED" value="true" />

  <!-- CLR Profiler for automatic instrumentation -->
  <add key="COR_ENABLE_PROFILING" value="1" />
  <add key="COR_PROFILER" value="{846F5F1C-F9AE-4B07-969E-05C26BC060D8}" />
</appSettings>
```

**Install Datadog .NET Tracer:**
1. Download from: https://github.com/DataDog/dd-trace-dotnet/releases/latest
2. Run the MSI installer: `datadog-dotnet-apm-{version}-x64.msi`
3. Restart IIS: `iisreset`

**Verify Datadog Tracer Installation:**
```powershell
# Check if profiler DLL exists
$profilerPath = "$env:ProgramFiles\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll"
if (Test-Path $profilerPath) {
    Write-Host "Datadog Tracer installed successfully" -ForegroundColor Green
} else {
    Write-Host "Datadog Tracer NOT found at: $profilerPath" -ForegroundColor Red
}
```

## API Testing

### 1. Health Check

```powershell
Invoke-RestMethod http://localhost:5001/health
```

**Expected response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-01-14T12:34:56.789Z"
}
```

### 2. Authentication Flow

```powershell
# Login with demo credentials
$loginBody = @{
    username = "demo"
    password = "password"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod `
    -Uri "http://localhost:5001/auth/login" `
    -Method POST `
    -Body $loginBody `
    -ContentType "application/json"

Write-Host "Login successful!" -ForegroundColor Green
Write-Host "Token: $($loginResponse.token)"
Write-Host "User ID: $($loginResponse.userId)"
Write-Host "Username: $($loginResponse.username)"

# Save token for subsequent requests
$token = $loginResponse.token
$headers = @{
    Authorization = "Bearer $token"
}
```

**Expected response:**
```json
{
  "success": true,
  "token": "demo-token-abc123...",
  "username": "demo",
  "userId": "user-demo-123",
  "message": "Login successful"
}
```

### 3. Get User Profile

```powershell
$profileResponse = Invoke-RestMethod `
    -Uri "http://localhost:5001/user/profile" `
    -Headers $headers

Write-Host "Profile retrieved:" -ForegroundColor Green
$profileResponse | Format-List
```

**Expected response:**
```json
{
  "userId": "user-demo-123",
  "username": "demo",
  "email": "demo@example.com",
  "fullName": "Demo User",
  "createdAt": "2024-01-01T00:00:00Z",
  "lastLoginAt": "2026-01-14T12:34:56.789Z"
}
```

### 4. Get Configuration

```powershell
$configResponse = Invoke-RestMethod `
    -Uri "http://localhost:5001/config" `
    -Headers $headers

Write-Host "Configuration retrieved:" -ForegroundColor Green
$configResponse | Format-List
```

**Expected response:**
```json
{
  "webViewUrl": "https://www.example.com",
  "featureFlags": {
    "enableAdvancedMetrics": true,
    "enablePushNotifications": false
  },
  "apiVersion": "1.0.0",
  "environment": "production"
}
```

### 5. Submit Data

```powershell
$dataBody = @{
    sessionName = "Windows Server Test"
    notes = "Testing from Windows Server IIS"
    numericValue = 42
    correlationId = [guid]::NewGuid().ToString()
} | ConvertTo-Json

$submitResponse = Invoke-RestMethod `
    -Uri "http://localhost:5001/data" `
    -Method POST `
    -Body $dataBody `
    -Headers $headers `
    -ContentType "application/json"

Write-Host "Data submitted successfully!" -ForegroundColor Green
$submitResponse | Format-List
```

**Expected response:**
```json
{
  "isSuccessful": true,
  "message": "Data received successfully",
  "correlationId": "12345678-1234-1234-1234-123456789abc",
  "timestamp": "2026-01-14T12:34:56.789Z"
}
```

### 6. Retrieve Submitted Data

```powershell
$dataResponse = Invoke-RestMethod `
    -Uri "http://localhost:5001/data" `
    -Headers $headers

Write-Host "Retrieved $($dataResponse.count) submissions:" -ForegroundColor Green
$dataResponse.data | Format-Table -AutoSize
```

### 7. Complete Test Script

Save this as `test-api.ps1`:

```powershell
#Requires -Version 5.1

param(
    [string]$BaseUrl = "http://localhost:5001"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "API Testing Script" -ForegroundColor Cyan
Write-Host "Base URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Health Check
Write-Host "[1/6] Testing health endpoint..." -ForegroundColor Yellow
try {
    $health = Invoke-RestMethod "$BaseUrl/health"
    Write-Host "✓ Health check passed: $($health.status)" -ForegroundColor Green
} catch {
    Write-Host "✗ Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 2: Login
Write-Host "[2/6] Testing authentication..." -ForegroundColor Yellow
try {
    $loginBody = @{ username = "demo"; password = "password" } | ConvertTo-Json
    $login = Invoke-RestMethod -Uri "$BaseUrl/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $login.token
    $headers = @{ Authorization = "Bearer $token" }
    Write-Host "✓ Login successful - Token: $($token.Substring(0, 20))..." -ForegroundColor Green
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 3: Profile
Write-Host "[3/6] Testing user profile..." -ForegroundColor Yellow
try {
    $profile = Invoke-RestMethod -Uri "$BaseUrl/user/profile" -Headers $headers
    Write-Host "✓ Profile retrieved: $($profile.username) ($($profile.email))" -ForegroundColor Green
} catch {
    Write-Host "✗ Profile retrieval failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 4: Config
Write-Host "[4/6] Testing configuration..." -ForegroundColor Yellow
try {
    $config = Invoke-RestMethod -Uri "$BaseUrl/config" -Headers $headers
    Write-Host "✓ Config retrieved: API v$($config.apiVersion), URL: $($config.webViewUrl)" -ForegroundColor Green
} catch {
    Write-Host "✗ Config retrieval failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 5: Submit Data
Write-Host "[5/6] Testing data submission..." -ForegroundColor Yellow
try {
    $dataBody = @{
        sessionName = "Test-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        notes = "Automated test"
        numericValue = 42
        correlationId = [guid]::NewGuid().ToString()
    } | ConvertTo-Json
    $submit = Invoke-RestMethod -Uri "$BaseUrl/data" -Method POST -Body $dataBody -Headers $headers -ContentType "application/json"
    Write-Host "✓ Data submitted: $($submit.correlationId)" -ForegroundColor Green
} catch {
    Write-Host "✗ Data submission failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 6: Retrieve Data
Write-Host "[6/6] Testing data retrieval..." -ForegroundColor Yellow
try {
    $data = Invoke-RestMethod -Uri "$BaseUrl/data" -Headers $headers
    Write-Host "✓ Data retrieved: $($data.count) submissions" -ForegroundColor Green
} catch {
    Write-Host "✗ Data retrieval failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "All Tests Passed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
```

Run it:
```powershell
.\test-api.ps1
```

## Testing from Another Machine

To test the API from another machine on the same network:

### 1. Configure Windows Firewall

```powershell
# Allow inbound traffic on port 5001
New-NetFirewallRule -DisplayName "IIS Framework API" `
    -Direction Inbound `
    -Protocol TCP `
    -LocalPort 5001 `
    -Action Allow
```

### 2. Find Server IP Address

```powershell
# Get IPv4 address
$ip = (Get-NetIPAddress -AddressFamily IPv4 -InterfaceAlias "Ethernet*" | Select-Object -First 1).IPAddress
Write-Host "Server IP: $ip"
```

### 3. Test from Client Machine

```powershell
# From another machine on the network
Invoke-RestMethod "http://192.168.1.100:5001/health"
```

## Troubleshooting

### Issue: 503 Service Unavailable

**Cause:** Application pool stopped or crashed.

**Solution:**
```powershell
# Check app pool status
Get-WebAppPoolState -Name "DatadogMauiFrameworkPool"

# Start app pool
Start-WebAppPool -Name "DatadogMauiFrameworkPool"

# Check Event Viewer for errors
Get-EventLog -LogName Application -Source "ASP.NET*" -Newest 10 | Format-List
```

### Issue: 500 Internal Server Error

**Cause:** Application error or missing dependencies.

**Solution:**
```powershell
# Enable detailed errors in Web.config
# Change: <customErrors mode="Off" />

# Check application logs
Get-Content "C:\inetpub\wwwroot\datadog-maui-api-framework\*.log" -Tail 50
```

### Issue: Build Failed

**Cause:** Missing MSBuild or NuGet.

**Solution:**
```powershell
# Install Visual Studio Build Tools
# Download from: https://visualstudio.microsoft.com/downloads/

# Or via chocolatey:
choco install visualstudio2022buildtools --params "--add Microsoft.VisualStudio.Workload.WebBuildTools"
```

### Issue: Datadog Traces Not Appearing

**Cause:** Tracer not installed or environment variables not set.

**Solution:**
```powershell
# Verify tracer installation
Test-Path "$env:ProgramFiles\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll"

# Check Web.config has correct settings
# Restart IIS
iisreset

# Check application logs for Datadog initialization
Get-EventLog -LogName Application -Newest 20 | Where-Object { $_.Message -like "*Datadog*" }
```

## Comparing with Azure App Service

This local IIS setup **exactly mimics** Azure App Service Windows:

| Aspect | Local IIS | Azure App Service Windows |
|--------|-----------|---------------------------|
| Web Server | IIS 10+ | IIS 10+ |
| .NET Runtime | Framework 4.8 | Framework 4.8 |
| Configuration | Web.config | Web.config + App Settings |
| Datadog | CLR Profiler | CLR Profiler |
| Ports | Custom (5001) | 80/443 |

**What this means:** If it works on your local IIS, it will work on Azure App Service Windows. The only differences are:
- Port numbers (Azure uses 80/443)
- Environment variables (set via Azure Portal instead of Web.config)
- HTTPS certificates (Azure manages automatically)

## Next Steps

Once testing is successful:

1. **Deploy to Azure App Service:** See [Azure Deployment Guide](AZURE_DEPLOYMENT.md)
2. **Enable HTTPS:** Configure SSL certificate in IIS or Azure
3. **Scale:** Configure additional app instances in Azure
4. **Monitor:** View traces in Datadog APM dashboard

## See Also

- [IIS Deployment Scripts](../scripts/README.md) - Automated deployment
- [.NET Framework Quick Start](../FRAMEWORK_QUICKSTART.md) - Quick reference
- [.NET Comparison Guide](DOTNET_COMPARISON.md) - Core vs Framework
