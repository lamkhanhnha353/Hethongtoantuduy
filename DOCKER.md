# Chạy dự án bằng Docker

Tài liệu này hướng dẫn chạy toàn bộ hệ thống bằng Docker Compose:

| Service | Image / Build | Cổng mặc định | Mô tả |
| --- | --- | --- | --- |
| `web` | `Web_Frontend/Dockerfile` (Node → Nginx) | `8080` | Giao diện Vue 3 |
| `backend` | `Backend/ToanTuDuy/Dockerfile` | `5114` | ASP.NET Core Web API |
| `ai-backend` | `AI_Backend/Dockerfile` | `8001` | FastAPI RAG chatbot |
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest` | `1433` | Cơ sở dữ liệu |
| `redis` | `redis:7-alpine` | `6379` | Bộ đếm thông báo chưa đọc |
| `migrate` | target `migrations` của backend | – | Chạy `dotnet ef database update` |
| `seed` | SQL Server image | – | Nạp `Backend/SeedData.sql` |

## 1. Chuẩn bị

Yêu cầu: Docker Desktop / Docker Engine + Docker Compose v2.

```powershell
# Từ thư mục gốc dự án
Copy-Item .env.example .env
```

Mở `.env` và **đổi tối thiểu** các giá trị sau:

- `MSSQL_SA_PASSWORD`: mật khẩu SQL Server (≥ 8 ký tự, có hoa/thường/số/ký tự đặc biệt).
- `JWT_KEY`: khóa ký JWT, ≥ 32 byte. Phải trùng giữa backend và AI service (compose đã tự truyền cùng một biến).
- `VITE_API_BASE_URL`: URL backend mà **trình duyệt** truy cập được, mặc định `http://localhost:5114`.
- `AI_LLM_API_KEY`, `AI_NOMIC_API_KEY` nếu dùng chatbot RAG.

> `.env` đã nằm trong `.gitignore`. Không commit file này.

## 2. Khởi chạy

```powershell
# Build và chạy SQL Server, Redis, backend, AI, web
docker compose up -d --build

# Lần đầu: áp dụng migration (database trống)
docker compose --profile tools run --rm migrate
```

Kiểm tra trạng thái:

```powershell
docker compose ps
docker compose logs -f backend
```

Truy cập:

- Web: <http://localhost:8080>
- API: <http://localhost:5114>
- AI Swagger: <http://localhost:8001/docs>
- SQL Server: `localhost,1433` (user `sa`, mật khẩu trong `.env`)

## 3. Dữ liệu demo (tùy chọn)

`Backend/SeedData.sql` sẽ **xóa và tạo lại** dữ liệu nghiệp vụ. Chỉ chạy trên database phát triển:

```powershell
docker compose --profile tools run --rm seed
```

## 4. Lệnh thường dùng

```powershell
# Xem log
docker compose logs -f web backend ai-backend

# Build lại một service sau khi sửa code
docker compose up -d --build backend
docker compose up -d --build web

# Dừng / xóa container (giữ dữ liệu trong volume)
docker compose down

# Xóa toàn bộ container + volume (MẤT dữ liệu SQL Server, Redis, AI storage)
docker compose down -v

# Mở shell trong container
docker compose exec backend sh
docker compose exec ai-backend bash
```

## 5. Ghi chú cấu hình

### Frontend gọi API

`VITE_API_BASE_URL` được Vite nhúng vào bundle **lúc build**, không phải lúc chạy. Sau khi đổi giá trị trong `.env`, phải build lại:

```powershell
docker compose up -d --build web
```

Nếu muốn web và API cùng origin (ví dụ qua reverse proxy), đặt `VITE_API_BASE_URL=http://localhost:8080`. Khi đó Nginx sẽ chuyển tiếp `/api`, `/chathub`, `/hubs` tới service `backend` (xem `Web_Frontend/nginx.conf`).

### Cloudinary / Resend / VnPay

Các cấu hình này hiện có giá trị mặc định trong `Backend/ToanTuDuy/appsettings.json`. Để ghi đè bằng biến môi trường, thêm vào service `backend` trong `docker-compose.yml`:

```yaml
    environment:
      Cloudinary__CloudName: ${CLOUDINARY_CLOUD_NAME}
      Cloudinary__ApiKey: ${CLOUDINARY_API_KEY}
      Cloudinary__ApiSecret: ${CLOUDINARY_API_SECRET}
      VnPay__TmnCode: ${VNPAY_TMN_CODE}
      VnPay__HashSecret: ${VNPAY_HASH_SECRET}
```

rồi khai báo các biến tương ứng trong `.env` và chạy `docker compose up -d backend`.

### AI service dùng Ollama trên máy host

Compose đã thêm `host.docker.internal:host-gateway`. Đặt:

```dotenv
AI_LLM_PROVIDER=ollama
AI_EMBEDDING_PROVIDER=ollama
OLLAMA_BASE_URL=http://host.docker.internal:11434
```

### HTTPS

Compose chỉ mở HTTP. Khi triển khai thật, đặt một reverse proxy (Nginx, Caddy, Traefik) phía trước để kết thúc TLS, rồi trỏ `VITE_API_BASE_URL` và `PUBLIC_WEB_URL` về domain HTTPS.

## 6. Xử lý sự cố

| Hiện tượng | Cách xử lý |
| --- | --- |
| `migrate` báo không kết nối được SQL Server | Chờ `sqlserver` healthy (`docker compose ps`), sau đó chạy lại. |
| Đăng nhập web bị lỗi mạng / CORS | Kiểm tra `VITE_API_BASE_URL` đúng URL trình duyệt truy cập được và rebuild `web`. |
| SignalR không kết nối | Đảm bảo URL backend đúng và reverse proxy có header `Upgrade`/`Connection` (đã cấu hình trong `nginx.conf`). |
| SQL Server báo sai mật khẩu | Đổi `MSSQL_SA_PASSWORD` **trước** lần chạy đầu; nếu đã tạo volume, chạy `docker compose down -v` để tạo lại. |
| Backend lỗi `Jwt:Key` | Đặt `JWT_KEY` trong `.env` (≥ 32 byte). |
| AI service `degraded` | Kiểm tra `AI_LLM_API_KEY` / `AI_NOMIC_API_KEY`; xem `docker compose logs ai-backend`. |

## 7. Phát triển không dùng Docker

Nếu chỉ muốn chạy SQL Server/Redis bằng Docker và chạy code trực tiếp:

```powershell
docker compose up -d sqlserver redis
docker compose --profile tools run --rm migrate

cd Backend\ToanTuDuy
dotnet run

cd ..\..\Web_Frontend
npm install
npm run dev
```
