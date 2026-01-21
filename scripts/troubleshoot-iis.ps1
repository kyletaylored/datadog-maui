#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Comprehensive IIS troubleshooting and fix script for Datadog MAUI Framework API
.DESCRIPTION
    Diagnoses and fixes common IIS deployment issues including:
    - Incorrect physical paths
    - File permissions issues
    - Missing files
    - Application pool configuration
.PARAMETER SiteName
    Name of the IIS website (default: DatadogMauiApiFramework)
.PARAMETER AppPoolName
    Name of the IIS application pool (default: DatadogMauiApiFrameworkPool)
.PARAMETER PhysicalPath
    Expected physical path of the application (default: auto-detect)
.PARAMETER FixAll
    Automatically fix all detected issues without prompting
.EXAMPLE
    .\troubleshoot-iis.ps1
    Interactive mode - prompts before fixing each issue
.EXAMPLE
    .\troubleshoot-iis.ps1 -FixAll
    Automatic mode - fixes all issues without prompting
#>

param(
    [string]$SiteName = "DatadogMauiApiFramework",
    [string]$AppPoolName = "DatadogMauiApiFrameworkPool",
    [string]$PhysicalPath = "",
    [switch]$FixAll
)

$ErrorActionPreference = "Stop"

# Auto-detect physical path if not provided
if ([string]::IsNullOrEmpty($PhysicalPath)) {
    $scriptDir = Split-Path -Parent $PSScriptRoot
    $PhysicalPath = Join-Path $scriptDir "ApiFramework"
}

$issuesFound = 0
$issuesFixed = 0

function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $Text -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Write-Issue {
    param([string]$Text)
    $script:issuesFound++
    Write-Host "[ISSUE $script:issuesFound] $Text" -ForegroundColor Red
}

function Write-Fix {
    param([string]$Text)
    $script:issuesFixed++
    Write-Host "[FIXED] $Text" -ForegroundColor Green
}

function Confirm-Fix {
    param([string]$Message)
    if ($FixAll) {
        return $true
    }
    Write-Host "$Message (Y/N)" -ForegroundColor Yellow
    $response = Read-Host
    return ($response -eq "Y" -or $response -eq "y")
}

Write-Header "IIS Troubleshooting Tool"
Write-Host "Site Name: $SiteName" -ForegroundColor White
Write-Host "App Pool: $AppPoolName" -ForegroundColor White
Write-Host "Expected Path: $PhysicalPath" -ForegroundColor White
Write-Host ""

# Check 1: Verify WebAdministration module
Write-Host "[1/7] Checking IIS Management tools..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Host "  OK: WebAdministration module loaded" -ForegroundColor Green
} catch {
    Write-Issue "WebAdministration module not available"
    Write-Host "  Please install IIS Management tools" -ForegroundColor Red
    exit 1
}

# Check 2: Verify site exists
Write-Host "[2/7] Checking if site exists..." -ForegroundColor Yellow
$site = Get-Website -Name $SiteName -ErrorAction SilentlyContinue

if (-not $site) {
    Write-Issue "Site '$SiteName' not found in IIS"
    Write-Host "  Available sites:" -ForegroundColor Yellow
    Get-Website | Format-Table Name, PhysicalPath, State -AutoSize
    Write-Host ""
    Write-Host "  Run the deployment script to create the site:" -ForegroundColor Cyan
    Write-Host "  .\scripts\deploy-iis-framework.ps1" -ForegroundColor White
    exit 1
}
Write-Host "  OK: Site exists" -ForegroundColor Green

# Check 3: Verify physical path
Write-Host "[3/7] Checking physical path..." -ForegroundColor Yellow
$currentPath = $site.PhysicalPath
Write-Host "  Current: $currentPath" -ForegroundColor White

if ($currentPath -like "*\bin") {
    Write-Issue "Physical path incorrectly points to bin folder"
    $correctPath = $currentPath -replace '\\bin$', ''
    Write-Host "  Should be: $correctPath" -ForegroundColor Yellow

    if (Confirm-Fix "Fix physical path?") {
        Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $correctPath
        $currentPath = $correctPath
        Write-Fix "Physical path corrected"
    }
} elseif ($currentPath -ne $PhysicalPath) {
    Write-Host "  WARNING: Path differs from expected" -ForegroundColor Yellow
    Write-Host "    Expected: $PhysicalPath" -ForegroundColor Gray
    Write-Host "    Actual:   $currentPath" -ForegroundColor Gray

    if (Confirm-Fix "Update to expected path?") {
        Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $PhysicalPath
        $currentPath = $PhysicalPath
        Write-Fix "Physical path updated"
    }
} else {
    Write-Host "  OK: Physical path is correct" -ForegroundColor Green
}

# Check 4: Verify path exists and has index.html
Write-Host "[4/7] Checking files..." -ForegroundColor Yellow

if (-not (Test-Path $currentPath)) {
    Write-Issue "Physical path does not exist: $currentPath"
    Write-Host "  Please verify the application is deployed" -ForegroundColor Red
    exit 1
}
Write-Host "  OK: Physical path exists" -ForegroundColor Green

