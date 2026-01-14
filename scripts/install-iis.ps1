#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Installs IIS and required components for hosting .NET applications
.DESCRIPTION
    Enables IIS with ASP.NET support for both .NET Core and .NET Framework applications
.EXAMPLE
    .\install-iis.ps1
#>

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "IIS Installation Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script must be run as Administrator"
    exit 1
}

Write-Host "Installing IIS and required features..." -ForegroundColor Yellow
Write-Host ""

# Core IIS Features
$features = @(
    "IIS-WebServerRole",
    "IIS-WebServer",
    "IIS-CommonHttpFeatures",
    "IIS-HttpErrors",
    "IIS-HttpRedirect",
    "IIS-ApplicationDevelopment",
    "IIS-NetFxExtensibility45",
    "IIS-HealthAndDiagnostics",
    "IIS-HttpLogging",
    "IIS-LoggingLibraries",
    "IIS-RequestMonitor",
    "IIS-HttpTracing",
    "IIS-Security",
    "IIS-RequestFiltering",
    "IIS-Performance",
    "IIS-WebServerManagementTools",
    "IIS-IIS6ManagementCompatibility",
    "IIS-Metabase",
    "IIS-ManagementConsole",
    "IIS-BasicAuthentication",
    "IIS-WindowsAuthentication",
    "IIS-StaticContent",
    "IIS-DefaultDocument",
    "IIS-DirectoryBrowsing",
    "IIS-WebSockets",
    "IIS-ApplicationInit",
    "IIS-ISAPIExtensions",
    "IIS-ISAPIFilter",
    "IIS-HttpCompressionStatic",
    "IIS-ASPNET45"
)

$installed = 0
$skipped = 0
$failed = 0

foreach ($feature in $features) {
    try {
        $state = Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction SilentlyContinue

        if ($null -eq $state) {
            Write-Host "  ⊘ $feature (not available)" -ForegroundColor Gray
            $skipped++
            continue
        }

        if ($state.State -eq "Enabled") {
            Write-Host "  ✓ $feature (already installed)" -ForegroundColor Green
            $installed++
        } else {
            Write-Host "  → Installing $feature..." -ForegroundColor Yellow
            Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart -WarningAction SilentlyContinue | Out-Null
            Write-Host "  ✓ $feature (installed)" -ForegroundColor Green
            $installed++
        }
    } catch {
        Write-Host "  ✗ $feature (failed: $($_.Exception.Message))" -ForegroundColor Red
        $failed++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installation Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installed/Enabled: $installed" -ForegroundColor Green
Write-Host "Skipped: $skipped" -ForegroundColor Gray
Write-Host "Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($failed -gt 0) {
    Write-Warning "Some features failed to install. Check the output above for details."
}

# Verify IIS is installed
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Host "✓ IIS Web Administration module loaded successfully" -ForegroundColor Green
} catch {
    Write-Error "Failed to load IIS Web Administration module. IIS may not be properly installed."
    exit 1
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Install .NET Core Hosting Bundle:" -ForegroundColor White
Write-Host "   https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Install Datadog .NET Tracer (for automatic instrumentation):" -ForegroundColor White
Write-Host "   https://github.com/DataDog/dd-trace-dotnet/releases" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Restart IIS:" -ForegroundColor White
Write-Host "   iisreset" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Deploy your application:" -ForegroundColor White
Write-Host "   .\scripts\deploy-iis-core.ps1" -ForegroundColor Gray
Write-Host "   .\scripts\deploy-iis-framework.ps1" -ForegroundColor Gray
Write-Host ""

# Check if restart is needed
$restartNeeded = Get-WindowsOptionalFeature -Online |
    Where-Object { $_.State -eq "EnablePending" -or $_.State -eq "DisablePending" } |
    Select-Object -First 1

if ($restartNeeded) {
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "RESTART REQUIRED" -ForegroundColor Yellow
    Write-Host "========================================" -ForegroundColor Yellow
    Write-Host "Windows features have been modified and require a restart." -ForegroundColor Yellow
    Write-Host ""
    $restart = Read-Host "Restart now? (y/n)"
    if ($restart -eq "y") {
        Restart-Computer -Force
    }
} else {
    Write-Host "No restart required." -ForegroundColor Green
}
