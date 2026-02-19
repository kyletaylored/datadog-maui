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

# Optional: reduce encoding weirdness in console output
try {
    [Console]::OutputEncoding = [System.Text.Encoding]::UTF8
    $OutputEncoding = [System.Text.Encoding]::UTF8
} catch { }

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

$enabled = 0
$skipped = 0
$failed = 0

foreach ($feature in $features) {
    try {
        $state = Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction SilentlyContinue

        if ($null -eq $state) {
            Write-Host ("  [SKIP] {0} (not available)" -f $feature) -ForegroundColor DarkGray
            $skipped++
            continue
        }

        if ($state.State -eq "Enabled") {
            Write-Host ("  [OK]   {0} (already enabled)" -f $feature) -ForegroundColor Green
            $enabled++
        }
        else {
            Write-Host ("  [DO]   Enabling {0}..." -f $feature) -ForegroundColor Yellow
            Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart -WarningAction SilentlyContinue | Out-Null

            # Re-check status (more reliable than assuming success)
            $state2 = Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction SilentlyContinue
            if ($state2 -and ($state2.State -eq "Enabled" -or $state2.State -eq "EnablePending")) {
                Write-Host ("  [OK]   {0} (enabled)" -f $feature) -ForegroundColor Green
                $enabled++
            } else {
                $stateText = if ($state2 -and $state2.State) { $state2.State } else { "Unknown" }
                Write-Host ("  [FAIL] {0} (state: {1})" -f $feature, $stateText) -ForegroundColor Red
                $failed++
            }
        }
    }
    catch {
        Write-Host ("  [FAIL] {0} ({1})" -f $feature, $_.Exception.Message) -ForegroundColor Red
        $failed++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Installation Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Enabled/Already Enabled: $enabled" -ForegroundColor Green
Write-Host "Skipped: $skipped" -ForegroundColor DarkGray
Write-Host "Failed: $failed" -ForegroundColor $(if ($failed -gt 0) { "Red" } else { "Green" })
Write-Host ""

if ($failed -gt 0) {
    Write-Warning "Some features failed to install. Check the output above for details."
}

# Verify IIS is installed
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Host "[OK] IIS WebAdministration module loaded successfully" -ForegroundColor Green
} catch {
    Write-Error "Failed to load IIS WebAdministration module. IIS may not be properly installed."
    exit 1
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Install .NET Core Hosting Bundle:" -ForegroundColor White
Write-Host "   https://dotnet.microsoft.com/download/dotnet/9.0" -ForegroundColor DarkGray
Write-Host ""
Write-Host "2. Install Datadog .NET Tracer (for automatic instrumentation):" -ForegroundColor White
Write-Host "   https://github.com/DataDog/dd-trace-dotnet/releases" -ForegroundColor DarkGray
Write-Host ""
Write-Host "3. Restart IIS:" -ForegroundColor White
Write-Host "   iisreset" -ForegroundColor DarkGray
Write-Host ""
Write-Host "4. Deploy your application:" -ForegroundColor White
Write-Host "   .\scripts\deploy-iis-core.ps1" -ForegroundColor DarkGray
Write-Host "   .\scripts\deploy-iis-framework.ps1" -ForegroundColor DarkGray
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
