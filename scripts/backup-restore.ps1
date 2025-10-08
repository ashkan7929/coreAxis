param(
    [string]$SqlInstance = "localhost",
    [string]$Database = "CoreAxis",
    [string]$BackupDir = "D:\\Backups",
    [switch]$RestoreTest
)

$ErrorActionPreference = "Stop"

# Backup
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupDir "$Database`_$timestamp.bak"
Write-Host "Backing up $Database to $backupFile" -ForegroundColor Yellow
sqlcmd -S $SqlInstance -Q "BACKUP DATABASE [$Database] TO DISK = N'$backupFile' WITH INIT, COMPRESSION"

if ($RestoreTest) {
    $restoreDb = "$Database`_restore_$timestamp"
    Write-Host "Restoring into $restoreDb" -ForegroundColor Yellow
    sqlcmd -S $SqlInstance -Q "RESTORE DATABASE [$restoreDb] FROM DISK = N'$backupFile' WITH MOVE '$Database' TO 'C:\\SQLData\\$restoreDb.mdf', MOVE '${Database}_log' TO 'C:\\SQLData\\$restoreDb.ldf'"
}

Write-Host "Backup (and optional restore) completed." -ForegroundColor Green