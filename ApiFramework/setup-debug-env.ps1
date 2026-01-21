#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Configures IIS Express debug environment variables for Datadog APM
.DESCRIPTION
    Updates the .csproj.user file to include Datadog environment variables
    that will be automatically applied when debugging with IIS Express in Visual Studio.
.NOTES
    This modifies your local .csproj.user file which is gitignored.
    Run this script once per developer workstation.
#>

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Datadog IIS Express Debug Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectDir = $PSScriptRoot
$userFilePath = Join-Path $projectDir "DatadogMauiApi.Framework.csproj.user"

# Check if Datadog tracer is installed
$tracerPath = "C:\Program Files\Datadog\.NET Tracer"
if (-not (Test-Path $tracerPath)) {
    Write-Warning "Datadog .NET Tracer not found at: $tracerPath"
    Write-Host ""
    Write-Host "Please install the Datadog .NET Tracer MSI first:" -ForegroundColor Yellow
    Write-Host "  https://github.com/DataDog/dd-trace-dotnet/releases/latest" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host "[OK] Datadog .NET Tracer found at: $tracerPath" -ForegroundColor Green

# Define environment variables (semicolon-separated)
$envVars = @(
    "COR_ENABLE_PROFILING=1",
    "COR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}",
    "COR_PROFILER_PATH_32=C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll",
    "COR_PROFILER_PATH_64=C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll",
    "CORECLR_ENABLE_PROFILING=1",
    "CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}",
    "CORECLR_PROFILER_PATH_32=C:\Program Files\Datadog\.NET Tracer\win-x86\Datadog.Trace.ClrProfiler.Native.dll",
    "CORECLR_PROFILER_PATH_64=C:\Program Files\Datadog\.NET Tracer\win-x64\Datadog.Trace.ClrProfiler.Native.dll",
    "DD_DOTNET_TRACER_HOME=C:\Program Files\Datadog\.NET Tracer",
    "DD_SERVICE=datadog-maui-api-framework",
    "DD_ENV=local",
    "DD_VERSION=1.0.0"
)
$envVarsString = $envVars -join ";"

# Create or update .csproj.user file
$userFileContent = @"
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <LastActiveSolutionConfig>Debug|Any CPU</LastActiveSolutionConfig>
  </PropertyGroup>
  <PropertyGroup Condition="'`$(Configuration)|`$(Platform)' == 'Debug|AnyCPU'">
    <EnvironmentVariables>$envVarsString</EnvironmentVariables>
  </PropertyGroup>
</Project>
"@

try {
    # Backup existing file if it exists
    if (Test-Path $userFilePath) {
        $backupPath = "$userFilePath.backup"
        Copy-Item $userFilePath $backupPath -Force
        Write-Host "[OK] Backed up existing .csproj.user file" -ForegroundColor Gray
    }

    # Write new content
    $userFileContent | Out-File -FilePath $userFilePath -Encoding UTF8 -Force
    Write-Host "[OK] Updated .csproj.user with Datadog environment variables" -ForegroundColor Green

    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Setup Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "1. Open the project in Visual Studio" -ForegroundColor White
    Write-Host "2. Press F5 to debug with IIS Express" -ForegroundColor White
    Write-Host "3. Verify environment variables are set:" -ForegroundColor White
    Write-Host "   - Get process ID from Task Manager (iisexpress.exe)" -ForegroundColor Gray
    Write-Host "   - Run: dd-dotnet check process [PID]" -ForegroundColor Gray
    Write-Host ""
    Write-Host "The environment variables will be automatically applied when debugging." -ForegroundColor Cyan
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "ERROR: Failed to update .csproj.user file" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    exit 1
}
