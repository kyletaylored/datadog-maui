# Configure IIS Express applicationHost.config for Datadog profiling
# Run this script to add Datadog environment variables to your IIS Express config

$ErrorActionPreference = "Stop"

# Find IIS Express config
$configPath = "$env:USERPROFILE\Documents\IISExpress\config\applicationHost.config"

if (-not (Test-Path $configPath)) {
    Write-Error "IIS Express config not found at $configPath"
    Write-Host "Please run the project at least once from Visual Studio to create the config."
    exit 1
}

Write-Host "Found IIS Express config: $configPath"
Write-Host "Creating backup..."

# Backup the config
$backupPath = "$configPath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
Copy-Item $configPath $backupPath
Write-Host "Backup created: $backupPath"

# Load the XML
[xml]$config = Get-Content $configPath

# Define Datadog environment variables
$ddVars = @{
    # .NET Framework variables
    "COR_ENABLE_PROFILING" = "1"
    "COR_PROFILER" = "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"
    "COR_PROFILER_PATH_32" = "C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll"
    "COR_PROFILER_PATH_64" = "C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll"
    # .NET Core variables
    "CORECLR_ENABLE_PROFILING" = "1"
    "CORECLR_PROFILER" = "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"
    "CORECLR_PROFILER_PATH_64" = "C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll"
    "CORECLR_PROFILER_PATH_32" = "C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll"
    # Common
    "DD_DOTNET_TRACER_HOME" = "C:\Program Files\Datadog\.NET Tracer"
}

Write-Host ""
Write-Host "Setting environment variables in multiple locations for maximum compatibility..."
Write-Host ""

# Location 1: Application Pool Defaults
Write-Host "1. Configuring Application Pool Defaults..."
$applicationPoolDefaults = $config.configuration.'system.applicationHost'.applicationPools.applicationPoolDefaults
if ($null -eq $applicationPoolDefaults) {
    Write-Warning "Could not find applicationPoolDefaults section"
} else {
    # Find or create environmentVariables in applicationPoolDefaults
    $appPoolEnvVars = $applicationPoolDefaults.SelectSingleNode("environmentVariables")
    if ($null -eq $appPoolEnvVars) {
        $appPoolEnvVars = $config.CreateElement("environmentVariables")
        $applicationPoolDefaults.AppendChild($appPoolEnvVars) | Out-Null
    }

    foreach ($key in $ddVars.Keys) {
        $existing = $appPoolEnvVars.SelectSingleNode("add[@name='$key']")
        if ($null -ne $existing) {
            $existing.SetAttribute("value", $ddVars[$key])
            Write-Host "  Updated $key in app pool defaults"
        } else {
            $addElement = $config.CreateElement("add")
            $addElement.SetAttribute("name", $key)
            $addElement.SetAttribute("value", $ddVars[$key])
            $appPoolEnvVars.AppendChild($addElement) | Out-Null
            Write-Host "  Added $key to app pool defaults"
        }
    }
}

# Location 2: system.webServer (global)
Write-Host ""
Write-Host "2. Configuring system.webServer (global)..."
$systemWebServer = $config.configuration.'system.webServer'
if ($null -eq $systemWebServer) {
    Write-Warning "Could not find system.webServer section"
} else {
    $globalEnvVars = $systemWebServer.SelectSingleNode("environmentVariables")
    if ($null -eq $globalEnvVars) {
        $globalEnvVars = $config.CreateElement("environmentVariables")
        $systemWebServer.AppendChild($globalEnvVars) | Out-Null
    }

    foreach ($key in $ddVars.Keys) {
        $existing = $globalEnvVars.SelectSingleNode("add[@name='$key']")
        if ($null -ne $existing) {
            $existing.SetAttribute("value", $ddVars[$key])
            Write-Host "  Updated $key in system.webServer"
        } else {
            $addElement = $config.CreateElement("add")
            $addElement.SetAttribute("name", $key)
            $addElement.SetAttribute("value", $ddVars[$key])
            $globalEnvVars.AppendChild($addElement) | Out-Null
            Write-Host "  Added $key to system.webServer"
        }
    }
}

# Save the config
$config.Save($configPath)

Write-Host ""
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "IIS Express configured for Datadog!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Environment variables have been set in:"
Write-Host "  - Application Pool Defaults (primary location)"
Write-Host "  - system.webServer (fallback location)"
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Close Visual Studio COMPLETELY (not just the solution)"
Write-Host "2. Wait 5 seconds"
Write-Host "3. Reopen Visual Studio"
Write-Host "4. Build and run the project (F5)"
Write-Host "5. Verify with: dd-dotnet check process <PID>"
Write-Host ""
Write-Host "If COR_ENABLE_PROFILING is still not set:"
Write-Host "  - Try setting it as a SYSTEM environment variable"
Write-Host "  - Or use the global MSI installer option during Datadog .NET Tracer installation"
Write-Host ""
Write-Host "Backup saved to: $backupPath" -ForegroundColor Cyan
Write-Host ""
