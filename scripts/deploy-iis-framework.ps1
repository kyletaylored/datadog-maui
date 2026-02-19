#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Deploys the .NET Framework 4.8 API to IIS
.DESCRIPTION
    Builds the .NET Framework API and deploys it to IIS with proper configuration
.NOTES
    IMPORTANT: If running on a remote Windows machine via RDP, copy this script and the
    entire project folder to a local drive (e.g., C:\temp\datadog-maui) before running.
    Running PowerShell scripts from \\tsclient\ network shares can cause parsing errors.
.PARAMETER SiteName
    Name of the IIS website (default: DatadogMauiApiFramework)
.PARAMETER AppPoolName
    Name of the IIS application pool (default: DatadogMauiApiFrameworkPool)
.PARAMETER Port
    Port number for the website (default: 5001)
.PARAMETER PhysicalPath
    Physical path where the application will be deployed (default: C:\inetpub\wwwroot\datadog-maui-api-framework)
.PARAMETER DdApiKey
    Datadog API key (optional, will prompt if not provided)
.PARAMETER DdEnv
    Datadog environment tag (default: production)
.EXAMPLE
    .\deploy-iis-framework.ps1 -DdApiKey "your-api-key"
#>

param(
    [string]$SiteName = "DatadogMauiApiFramework",
    [string]$AppPoolName = "DatadogMauiApiFrameworkPool",
    [int]$Port = 5001,
    [string]$PhysicalPath = "C:\inetpub\wwwroot\datadog-maui-api-framework",
    [string]$DdApiKey = "",
    [string]$DdEnv = "dev"
)

$ErrorActionPreference = "Stop"

