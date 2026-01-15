#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Diagnoses 503 Service Unavailable errors in IIS
.DESCRIPTION
    Checks application pool state, recent events, and configuration
.PARAMETER SiteName
    Name of the IIS website (default: DatadogMauiApiFramework)
.PARAMETER AppPoolName
    Name of the IIS application pool (default: DatadogMauiApiFrameworkPool)
#>

param(
    [string]$SiteName = "DatadogMauiApiFramework",
    [string]$AppPoolName = "DatadogMauiApiFrameworkPool"
)

Import-Module WebAdministration

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "IIS 503 Error Diagnosis" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check 1: Application Pool State
Write-Host "[1/5] Checking application pool state..." -ForegroundColor Yellow
$appPool = Get-WebAppPoolState -Name $AppPoolName

Write-Host "  Application Pool: $AppPoolName" -ForegroundColor White
Write-Host "  Current State: $($appPool.Value)" -ForegroundColor $(if ($appPool.Value -eq "Started") { "Green" } else { "Red" })

if ($appPool.Value -ne "Started") {
    Write-Host ""
    Write-Host "  ISSUE: Application pool is not running!" -ForegroundColor Red
    Write-Host ""
    Write-Host "  Attempting to start..." -ForegroundColor Yellow

    try {
        Start-WebAppPool -Name $AppPoolName
        Start-Sleep -Seconds 2
        $newState = Get-WebAppPoolState -Name $AppPoolName
        Write-Host "  New State: $($newState.Value)" -ForegroundColor Green

        if ($newState.Value -ne "Started") {
            Write-Host "  WARNING: App pool stopped immediately after starting" -ForegroundColor Red
            Write-Host "  This indicates an application crash. Check Event Viewer below." -ForegroundColor Yellow
        }
    } catch {
        Write-Host "  ERROR: Could not start app pool: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""

# Check 2: Application Pool Configuration
Write-Host "[2/5] Checking application pool configuration..." -ForegroundColor Yellow
$appPoolConfig = Get-Item "IIS:\AppPools\$AppPoolName"

Write-Host "  .NET CLR Version: $($appPoolConfig.managedRuntimeVersion)" -ForegroundColor White
Write-Host "  Pipeline Mode: $($appPoolConfig.managedPipelineMode)" -ForegroundColor White
Write-Host "  Identity: $($appPoolConfig.processModel.identityType)" -ForegroundColor White
Write-Host "  Enable 32-Bit Apps: $($appPoolConfig.enable32BitAppOnWin64)" -ForegroundColor White

if ($appPoolConfig.managedRuntimeVersion -ne "v4.0") {
    Write-Host "  WARNING: .NET CLR version should be v4.0 for .NET Framework 4.8" -ForegroundColor Yellow
}

Write-Host ""

# Check 3: Site State
Write-Host "[3/5] Checking website state..." -ForegroundColor Yellow
$site = Get-Website -Name $SiteName

Write-Host "  Website: $SiteName" -ForegroundColor White
Write-Host "  State: $($site.State)" -ForegroundColor $(if ($site.State -eq "Started") { "Green" } else { "Red" })
Write-Host "  Physical Path: $($site.PhysicalPath)" -ForegroundColor White

if ($site.State -ne "Started") {
    Write-Host "  Starting website..." -ForegroundColor Yellow
    Start-Website -Name $SiteName
    Write-Host "  Website started" -ForegroundColor Green
}

Write-Host ""

# Check 4: Recent Application Errors
Write-Host "[4/5] Checking recent application errors..." -ForegroundColor Yellow

$errors = Get-EventLog -LogName Application -Source "ASP.NET*","IIS*" -EntryType Error -Newest 5 -ErrorAction SilentlyContinue

if ($errors) {
    Write-Host "  Found recent errors:" -ForegroundColor Red
    Write-Host ""

    foreach ($error in $errors) {
        Write-Host "  [$($error.TimeGenerated)] $($error.Source)" -ForegroundColor Yellow
        Write-Host "  $($error.Message.Substring(0, [Math]::Min(200, $error.Message.Length)))..." -ForegroundColor Gray
        Write-Host ""
    }

    Write-Host "  For full details, run:" -ForegroundColor Cyan
    Write-Host "  Get-EventLog -LogName Application -Source 'ASP.NET*','IIS*' -EntryType Error -Newest 10 | Format-List" -ForegroundColor White
} else {
    Write-Host "  No recent application errors found" -ForegroundColor Green
}

Write-Host ""

# Check 5: Web.config Issues
Write-Host "[5/5] Checking Web.config..." -ForegroundColor Yellow
$webConfigPath = Join-Path $site.PhysicalPath "Web.config"

if (Test-Path $webConfigPath) {
    Write-Host "  Web.config exists: $webConfigPath" -ForegroundColor Green

    try {
        [xml]$webConfig = Get-Content $webConfigPath
        Write-Host "  Web.config is valid XML" -ForegroundColor Green
    } catch {
        Write-Host "  ERROR: Web.config has invalid XML" -ForegroundColor Red
        Write-Host "  $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "  WARNING: Web.config not found at $webConfigPath" -ForegroundColor Red
}

Write-Host ""

# Summary and Recommendations
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Recommendations" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$appPoolState = (Get-WebAppPoolState -Name $AppPoolName).Value

if ($appPoolState -ne "Started") {
    Write-Host "ACTION REQUIRED: Application pool is stopped" -ForegroundColor Red
    Write-Host ""
    Write-Host "Common causes:" -ForegroundColor Yellow
    Write-Host "1. Application is crashing on startup" -ForegroundColor White
    Write-Host "2. Missing dependencies (.NET Framework 4.8)" -ForegroundColor White
    Write-Host "3. Permission issues" -ForegroundColor White
    Write-Host "4. Invalid Web.config" -ForegroundColor White
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Check Event Viewer (shown above)" -ForegroundColor White
    Write-Host "2. Verify .NET Framework 4.8 is installed:" -ForegroundColor White
    Write-Host "   Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\' | Get-ItemProperty -Name Version" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Check for missing DLLs in bin folder:" -ForegroundColor White
    Write-Host "   Test-Path '$($site.PhysicalPath)\bin\Datadog.Trace.dll'" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. Run permissions fix:" -ForegroundColor White
    Write-Host "   .\scripts\troubleshoot-iis.ps1 -FixAll" -ForegroundColor Gray
    Write-Host ""
    Write-Host "5. Try accessing a simple endpoint to see detailed error:" -ForegroundColor White
    Write-Host "   Browse to: http://localhost:5005/health" -ForegroundColor Gray
    Write-Host "   (May show more detailed error than 503)" -ForegroundColor Gray
} else {
    Write-Host "Application pool is running" -ForegroundColor Green
    Write-Host ""
    Write-Host "If you're still getting 503 errors:" -ForegroundColor Yellow
    Write-Host "- Clear browser cache and try again" -ForegroundColor White
    Write-Host "- Check the site bindings: Get-Website -Name '$SiteName' | Select-Object -ExpandProperty Bindings" -ForegroundColor White
    Write-Host "- Verify the port is correct in your browser" -ForegroundColor White
}

Write-Host ""
Write-Host "Would you like to view the most recent IIS error log? (Y/N)" -ForegroundColor Yellow
$response = Read-Host

if ($response -eq "Y" -or $response -eq "y") {
    Write-Host ""
    Write-Host "Recent IIS Errors:" -ForegroundColor Cyan
    Write-Host "==================" -ForegroundColor Cyan
    Get-EventLog -LogName Application -Source "ASP.NET*","IIS*","Windows Server*" -EntryType Error,Warning -Newest 10 | Format-List TimeGenerated, Source, Message
}
