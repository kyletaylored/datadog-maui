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

# Find or create the environmentVariables section
$systemWebServer = $config.configuration.'system.webServer'
if ($null -eq $systemWebServer) {
    Write-Error "Unable to find system.webServer section in config"
    exit 1
}

# Find the aspNetCore section or create environment variables section
$envVarsSection = $systemWebServer.SelectSingleNode("//environmentVariables")

if ($null -eq $envVarsSection) {
    Write-Host "Creating environmentVariables section..."
    $envVarsSection = $config.CreateElement("environmentVariables")
    $systemWebServer.AppendChild($envVarsSection) | Out-Null
}

# Add Datadog environment variables for both .NET Framework and .NET Core
$ddVars = @{
    # .NET Framework variables
    "COR_ENABLE_PROFILING" = "1"
    "COR_PROFILER" = "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"
    "COR_PROFILER_PATH" = "C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll"
    # .NET Core variables
    "CORECLR_ENABLE_PROFILING" = "1"
    "CORECLR_PROFILER" = "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"
    "CORECLR_PROFILER_PATH_64" = "C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll"
    "CORECLR_PROFILER_PATH_32" = "C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll"
    # Common
    "DD_DOTNET_TRACER_HOME" = "C:\Program Files\Datadog\.NET Tracer"
}

foreach ($key in $ddVars.Keys) {
    # Check if variable already exists
    $existing = $envVarsSection.SelectSingleNode("//add[@name='$key']")

    if ($null -ne $existing) {
        Write-Host "Updating $key..."
        $existing.SetAttribute("value", $ddVars[$key])
    } else {
        Write-Host "Adding $key..."
        $addElement = $config.CreateElement("add")
        $addElement.SetAttribute("name", $key)
        $addElement.SetAttribute("value", $ddVars[$key])
        $envVarsSection.AppendChild($addElement) | Out-Null
    }
}

# Save the config
$config.Save($configPath)

Write-Host ""
Write-Host ""
Write-Host "IIS Express configured for Datadog profiling!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:"
Write-Host "1. Close Visual Studio completely"
Write-Host "2. Reopen Visual Studio"
Write-Host "3. Run the project (F5)"
Write-Host "4. Datadog APM should now work in IIS Express"
Write-Host ""
Write-Host "To restore original config, use: ${backupPath}"
