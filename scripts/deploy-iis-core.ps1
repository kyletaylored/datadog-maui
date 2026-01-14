#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Deploys the .NET Core 9.0 API to IIS
.DESCRIPTION
    Builds the .NET Core API and deploys it to IIS with proper configuration
.PARAMETER SiteName
    Name of the IIS website (default: DatadogMauiApi)
.PARAMETER AppPoolName
    Name of the IIS application pool (default: DatadogMauiApiPool)
.PARAMETER Port
    Port number for the website (default: 5000)
.PARAMETER PhysicalPath
    Physical path where the application will be deployed (default: C:\inetpub\wwwroot\datadog-maui-api)
.PARAMETER DdApiKey
    Datadog API key (optional, will prompt if not provided)
.PARAMETER DdEnv
    Datadog environment tag (default: production)
.EXAMPLE
    .\deploy-iis-core.ps1 -DdApiKey "your-api-key"
#>

param(
    [string]$SiteName = "DatadogMauiApi",
    [string]$AppPoolName = "DatadogMauiApiPool",
    [int]$Port = 5000,
    [string]$PhysicalPath = "C:\inetpub\wwwroot\datadog-maui-api",
    [string]$DdApiKey = "",
    [string]$DdEnv = "production"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Datadog MAUI API (.NET Core 9.0)" -ForegroundColor Cyan
Write-Host "IIS Deployment Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify we're in the correct directory
if (-not (Test-Path "Api/DatadogMauiApi.csproj")) {
    Write-Error "Error: Must run this script from the repository root directory"
    exit 1
}

# Verify IIS is installed
if (-not (Get-Command "Get-Website" -ErrorAction SilentlyContinue)) {
    Write-Error "Error: IIS is not installed. Run 'scripts/install-iis.ps1' first"
    exit 1
}

# Verify .NET Core 9.0 Hosting Bundle is installed
$hostingBundle = Get-ItemProperty "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Updates\.NET\*" -ErrorAction SilentlyContinue |
    Where-Object { $_.DisplayName -like "*ASP.NET Core*Hosting Bundle*" }

if (-not $hostingBundle) {
    Write-Warning "Warning: ASP.NET Core Hosting Bundle may not be installed"
    Write-Host "Download from: https://dotnet.microsoft.com/download/dotnet/9.0"
    $continue = Read-Host "Continue anyway? (y/n)"
    if ($continue -ne "y") {
        exit 1
    }
}

# Prompt for Datadog API key if not provided
if ([string]::IsNullOrEmpty($DdApiKey)) {
    $DdApiKey = Read-Host "Enter Datadog API key (or press Enter to skip)"
}

Write-Host "[1/7] Building application..." -ForegroundColor Yellow
Push-Location Api
try {
    dotnet publish -c Release -o ../publish/core
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
} finally {
    Pop-Location
}
Write-Host "✓ Build complete" -ForegroundColor Green

Write-Host "[2/7] Creating physical directory..." -ForegroundColor Yellow
if (-not (Test-Path $PhysicalPath)) {
    New-Item -ItemType Directory -Path $PhysicalPath -Force | Out-Null
}

Write-Host "[3/7] Copying files to deployment directory..." -ForegroundColor Yellow
Copy-Item -Path "publish/core/*" -Destination $PhysicalPath -Recurse -Force
Write-Host "✓ Files copied" -ForegroundColor Green

Write-Host "[4/7] Configuring Application Pool..." -ForegroundColor Yellow
# Remove existing app pool if it exists
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Write-Host "  Removing existing app pool..." -ForegroundColor Gray
    Remove-WebAppPool -Name $AppPoolName
}

# Create new app pool
New-WebAppPool -Name $AppPoolName | Out-Null
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
Write-Host "✓ App pool created: $AppPoolName" -ForegroundColor Green

Write-Host "[5/7] Configuring Website..." -ForegroundColor Yellow
# Remove existing website if it exists
if (Test-Path "IIS:\Sites\$SiteName") {
    Write-Host "  Removing existing website..." -ForegroundColor Gray
    Remove-Website -Name $SiteName
}

