<#
.SYNOPSIS
    Khởi chạy toàn bộ stack Docker Compose kết hợp Migration và Nạp dữ liệu vào SQL Server.

.DESCRIPTION
    Script thực hiện theo đúng thứ tự an toàn:
      1. Kiểm tra file .env (tự tạo từ .env.example nếu chưa có)
      2. Khởi chạy SQL Server & Redis, chờ SQL Server sẵn sàng (healthy)
      3. Chạy EF Core Migrations (tạo/cập nhật schema database)
      4. Nạp dữ liệu mẫu (SeedData.sql hoặc DataSeed_Bogus_1000.sql)
      5. Build & khởi chạy các service ứng dụng (backend, ai-backend, import-worker, web)

.PARAMETER Seed
    Chế độ nạp dữ liệu:
      - demo  (mặc định): Nạp Backend/SeedData.sql
      - bogus           : Nạp Backend/DataSeed_Bogus_1000.sql (~1000 dòng/bảng)
      - none            : Chỉ migrate schema, không nạp dữ liệu

.PARAMETER ResetDb
    Xóa toàn bộ container và volume database cũ (docker compose down -v) trước khi khởi chạy lại.

.PARAMETER SkipBuild
    Bỏ qua bước --build khi khởi chạy các service ứng dụng.

.EXAMPLE
    .\deploy.ps1
    .\deploy.ps1 -Seed bogus
    .\deploy.ps1 -Seed demo -ResetDb
#>

[CmdletBinding()]
param(
    [ValidateSet("demo", "bogus", "none")]
    [string]$Seed = "demo",

    [switch]$ResetDb,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

function Write-Step([string]$Message) {
    Write-Host ""
    Write-Host ">>> $Message" -ForegroundColor Cyan
}

# 1. Kiểm tra Docker & .env
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Không tìm thấy lệnh 'docker'. Vui lòng cài đặt và bật Docker Desktop."
}

if (-not (Test-Path ".env")) {
    Write-Step "Chưa có file .env, đang sao chép từ .env.example..."
    Copy-Item ".env.example" ".env"
    Write-Warning "Đã tạo .env từ .env.example. Hãy kiểm tra các API Key trong .env nếu cần dùng AI / Import Worker."
}

# 2. Reset toàn bộ dữ liệu nếu truyền -ResetDb
if ($ResetDb) {
    Write-Step "Đang xóa container và volume cũ (docker compose down -v)..."
    docker compose --profile tools down -v
    if ($LASTEXITCODE -ne 0) { throw "Lỗi khi chạy docker compose down -v." }
}

# 3. Khởi chạy SQL Server & Redis trước để đợi healthy
Write-Step "Khởi chạy hạ tầng dữ liệu (sqlserver, redis)..."
docker compose up -d sqlserver redis
if ($LASTEXITCODE -ne 0) { throw "Không thể khởi chạy sqlserver / redis." }

Write-Step "Đang chờ SQL Server sẵn sàng (healthy)..."
$maxWaitSeconds = 120
$elapsed = 0
while ($true) {
    $health = docker inspect --format "{{.State.Health.Status}}" toantuduy-sqlserver 2>$null
    if ($health -eq "healthy") {
        Write-Host "SQL Server đã sẵn sàng!" -ForegroundColor Green
        break
    }
    if ($elapsed -ge $maxWaitSeconds) {
        docker compose logs --tail=30 sqlserver
        throw "SQL Server không chuyển sang trạng thái healthy sau $maxWaitSeconds giây."
    }
    Start-Sleep -Seconds 3
    $elapsed += 3
    Write-Host "  ... trạng thái hiện tại: $health ($elapsed/$maxWaitSeconds s)"
}

# 4. Chạy EF Core Migrations
Write-Step "Đang áp dụng EF Core Migrations (tạo/cập nhật bảng)..."
docker compose --profile tools run --rm migrate
if ($LASTEXITCODE -ne 0) { throw "Migration thất bại! Kiểm tra log ở trên." }

# 5. Nạp dữ liệu vào Database
if ($Seed -ne "none") {
    $seedFile = if ($Seed -eq "bogus") { "DataSeed_Bogus_1000.sql" } else { "SeedData.sql" }
    $localSeedPath = Join-Path "Backend" $seedFile
    if (-not (Test-Path $localSeedPath)) {
        throw "Không tìm thấy file seed: $localSeedPath"
    }

    Write-Step "Đang nạp dữ liệu vào SQL Server từ $localSeedPath..."
    $env:SEED_FILE = $seedFile
    try {
        docker compose --profile tools run --rm seed
        if ($LASTEXITCODE -ne 0) { throw "Nạp dữ liệu ($seedFile) thất bại!" }
    }
    finally {
        Remove-Item Env:SEED_FILE -ErrorAction SilentlyContinue
    }
}
else {
    Write-Step "Bỏ qua bước nạp dữ liệu (-Seed none)."
}

# 6. Build & khởi chạy toàn bộ ứng dụng
Write-Step "Đang khởi chạy các dịch vụ ứng dụng (backend, ai-backend, import-worker, web)..."
if ($SkipBuild) {
    docker compose up -d backend ai-backend import-worker web
}
else {
    docker compose up -d --build backend ai-backend import-worker web
}
if ($LASTEXITCODE -ne 0) { throw "Lỗi khi khởi chạy các service ứng dụng." }

# 7. Tổng kết
Write-Step "Hoàn tất triển khai! Trạng thái các container:"
docker compose ps

Write-Host ""
Write-Host "==================================================================" -ForegroundColor Green
Write-Host " HỆ THỐNG TOÁN TƯ DUY ĐÃ SẴN SÀNG!" -ForegroundColor Green
Write-Host "  - Web Frontend : http://localhost:8080"
Write-Host "  - Backend API  : http://localhost:5114"
Write-Host "  - AI Swagger   : http://localhost:8001/docs"
Write-Host "  - Import Worker: http://localhost:8000/docs"
Write-Host "  - Tài khoản    : Admin123 / Teacher123 (Mật khẩu: Demo123@)"
Write-Host "==================================================================" -ForegroundColor Green
