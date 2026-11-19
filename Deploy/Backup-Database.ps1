# =========================================================================================
# Tác vụ sao lưu tự động CSDL CuaHangTienLoi (PowerShell)
# Đáp ứng NFR03: Sao lưu dữ liệu theo lịch hàng ngày
# Có thể đưa vào Windows Task Scheduler để chạy lúc 01:00 AM mỗi đêm
# =========================================================================================

param(
    [string]$ServerInstance = "localhost",
    [string]$DatabaseName = "CuaHangTienLoi",
    [string]$BackupDir = "D:\Backups\CuaHangTienLoi",
    [int]$RetentionDays = 30
)

$ErrorActionPreference = "Stop"

try {
    Write-Host "[INFO] Bắt đầu tác vụ sao lưu CSDL $DatabaseName..." -ForegroundColor Cyan

    if (-not (Test-Path -Path $BackupDir)) {
        New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
        Write-Host "[INFO] Đã tạo thư mục lưu trữ: $BackupDir" -ForegroundColor Green
    }

    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFileName = "$DatabaseName`_Full_$timestamp.bak"
    $backupFilePath = Join-Path -Path $BackupDir -ChildPath $backupFileName

    # Câu lệnh SQL thực hiện Backup
    $sqlCmd = @"
BACKUP DATABASE [$DatabaseName] 
TO DISK = N'$backupFilePath' 
WITH FORMAT, INIT, COMPRESSION, CHECKSUM, STATS = 10;

RESTORE VERIFYONLY FROM DISK = N'$backupFilePath' WITH CHECKSUM;
"@

    Write-Host "[INFO] Đang sao lưu vào: $backupFilePath..." -ForegroundColor Yellow
    Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $sqlCmd -QueryTimeout 600

    Write-Host "[SUCCESS] Sao lưu CSDL $DatabaseName thành công!" -ForegroundColor Green

    # Dọn dẹp bản sao lưu cũ vượt quá số ngày lưu trữ
    $limitDate = (Get-Date).AddDays(-$RetentionDays)
    $oldFiles = Get-ChildItem -Path $BackupDir -Filter "$DatabaseName`_Full_*.bak" | Where-Object { $_.LastWriteTime -lt $limitDate }

    foreach ($file in $oldFiles) {
        Remove-Item -Path $file.FullName -Force
        Write-Host "[CLEANUP] Đã xóa bản sao lưu cũ: $($file.Name)" -ForegroundColor DarkGray
    }

    Write-Host "[DONE] Tác vụ sao lưu hoàn tất lúc $(Get-Date)" -ForegroundColor Cyan
}
catch {
    Write-Host "[ERROR] Lỗi sao lưu CSDL: $_" -ForegroundColor Red
    exit 1
}