# Create new website
New-Website -Name $SiteName `
    -Port $Port `
    -PhysicalPath $PhysicalPath `
    -ApplicationPool $AppPoolName `
    -Force | Out-Null

Write-Host "✓ Website created: $SiteName" -ForegroundColor Green

Write-Host "[6/7] Configuring permissions..." -ForegroundColor Yellow
$acl = Get-Acl $PhysicalPath
$appPoolIdentity = "IIS AppPool\$AppPoolName"
$permission = $appPoolIdentity, "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $PhysicalPath $acl
Write-Host "✓ Permissions configured" -ForegroundColor Green

Write-Host "[7/7] Configuring Datadog environment variables..." -ForegroundColor Yellow
$webConfigPath = Join-Path $PhysicalPath "web.config"
if (Test-Path $webConfigPath) {
    [xml]$webConfig = Get-Content $webConfigPath

    # Ensure environmentVariables section exists
    $aspNetCore = $webConfig.configuration.'system.webServer'.aspNetCore
    if ($null -eq $aspNetCore.environmentVariables) {
        $envVars = $webConfig.CreateElement("environmentVariables")
        $aspNetCore.AppendChild($envVars) | Out-Null
    }

    # Helper function to set environment variable
    function Set-EnvVar($name, $value) {
        $envVars = $webConfig.configuration.'system.webServer'.aspNetCore.environmentVariables
        $existing = $envVars.environmentVariable | Where-Object { $_.name -eq $name }

        if ($existing) {
            $existing.value = $value
        } else {
            $newVar = $webConfig.CreateElement("environmentVariable")
            $newVar.SetAttribute("name", $name)
            $newVar.SetAttribute("value", $value)
            $envVars.AppendChild($newVar) | Out-Null
        }
    }

    # Set Datadog variables
    if (-not [string]::IsNullOrEmpty($DdApiKey)) {
        Set-EnvVar "DD_API_KEY" $DdApiKey
    }
    Set-EnvVar "DD_ENV" $DdEnv
    Set-EnvVar "DD_SERVICE" "datadog-maui-api"
    Set-EnvVar "DD_VERSION" "1.0.0"
    Set-EnvVar "ASPNETCORE_ENVIRONMENT" "Production"
    Set-EnvVar "CORECLR_ENABLE_PROFILING" "1"
    Set-EnvVar "CORECLR_PROFILER" "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"

    $webConfig.Save($webConfigPath)
    Write-Host "✓ Datadog configuration updated in web.config" -ForegroundColor Green
} else {
    Write-Warning "Warning: web.config not found. Datadog automatic instrumentation may not work."
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Website URL: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "Physical Path: $PhysicalPath" -ForegroundColor Cyan
Write-Host "App Pool: $AppPoolName" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test the deployment:" -ForegroundColor Yellow
Write-Host "  Invoke-RestMethod http://localhost:$Port/health" -ForegroundColor Gray
Write-Host ""

# Start the website if it's not running
$site = Get-Website -Name $SiteName
if ($site.State -ne "Started") {
    Write-Host "Starting website..." -ForegroundColor Yellow
    Start-Website -Name $SiteName
    Write-Host "✓ Website started" -ForegroundColor Green
}

# Test the endpoint
Write-Host "Testing endpoint..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
try {
    $response = Invoke-RestMethod "http://localhost:$Port/health" -TimeoutSec 5
    Write-Host "✓ Health check passed: $($response.status)" -ForegroundColor Green
} catch {
    Write-Warning "Warning: Health check failed. Check IIS logs for details."
    Write-Host "Troubleshooting:" -ForegroundColor Yellow
    Write-Host "  1. Check Event Viewer > Windows Logs > Application" -ForegroundColor Gray
    Write-Host "  2. Check IIS logs in C:\inetpub\logs\LogFiles" -ForegroundColor Gray
    Write-Host "  3. Verify .NET Core Hosting Bundle is installed" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Cleanup: Remove publish directory" -ForegroundColor Yellow
Remove-Item -Path "publish/core" -Recurse -Force -ErrorAction SilentlyContinue