# Helper function to stop any process using the specified port (to free it up for IIS)
function Stop-ProcessUsingPort {
    param([Parameter(Mandatory)][int]$Port)

    $conns = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if (-not $conns) { return }

    $procIds = $conns | Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($procId in $procIds) {
        try {
            $p = Get-Process -Id $procId -ErrorAction Stop
            Write-Host "Port $Port is in use by PID $procId ($($p.ProcessName)). Stopping it..." -ForegroundColor Yellow
            Stop-Process -Id $procId -Force -ErrorAction Stop
        } catch {
            Write-Warning "Couldn't stop PID $procId using port ${Port}: $($_.Exception.Message)"
        }
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Datadog MAUI API (.NET Framework 4.8)" -ForegroundColor Cyan
Write-Host "IIS Deployment Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify we're in the correct directory
if (-not (Test-Path "ApiFramework/DatadogMauiApi.Framework.csproj")) {
    Write-Error "Error: Must run this script from the repository root directory"
    exit 1
}

# Verify IIS is installed
if (-not (Get-Command "Get-Website" -ErrorAction SilentlyContinue)) {
    Write-Error "Error: IIS is not installed. Run 'scripts/install-iis.ps1' first"
    exit 1
}

# Verify .NET Framework 4.8 is installed
$netFramework = Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\' -ErrorAction SilentlyContinue |
    Get-ItemProperty | Where-Object { $_.Release -ge 528040 }

if (-not $netFramework) {
    Write-Error "Error: .NET Framework 4.8 or higher is not installed"
    Write-Host "Download from: https://dotnet.microsoft.com/download/dotnet-framework/net48"
    exit 1
}

# Check for MSBuild
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -requires Microsoft.Component.MSBuild `
    -latest `
    -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1

if (-not $msbuild) {
    Write-Error "Error: MSBuild not found. Install Visual Studio or Build Tools"
    Write-Host "Download from: https://visualstudio.microsoft.com/downloads/"
    exit 1
}

# Check for NuGet
$nuget = Get-Command "nuget.exe" -ErrorAction SilentlyContinue
if (-not $nuget) {
    Write-Warning "Warning: NuGet.exe not found in PATH"
    Write-Host "Attempting to download NuGet.exe..." -ForegroundColor Yellow
    $nugetPath = Join-Path $PSScriptRoot "nuget.exe"
    Invoke-WebRequest "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetPath
    $nuget = Get-Command $nugetPath
}

# Prompt for Datadog API key if not provided
# if ([string]::IsNullOrEmpty($DdApiKey)) {
#     $DdApiKey = Read-Host "Enter Datadog API key (or press Enter to skip)"
# }

Write-Host "[1/8] Restoring NuGet packages..." -ForegroundColor Yellow
Push-Location ApiFramework
try {
    & $nuget.Source restore -NonInteractive
    if ($LASTEXITCODE -ne 0) {
        throw "NuGet restore failed"
    }
} finally {
    Pop-Location
}
Write-Host "[OK] NuGet restore complete" -ForegroundColor Green

Write-Host "[2/8] Building application..." -ForegroundColor Yellow
Push-Location ApiFramework
try {
    $project = ".\DatadogMauiApi.Framework.csproj"

    # Ask MSBuild where OutputPath is for Release
    $outputPath = & $msbuild $project /nologo /v:q /t:GetTargetPath /p:Configuration=Release `
        /p:CustomAfterMicrosoftCommonTargets="" 2>$null |
        Select-String -Pattern '->\s*(.+)$' |
        ForEach-Object { $_.Matches[0].Groups[1].Value } |
        Select-Object -First 1

    # Build Release
    & $msbuild $project /p:Configuration=Release /verbosity:minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }

    # Fallback: if GetTargetPath parsing didn't work, infer from the built DLL line pattern you saw
    if (-not $outputPath) {
        $dll = Get-ChildItem -Path ".\bin" -Filter "*.dll" -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($dll) { $outputPath = $dll.FullName }
    }

    if (-not $outputPath) {
        throw "Could not determine build output path."
    }

    $script:BuildOutputDir = Split-Path -Parent $outputPath
}
finally {
    Pop-Location
}

Write-Host "[OK] Build complete" -ForegroundColor Green
Write-Host "Build output directory: $BuildOutputDir" -ForegroundColor Gray

# Optional: List built files for debugging
Get-ChildItem ApiFramework -Directory -Recurse -Depth 3 |
  Where-Object FullName -match '\\bin\\Release|\\obj\\Release' |
  Select-Object -ExpandProperty FullName   

Write-Host "[3/8] Creating physical directory..." -ForegroundColor Yellow
if (-not (Test-Path $PhysicalPath)) {
    New-Item -ItemType Directory -Path $PhysicalPath -Force | Out-Null
}

Write-Host "[4/8] Copying files to deployment directory..." -ForegroundColor Yellow

# Prefer a staged web output if it exists; otherwise use the build output folder
$sourceCandidates = @()

# Common web packaging folder (full site content)
$packageTmp = "ApiFramework\obj\Release\Package\PackageTmp"
if (Test-Path $packageTmp) { $sourceCandidates += $packageTmp }

# The MSBuild-derived output folder (your case is ApiFramework\bin)
if ($BuildOutputDir -and (Test-Path $BuildOutputDir)) { $sourceCandidates += $BuildOutputDir }

# Last resort
if (Test-Path "ApiFramework\bin") { $sourceCandidates += "ApiFramework\bin" }

$sourcePath = $sourceCandidates | Select-Object -First 1

if (-not $sourcePath -or -not (Test-Path $sourcePath)) {
    Write-Error "Error: Build output not found. Checked: $($sourceCandidates -join ', ')"
    exit 1
}

Copy-Item -Path (Join-Path $sourcePath '*') -Destination $PhysicalPath -Recurse -Force
Write-Host "[OK] Files copied from $sourcePath" -ForegroundColor Green

Write-Host "[5/8] Configuring Application Pool..." -ForegroundColor Yellow
# Remove existing app pool if it exists
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Write-Host "  Removing existing app pool..." -ForegroundColor Gray
    Remove-WebAppPool -Name $AppPoolName
}

# Create new app pool for .NET Framework 4.8
New-WebAppPool -Name $AppPoolName | Out-Null
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "managedPipelineMode" -Value "Integrated"
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
Set-ItemProperty -Path "IIS:\AppPools\$AppPoolName" -Name "enable32BitAppOnWin64" -Value $false
Write-Host "[OK] App pool created: $AppPoolName (.NET Framework v4.0)" -ForegroundColor Green

Write-Host "[6/8] Configuring Website..." -ForegroundColor Yellow
# Remove existing website if it exists
if (Test-Path "IIS:\Sites\$SiteName") {
    Write-Host "  Removing existing website..." -ForegroundColor Gray
    Remove-Website -Name $SiteName
}

$conflicts = Get-WebBinding | Where-Object {
    $parts = $_.bindingInformation -split ':', 3
    $parts.Count -ge 2 -and [int]$parts[1] -eq $Port
}

if ($conflicts) {
    Write-Warning "Another IIS binding is using port ${Port}:"
    $conflicts | ForEach-Object {
        Write-Host "  $($_.ItemXPath) -> $($_.bindingInformation)" -ForegroundColor Gray
    }
}

# Create new website
New-Website -Name $SiteName `
    -Port $Port `
    -PhysicalPath $PhysicalPath `
    -ApplicationPool $AppPoolName `
    -Force | Out-Null

# Correct place: root vdir of the root application
$filter = "/system.applicationHost/sites/site[@name='$SiteName']/application[@path='/']/virtualDirectory[@path='/']"

# Force the physical path (authoritative)
Set-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST" `
    -Filter $filter `
    -Name "physicalPath" `
    -Value $PhysicalPath

# Read it back (authoritative)
$actualPath = (Get-WebConfigurationProperty -PSPath "MACHINE/WEBROOT/APPHOST" `
    -Filter $filter `
    -Name "physicalPath").Value

Write-Host "[INFO] IIS PhysicalPath is: $actualPath" -ForegroundColor Cyan

if ([string]::IsNullOrWhiteSpace($actualPath) -or ($actualPath -ne $PhysicalPath)) {
    throw "IIS PhysicalPath mismatch. Expected '$PhysicalPath' but got '$actualPath'"
}

Write-Host "[OK] Website created: $SiteName" -ForegroundColor Green

Write-Host "[7/8] Configuring permissions..." -ForegroundColor Yellow
$acl = Get-Acl $PhysicalPath
$appPoolIdentity = "IIS AppPool\$AppPoolName"
$permission = $appPoolIdentity, "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $PhysicalPath $acl
Write-Host "[OK] Permissions configured" -ForegroundColor Green

Write-Host "[8/8] Configuring Datadog environment..." -ForegroundColor Yellow
$webConfigPath = Join-Path $PhysicalPath "Web.config"
if (Test-Path $webConfigPath) {
    [xml]$webConfig = Get-Content $webConfigPath

    # Update appSettings
    $appSettings = $webConfig.configuration.appSettings

    # Helper function to set app setting
    function Set-AppSetting($key, $value) {
        $setting = $appSettings.add | Where-Object { $_.key -eq $key }
        if ($setting) {
            $setting.value = $value
        } else {
            $newSetting = $webConfig.CreateElement("add")
            $newSetting.SetAttribute("key", $key)
            $newSetting.SetAttribute("value", $value)
            $appSettings.AppendChild($newSetting) | Out-Null
        }
    }

    # Set Datadog configuration
    # if (-not [string]::IsNullOrEmpty($DdApiKey)) {
    #     Set-AppSetting "DD_API_KEY" $DdApiKey
    # }
    Set-AppSetting "DD_ENV" $DdEnv
    Set-AppSetting "DD_SERVICE" "datadog-maui-api-framework"
    Set-AppSetting "DD_VERSION" "1.0.0"
    Set-AppSetting "DD_TRACE_ENABLED" "true"

    # Add CLR Profiler settings for automatic instrumentation
    Set-AppSetting "COR_ENABLE_PROFILING" "1"
    Set-AppSetting "COR_PROFILER" "{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"

    $webConfig.Save($webConfigPath)
    Write-Host "[OK] Datadog configuration updated in Web.config" -ForegroundColor Green
} else {
    Write-Warning "Warning: Web.config not found. Datadog automatic instrumentation may not work."
}

# Also set environment variables on the Application Pool for profiler
Write-Host "  Setting CLR Profiler environment variables on App Pool..." -ForegroundColor Gray
$appPoolPath = "IIS:\AppPools\$AppPoolName"

# Create environment variables collection if it doesn't exist
$envVars = @(
    @{ name = 'COR_ENABLE_PROFILING'; value = '1' }
    @{ name = 'COR_PROFILER'; value = '{846F5F1C-F9AE-4B07-969E-05C26BC060D8}' }
    @{ name = 'DD_DOTNET_TRACER_HOME'; value = '%ProgramFiles%\Datadog\.NET Tracer\' + '' }
)

foreach ($envVar in $envVars) {
    # Note: Setting env vars on app pool requires IIS 10+ and specific configuration
    # This is typically done via IIS Manager or appcmd.exe
    Write-Host "    - $($envVar.name)=$($envVar.value)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "NOTE: For automatic instrumentation to work, ensure:" -ForegroundColor Yellow
Write-Host "  1. Datadog .NET Tracer MSI is installed" -ForegroundColor Gray
Write-Host "  2. Environment variables are set system-wide or in IIS App Pool" -ForegroundColor Gray
Write-Host "  3. IIS is restarted after installing the tracer" -ForegroundColor Gray
Write-Host ""

Write-Host "========================================" -ForegroundColor Green
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Website URL: http://localhost:$Port" -ForegroundColor Cyan
Write-Host "Physical Path: $PhysicalPath" -ForegroundColor Cyan
Write-Host "App Pool: $AppPoolName (.NET Framework 4.8)" -ForegroundColor Cyan
Write-Host ""
Write-Host "Test the deployment:" -ForegroundColor Yellow
Write-Host "  Invoke-RestMethod http://localhost:$Port/health" -ForegroundColor Gray
Write-Host ""

# Start the website if it's not running
$site = Get-Website -Name $SiteName
if ($site.State -ne "Started") {
    Write-Host "Starting website..." -ForegroundColor Yellow

    # Make sure nothing else is listening on the port
    Stop-ProcessUsingPort -Port $Port

    # Restart WAS/W3SVC to ensure bindings are refreshed (helps after remove/create)
    Restart-Service WAS -Force -ErrorAction SilentlyContinue
    Restart-Service W3SVC -Force -ErrorAction SilentlyContinue

    Start-Website -Name $SiteName
    Write-Host "[OK] Website started" -ForegroundColor Green
}

# Test the endpoint
Write-Host "Testing endpoint..." -ForegroundColor Yellow
Start-Sleep -Seconds 3
try {
    $response = Invoke-RestMethod "http://localhost:$Port/health" -TimeoutSec 5
    Write-Host '[OK] Health check passed: ' + $($response.status) -ForegroundColor Green
} catch {
    Write-Warning 'Warning: Health check failed. Check IIS logs for details.'
    Write-Host 'Troubleshooting:' -ForegroundColor Yellow
    Write-Host '  1. Check Event Viewer > Windows Logs > Application' -ForegroundColor Gray
    Write-Host '  2. Check IIS logs in C:\inetpub\logs\LogFiles' -ForegroundColor Gray
    Write-Host '  3. Verify .NET Framework 4.8 is installed' -ForegroundColor Gray
    Write-Host '  4. Check Web.config for errors' -ForegroundColor Gray
    Write-Host '  5. Ensure NuGet packages were restored correctly' -ForegroundColor Gray
}

Write-Host ''
Write-Host 'Next Steps:' -ForegroundColor Yellow
Write-Host '  1. Test authentication: POST http://localhost:$Port/auth/login' -ForegroundColor Gray
Write-Host '  2. Verify Datadog traces in APM' -ForegroundColor Gray
Write-Host '  3. Check Datadog metrics and logs' -ForegroundColor Gray
