# Hệ thống Toán Tư Duy

Nền tảng quản lý trung tâm toán tư duy, phục vụ quản trị viên, giáo viên, phụ huynh và học sinh. Hệ thống gồm API nghiệp vụ, cổng quản trị web, ứng dụng di động và dịch vụ nhập câu hỏi từ tài liệu.

## Cấu trúc repository

| Thư mục | Nội dung |
| --- | --- |
| `Backend/ToanTuDuy/` | ASP.NET Core Web API, mô hình dữ liệu, migration, xác thực và nghiệp vụ |
| `Backend/SeedData.sql` | Dữ liệu demo cho SQL Server; chạy sau khi đã áp dụng migration |
| `Web_Frontend/` | SPA Vue cho quản trị viên và giáo viên |
| `Mobile_App/` | Ứng dụng Expo cho thiết bị di động |
| `import-worker/` | FastAPI worker trích xuất câu hỏi từ PDF/DOCX |
| `docker-compose.yml` | SQL Server, chạy migration và seed dữ liệu qua Docker Compose |

Repository hiện không có thư mục `AI_Backend/`. Chức năng chat và các tích hợp liên quan cần được kiểm tra theo cấu hình/dịch vụ thực tế trong từng môi trường.

## Công nghệ

### Backend

- .NET 10, ASP.NET Core Web API, Entity Framework Core 10
- SQL Server 2022, ASP.NET Core Identity và JWT
- SignalR cho chat và thông báo realtime
- Redis cho bộ đếm thông báo chưa đọc
- Resend cho email; Cloudinary cho ảnh/tệp đính kèm
- OpenAPI chỉ được bật trong môi trường Development

### Web

- Vue 3, Vue Router 5, Vite 8, JavaScript ESM
- Tailwind CSS 4, Element Plus, Heroicons, Axios
- Có cổng quản trị và giáo viên; Vite proxy API tới backend và `/api/parser` tới import worker.

### Mobile

- Expo SDK 58, React Native 0.88, React 19, TypeScript 6
- Expo Router, NativeWind 4, Zustand, TanStack Query
- SignalR, Axios, Skia

### Import Worker

- FastAPI, Uvicorn, Python
- Trích xuất PDF bằng PyMuPDF và DOCX bằng `python-docx`
- LLM provider cấu hình được: Gemini, OpenAI hoặc `mock`
- Mặc định chạy ở cổng `8000`; dữ liệu tải lên/ảnh được lưu trong thư mục `storage/`.

## Yêu cầu

- .NET SDK 10
- SQL Server 2022 hoặc Docker Desktop với Docker Compose
- Node.js theo `engines` của `Web_Frontend/package.json` (22.18+ hoặc 24.12+)
- Node.js tương thích với Expo SDK 58 cho ứng dụng di động
- Python 3.11+ cho import worker

## Khởi chạy

### 1. Cơ sở dữ liệu

**Chạy SQL Server bằng Docker Compose:**

Tạo file `.env` ở thư mục gốc và khai báo mật khẩu SQL Server cho biến `MSSQL_SA_PASSWORD`. Không đưa file `.env` hoặc thông tin bí mật vào Git.

```powershell
docker compose up -d sqlserver
docker compose run --rm migrate
```

Sau khi migration hoàn tất, có thể nạp dữ liệu demo:

```powershell
docker compose --profile seed run --rm seed
```

`Backend/SeedData.sql` xóa và tạo lại dữ liệu demo trong các bảng nghiệp vụ trước khi chèn dữ liệu mới. **Chỉ chạy trên cơ sở dữ liệu phát triển có thể làm mới; không chạy trên dữ liệu cần giữ lại.**

**Hoặc dùng SQL Server LocalDB trên Windows:**

```powershell
cd Backend\ToanTuDuy
dotnet ef database update
```

Backend dùng `ConnectionStrings:DefaultConnection`; nếu không cấu hình, sẽ thử kết nối `ToanTuDuyDb` trên LocalDB. Có thể đặt cấu hình bằng biến môi trường `ConnectionStrings__DefaultConnection`.

### 2. Backend API

```powershell
cd Backend\ToanTuDuy
dotnet run
```

Theo `launchSettings.json`, HTTP chạy tại `http://localhost:5114`, HTTPS tại `https://localhost:7206`. OpenAPI trong môi trường Development: `http://localhost:5114/openapi/v1.json`.

### 3. Web Frontend

```powershell
cd Web_Frontend
npm install
npm run dev
npm run build
```

Vite chạy tại `http://localhost:5173`. Mặc định `/api` được proxy tới `http://localhost:5114`; `/api/parser` được proxy tới `http://localhost:8000`. Có thể thay địa chỉ backend bằng biến `VITE_API_BASE_URL`.

### 4. Import Worker

```powershell
cd import-worker
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
uvicorn app.main:app --reload --port 8000
```

Chọn `LLM_PROVIDER=gemini` hoặc `LLM_PROVIDER=openai` và cấu hình API key tương ứng trong file `.env`. Có thể chọn `LLM_PROVIDER=mock` để chạy luồng kiểm thử không dùng dịch vụ LLM bên ngoài. API docs có tại `http://localhost:8000/docs`; health check tại `http://localhost:8000/health`.

### 5. Mobile App

