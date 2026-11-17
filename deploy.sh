#!/usr/bin/env bash
set -euo pipefail

# =============================================================================
# Script khởi chạy toàn bộ stack Docker Compose kết hợp Migration & Seed Data
# Cách dùng:
#   ./deploy.sh                  # Chạy mặc định với SeedData.sql
#   ./deploy.sh --seed bogus     # Nạp dữ liệu mẫu Bogus (~1000 dòng/bảng)
#   ./deploy.sh --seed none      # Chỉ migrate schema, không nạp dữ liệu
#   ./deploy.sh --reset-db       # Xóa volume DB cũ trước khi chạy lại
#   ./deploy.sh --skip-build     # Không rebuild image ứng dụng
# =============================================================================

cd "$(dirname "${BASH_SOURCE[0]}")"

SEED_MODE="demo"
RESET_DB=false
SKIP_BUILD=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --seed)
      SEED_MODE="${2:-}"
      shift 2
      ;;
    --reset-db)
      RESET_DB=true
      shift
      ;;
    --skip-build)
      SKIP_BUILD=true
      shift
      ;;
    -h|--help)
      echo "Cách dùng: $0 [--seed demo|bogus|none] [--reset-db] [--skip-build]"
      exit 0
      ;;
    *)
      echo "Tham số không hợp lệ: $1" >&2
      exit 1
      ;;
  esac
done

if [[ "$SEED_MODE" != "demo" && "$SEED_MODE" != "bogus" && "$SEED_MODE" != "none" ]]; then
  echo "Giá trị --seed phải là: demo | bogus | none" >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Không tìm thấy lệnh 'docker'." >&2
  exit 1
fi

if [[ ! -f .env ]]; then
  echo ">>> Chưa có .env, đang sao chép từ .env.example..."
  cp .env.example .env
fi

if [[ "$RESET_DB" == true ]]; then
  echo ">>> Đang xóa container và volume cũ (docker compose down -v)..."
  docker compose --profile tools down -v
fi

echo ">>> Khởi chạy sqlserver và redis..."
docker compose up -d sqlserver redis

echo ">>> Đang chờ SQL Server sẵn sàng (healthy)..."
elapsed=0
max_wait=120
while true; do
  health="$(docker inspect --format '{{.State.Health.Status}}' toantuduy-sqlserver 2>/dev/null || echo "starting")"
  if [[ "$health" == "healthy" ]]; then
    echo "SQL Server đã sẵn sàng!"
    break
  fi
  if [[ $elapsed -ge $max_wait ]]; then
    docker compose logs --tail=30 sqlserver
    echo "SQL Server không healthy sau ${max_wait}s." >&2
    exit 1
  fi
  sleep 3
  elapsed=$((elapsed + 3))
  echo "  ... trạng thái: $health (${elapsed}/${max_wait}s)"
done

echo ">>> Đang áp dụng EF Core Migrations..."
docker compose --profile tools run --rm migrate

if [[ "$SEED_MODE" != "none" ]]; then
  if [[ "$SEED_MODE" == "bogus" ]]; then
    SEED_FILE="DataSeed_Bogus_1000.sql"
  else
    SEED_FILE="SeedData.sql"
  fi

  if [[ ! -f "Backend/$SEED_FILE" ]]; then
    echo "Không tìm thấy file Backend/$SEED_FILE" >&2
    exit 1
  fi

  echo ">>> Đang nạp dữ liệu từ Backend/$SEED_FILE..."
  SEED_FILE="$SEED_FILE" docker compose --profile tools run --rm seed
else
  echo ">>> Bỏ qua bước nạp dữ liệu (--seed none)."
fi

echo ">>> Đang khởi chạy các dịch vụ ứng dụng..."
if [[ "$SKIP_BUILD" == true ]]; then
  docker compose up -d backend ai-backend import-worker web
else
  docker compose up -d --build backend ai-backend import-worker web
fi

docker compose ps

echo ""
echo "=================================================================="
echo " HỆ THỐNG TOÁN TƯ DUY ĐÃ SẴN SÀNG!"
echo "  - Web Frontend : http://localhost:8080"
echo "  - Backend API  : http://localhost:5114"
echo "  - AI Swagger   : http://localhost:8001/docs"
echo "  - Import Worker: http://localhost:8000/docs"
echo "  - Tài khoản    : Admin123 / Teacher123 (Mật khẩu: Demo123@)"
echo "=================================================================="