$indexPath = Join-Path $currentPath "index.html"
if (-not (Test-Path $indexPath)) {
    Write-Issue "index.html not found at: $indexPath"
    Write-Host "  The site will show a 403 error without a default document" -ForegroundColor Yellow

    $sourceIndex = Join-Path $currentPath "wwwroot\index.html"
    if (Test-Path $sourceIndex) {
        if (Confirm-Fix "Copy index.html from wwwroot?") {
            Copy-Item $sourceIndex $indexPath
            Write-Fix "index.html copied"
        }
    }
} else {
    Write-Host "  OK: index.html exists" -ForegroundColor Green
}

# Check 5: Verify permissions
Write-Host "[5/7] Checking permissions..." -ForegroundColor Yellow

$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
$hasIssues = $false

try {
    $acl = Get-Acl $currentPath
    $appPoolIdentity = "IIS APPPOOL\$AppPoolName"

    $appPoolAccess = $acl.Access | Where-Object { $_.IdentityReference -eq $appPoolIdentity }
    if (-not $appPoolAccess) {
        Write-Issue "App pool identity does not have permissions"
        $hasIssues = $true
    }

    $iisUsersAccess = $acl.Access | Where-Object { $_.IdentityReference -like "*IIS_IUSRS*" }
    if (-not $iisUsersAccess) {
        Write-Issue "IIS_IUSRS group does not have permissions"
        $hasIssues = $true
    }

    if ($hasIssues) {
        if (Confirm-Fix "Fix permissions?") {
            Write-Host "  Granting permissions..." -ForegroundColor Yellow

            # Grant to current user
            icacls "$currentPath" /grant "${currentUser}:(OI)(CI)F" /T /Q | Out-Null

            # Grant to IIS users
            icacls "$currentPath" /grant "IIS_IUSRS:(OI)(CI)RX" /T /Q | Out-Null
            icacls "$currentPath" /grant "IUSR:(OI)(CI)RX" /T /Q | Out-Null
            icacls "$currentPath" /grant "${appPoolIdentity}:(OI)(CI)RX" /T /Q | Out-Null

            # Grant write to bin and App_Data
            $binPath = Join-Path $currentPath "bin"
            $appDataPath = Join-Path $currentPath "App_Data"

            if (Test-Path $binPath) {
                icacls "$binPath" /grant "${appPoolIdentity}:(OI)(CI)M" /T /Q | Out-Null
            }

            if (-not (Test-Path $appDataPath)) {
                New-Item -Path $appDataPath -ItemType Directory -Force | Out-Null
            }
            icacls "$appDataPath" /grant "${appPoolIdentity}:(OI)(CI)M" /T /Q | Out-Null

            Write-Fix "Permissions updated"
        }
    } else {
        Write-Host "  OK: Permissions look good" -ForegroundColor Green
    }
} catch {
    Write-Host "  WARNING: Could not verify permissions: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Check 6: Verify app pool configuration
Write-Host "[6/7] Checking application pool..." -ForegroundColor Yellow

$appPool = Get-Item "IIS:\AppPools\$AppPoolName" -ErrorAction SilentlyContinue
if (-not $appPool) {
    Write-Issue "Application pool '$AppPoolName' not found"
} else {
    $state = $appPool.State
    Write-Host "  State: $state" -ForegroundColor White

    if ($state -ne "Started") {
        if (Confirm-Fix "Start application pool?") {
            Start-WebAppPool -Name $AppPoolName
            Write-Fix "Application pool started"
        }
    } else {
        Write-Host "  OK: Application pool is running" -ForegroundColor Green
    }
}

# Check 7: Verify site state
Write-Host "[7/7] Checking site state..." -ForegroundColor Yellow

$siteState = $site.State
Write-Host "  State: $siteState" -ForegroundColor White

if ($siteState -ne "Started") {
    if (Confirm-Fix "Start website?") {
        Start-Website -Name $SiteName
        Write-Fix "Website started"
    }
} else {
    Write-Host "  OK: Website is running" -ForegroundColor Green
}

# Summary
Write-Header "Summary"

if ($issuesFound -eq 0) {
    Write-Host "No issues detected!" -ForegroundColor Green
    Write-Host "Site should be accessible at: http://localhost:5001/" -ForegroundColor Cyan
} else {
    Write-Host "Found $issuesFound issue(s)" -ForegroundColor Yellow
    Write-Host "Fixed $issuesFixed issue(s)" -ForegroundColor Green

    if ($issuesFixed -gt 0) {
        Write-Host ""
        Write-Host "Restarting IIS to apply changes..." -ForegroundColor Yellow
        if (Confirm-Fix "Restart IIS?") {
            iisreset /restart | Out-Null
            Write-Host "IIS restarted" -ForegroundColor Green
        }
    }

    if ($issuesFixed -lt $issuesFound) {
        Write-Host ""
        Write-Host "Some issues were not fixed" -ForegroundColor Yellow
        Write-Host "Re-run with -FixAll to automatically fix all issues:" -ForegroundColor Cyan
        Write-Host "  .\scripts\troubleshoot-iis.ps1 -FixAll" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "For more troubleshooting help, see:" -ForegroundColor Cyan
Write-Host "  docs\IIS_TROUBLESHOOTING.md" -ForegroundColor White
Write-Host ""