```powershell
cd Mobile_App
npm install
npx expo start
```

Dùng `npx expo start --android`, `npx expo start --ios` hoặc `npm run web` để chọn nền tảng. Dự án dùng Expo Continuous Native Generation; không tạo thủ công thư mục `ios/` hoặc `android/`.

## Nghiệp vụ chính

- **Quản trị:** quản lý tài khoản, phụ huynh/học sinh, giáo viên, khóa học, lớp học, lịch học, hóa đơn, bảng lương và thông báo.
- **Giáo viên:** quản lý lớp, học sinh, điểm danh, bài tập, chấm điểm, lịch dạy và thông báo phụ huynh.
- **Học tập:** ngân hàng câu hỏi, bài tập và tiến độ học sinh.
- **Thông báo:** quản lý thông báo tập trung, hộp thư theo từng tài khoản, đánh dấu đã đọc, tùy chọn kênh và cập nhật realtime.
- **Nhập đề:** nhận PDF/DOCX, trích xuất nội dung và chuẩn hóa câu hỏi qua import worker.

### Luồng thông báo

Thông báo được lưu trong `NOTIFICATION`; đối tượng nhận được đưa vào `OUTBOX`. `NotificationFanoutWorker` phân giải đối tượng nhận, tạo từng bản ghi hộp thư trong `USER_INBOX`, cập nhật bộ đếm chưa đọc và phát sự kiện SignalR. Cấu hình kênh theo tài khoản được lưu trong `NOTIFICATION_SETTING`.

Trang admin dùng `GET /api/admin/notifications` để lọc và phân trang lịch sử, `POST /api/admin/notifications` để tạo thông báo. Hiện có thể gửi tới phụ huynh, giáo viên hoặc cả hai; thông báo được đưa vào hàng đợi để worker xử lý.

## Cơ sở dữ liệu và migration

Database mặc định là `ToanTuDuyDb` trên SQL Server. Schema được quản lý bằng EF Core migrations trong `Backend/ToanTuDuy/Migrations/`. Các nhóm dữ liệu nghiệp vụ gồm:

- Tài khoản và hồ sơ: `ACCOUNT`, `ADMIN_PROFILE`, `PARENT_PROFILE`, `STUDENT_PROFILE`, `TEACHER_PROFILE`
- Đào tạo: khóa học, cấp độ, lớp, lịch học, buổi học, học viên lớp, điểm danh
- Học tập: ngân hàng câu hỏi, câu hỏi, bài tập, bài làm, huy hiệu
- Vận hành: đơn xin nghỉ, hóa đơn, thanh toán, bảng lương và điều chỉnh lương
- Thông báo: `NOTIFICATION`, `OUTBOX`, `USER_INBOX`, `NOTIFICATION_SETTING`
- ASP.NET Core Identity và lịch sử migration EF Core

Tạo migration và cập nhật database:

```powershell
cd Backend\ToanTuDuy
dotnet ef migrations add TenMigrationMoi
dotnet ef database update
```

## API tiêu biểu

API được bảo vệ bằng JWT và phân quyền theo vai trò tùy endpoint. Xem OpenAPI khi chạy backend ở Development để tra cứu đầy đủ request/response.

| Nhóm | Endpoint tiêu biểu | Mô tả |
| --- | --- | --- |
| Xác thực | `/api/auth/register`, `/api/auth/login`, `/api/auth/google-login` | Đăng ký và đăng nhập |
| Hồ sơ | `/api/profile` | Hồ sơ tài khoản |
| Khóa học/lớp | `/api/courses`, `/api/classes`, `/api/classSchedules` | Quản lý khóa học, lớp và lịch |
| Học tập | `/api/question-banks`, `/api/assignments`, `/api/lessons` | Câu hỏi, bài tập và buổi học |
| Điểm danh | `/api/attendance` | Điểm danh theo buổi hoặc học sinh |
| Hóa đơn | `/api/invoice/history`, `/api/admin/invoices` | Lịch sử và tra cứu hóa đơn |
| Thông báo cá nhân | `/api/notifications`, `/api/notifications/settings` | Hộp thư, trạng thái đọc và cài đặt |
| Thông báo admin | `/api/admin/notifications` | Lịch sử và tạo thông báo |
| Chat | `/api/chat/conversations`, `/api/chat/send`, `/api/chat/send-attachment` | Hội thoại và gửi tin nhắn/tệp đính kèm |
| Realtime | `/hubs/notifications`, `/chathub` | SignalR cho thông báo và chat |

## Kiểm tra

```powershell
# Build backend
dotnet build Backend\ToanTuDuy\ToanTuDuy.csproj

# Build web
cd Web_Frontend
npm run build

# Kiểm tra kiểu/lint ứng dụng di động
cd ..\Mobile_App
npm run lint
```

Import worker có kiểm thử trong `import-worker/tests/`; cách chạy cụ thể được mô tả trong [import-worker/README.md](./import-worker/README.md).

## Git

Có thể đặt nhánh theo dạng `<module>/feat/<mo-ta>`, ví dụ `backend/feat/course-endpoints`, `mobile/feat/login-screen` hoặc `web/feat/admin-notifications`. Trước khi merge, kiểm tra trạng thái worktree và lưu các thay đổi cần thiết.
