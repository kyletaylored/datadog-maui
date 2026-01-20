# Restore IIS Express applicationHost.config from backup

$ErrorActionPreference = "Stop"

$configPath = "$env:USERPROFILE\Documents\IISExpress\config\applicationHost.config"

Write-Host "Looking for backup files..."
$backups = Get-ChildItem "$configPath.backup.*" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending

if ($backups.Count -eq 0) {
    Write-Error "No backup files found at $configPath.backup.*"
    exit 1
}

$latestBackup = $backups[0]
Write-Host "Found latest backup: $($latestBackup.Name)"
Write-Host "Created: $($latestBackup.LastWriteTime)"

$confirm = Read-Host "Restore this backup? (y/n)"
if ($confirm -ne 'y') {
    Write-Host "Cancelled"
    exit 0
}

Copy-Item $latestBackup.FullName $configPath -Force
Write-Host "Config restored successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "IIS Express should now work normally."
Write-Host "Close and reopen Visual Studio to pick up the restored config."
