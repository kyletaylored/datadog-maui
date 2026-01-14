# PowerShell script to install Datadog .NET Tracer on Windows Azure App Service
# This script should be run as part of the deployment process

param(
    [string]$TracerVersion = "3.8.0",
    [string]$InstallPath = "D:\home\site\wwwroot\datadog"
)

Write-Host "🐕 Installing Datadog .NET Tracer for Windows"
Write-Host "   Version: $TracerVersion"
Write-Host "   Path: $InstallPath"
Write-Host ""

# Create installation directory
New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null
Write-Host "✅ Created installation directory"

# Download the tracer
$downloadUrl = "https://github.com/DataDog/dd-trace-dotnet/releases/download/v$TracerVersion/datadog-dotnet-apm-$TracerVersion-x64.msi"
$msiPath = "$env:TEMP\datadog-apm.msi"

Write-Host "📥 Downloading Datadog tracer..."
Write-Host "   URL: $downloadUrl"

try {
    Invoke-WebRequest -Uri $downloadUrl -OutFile $msiPath
    Write-Host "✅ Download complete"
} catch {
    Write-Host "❌ Failed to download tracer: $_"
    exit 1
}

# Extract MSI contents (Azure App Service doesn't allow MSI installation)
Write-Host "📦 Extracting tracer files..."

$extractPath = "$env:TEMP\datadog-extract"
New-Item -ItemType Directory -Force -Path $extractPath | Out-Null

# Use msiexec to extract
Start-Process msiexec.exe -ArgumentList "/a `"$msiPath`" /qn TARGETDIR=`"$extractPath`"" -Wait -NoNewWindow

# Copy tracer files to installation path
$tracerSource = "$extractPath\Datadog .NET Tracer"
if (Test-Path $tracerSource) {
    Copy-Item -Path "$tracerSource\*" -Destination $InstallPath -Recurse -Force
    Write-Host "✅ Tracer files installed"
} else {
    Write-Host "❌ Failed to extract tracer files"
    exit 1
}

# Clean up
Remove-Item $msiPath -Force -ErrorAction SilentlyContinue
Remove-Item $extractPath -Recurse -Force -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "✅ Datadog .NET Tracer installation complete"
Write-Host ""
Write-Host "📋 Environment variables to set in Azure App Service:"
Write-Host "   CORECLR_ENABLE_PROFILING = 1"
Write-Host "   CORECLR_PROFILER = {846F5F1C-F9AE-4B07-969E-05C26BC060D8}"
Write-Host "   CORECLR_PROFILER_PATH = $InstallPath\win-x64\Datadog.Trace.ClrProfiler.Native.dll"
Write-Host "   DD_DOTNET_TRACER_HOME = $InstallPath"
Write-Host "   DD_INTEGRATIONS = $InstallPath\integrations.json"
Write-Host ""
