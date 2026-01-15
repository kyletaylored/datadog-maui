#Requires -RunAsAdministrator

<#
.SYNOPSIS
    Finds what process is using a specific port
.DESCRIPTION
    Uses netstat to identify which process is binding to a given port
.PARAMETER Port
    The port number to check (default: 5001)
.EXAMPLE
    .\find-port-usage.ps1 -Port 5001
#>

param(
    [int]$Port = 5001
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Port Usage Check: $Port" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Find processes using the port
Write-Host "Checking port $Port..." -ForegroundColor Yellow
$connections = netstat -ano | Select-String ":$Port "

if (-not $connections) {
    Write-Host "Port $Port is not in use" -ForegroundColor Green
    exit 0
}

Write-Host "Found processes using port ${Port}:" -ForegroundColor Yellow
Write-Host ""

$pids = @()

foreach ($line in $connections) {
    $lineStr = $line.ToString().Trim()
    $parts = $lineStr -split '\s+' | Where-Object { $_ -ne '' }

    # Extract PID (last column)
    $pid = $parts[-1]

    if ($pid -match '^\d+$' -and $pids -notcontains $pid) {
        $pids += $pid

        try {
            $process = Get-Process -Id $pid -ErrorAction Stop
            Write-Host "PID: $pid" -ForegroundColor White
            Write-Host "  Name: $($process.ProcessName)" -ForegroundColor Cyan
            Write-Host "  Path: $($process.Path)" -ForegroundColor Gray
            Write-Host "  Description: $($process.Description)" -ForegroundColor Gray
            Write-Host ""
        } catch {
            Write-Host "PID: $pid (System process)" -ForegroundColor White
            Write-Host ""
        }
    }
}

Write-Host ""
Write-Host "Solutions:" -ForegroundColor Cyan
Write-Host ""

# Check if it's IIS-related
$iisProcesses = $pids | ForEach-Object {
    try {
        $proc = Get-Process -Id $_ -ErrorAction Stop
        if ($proc.ProcessName -match 'w3wp|iisexpress') {
            $_
        }
    } catch {}
}

if ($iisProcesses) {
    Write-Host "IIS/IIS Express processes detected:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Option 1: Stop and restart IIS" -ForegroundColor White
    Write-Host "  iisreset /stop" -ForegroundColor Gray
    Write-Host "  iisreset /start" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Option 2: Kill specific IIS worker processes" -ForegroundColor White
    foreach ($pid in $iisProcesses) {
        Write-Host "  Stop-Process -Id $pid -Force" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "Would you like to restart IIS now? (Y/N)" -ForegroundColor Yellow
    $response = Read-Host

    if ($response -eq "Y" -or $response -eq "y") {
        Write-Host "Stopping IIS..." -ForegroundColor Yellow
        iisreset /stop
        Start-Sleep -Seconds 2
        Write-Host "Starting IIS..." -ForegroundColor Yellow
        iisreset /start
        Write-Host "IIS restarted successfully" -ForegroundColor Green
    }
} else {
    Write-Host "Non-IIS processes detected:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Option 1: Kill the processes manually" -ForegroundColor White
    foreach ($pid in $pids) {
        try {
            $proc = Get-Process -Id $pid -ErrorAction Stop
            Write-Host "  Stop-Process -Id $pid -Force  # $($proc.ProcessName)" -ForegroundColor Gray
        } catch {}
    }
    Write-Host ""
    Write-Host "Option 2: Change your IIS site to use a different port" -ForegroundColor White
    Write-Host "  1. Open IIS Manager" -ForegroundColor Gray
    Write-Host "  2. Select your site" -ForegroundColor Gray
    Write-Host "  3. Click 'Bindings' in Actions pane" -ForegroundColor Gray
    Write-Host "  4. Edit the binding and change port to 5002 or 5003" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Would you like to kill all these processes? (Y/N)" -ForegroundColor Yellow
    $response = Read-Host

    if ($response -eq "Y" -or $response -eq "y") {
        foreach ($pid in $pids) {
            try {
                $proc = Get-Process -Id $pid -ErrorAction Stop
                Write-Host "Stopping $($proc.ProcessName) (PID: $pid)..." -ForegroundColor Yellow
                Stop-Process -Id $pid -Force
                Write-Host "  Stopped" -ForegroundColor Green
            } catch {
                Write-Host "  Could not stop PID $pid" -ForegroundColor Red
            }
        }
        Write-Host ""
        Write-Host "Processes stopped. Try starting your site now." -ForegroundColor Green
    }
}
