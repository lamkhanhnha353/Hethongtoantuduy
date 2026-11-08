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

# Áp dụng migration
dotnet ef database update

# Chạy API
dotnet run
```

API lắng nghe ở `http://localhost:5114` và `https://localhost:7206` theo `Properties/launchSettings.json`.

Chuỗi kết nối được đọc từ khóa `ConnectionStrings:DefaultConnection`. Giá trị trong `appsettings.json` đang trỏ tới SQL Server LocalDB; trên máy phát triển nên ghi đè bằng biến môi trường:

```powershell
[Environment]::SetEnvironmentVariable(
  'ConnectionStrings__DefaultConnection',
  'Server=.;Database=ToanTuDuyDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true',
  'User')
```

Mở terminal mới sau khi đặt biến để ứng dụng nhận giá trị.

Kết nối trực tiếp bằng `sqlcmd`:

```powershell
sqlcmd -S . -E -C -d ToanTuDuyDb
```

Khi có thay đổi model, tạo migration mới với tên mô tả thay đổi:

```powershell
dotnet ef migrations add TenMigrationMoi
dotnet ef database update
```

Cài công cụ EF Core lần đầu: `dotnet tool install --global dotnet-ef --version 10.0.12`.

---

## Mobile App

`Mobile_App/` là ứng dụng Expo với NativeWind. Điều hướng theo cấu trúc thư mục: mọi file trong `src/app/` là một màn hình, các file `_layout.tsx` định nghĩa navigator.

| Đường dẫn | Nội dung |
| --- | --- |
| `src/app/_layout.tsx` | Root navigator, dùng `Stack`, ẩn header |
| `src/app/index.tsx` | Màn hình khởi đầu, hiện là nội dung mẫu |
| `src/global.css` | Điểm vào cho các chỉ thị Tailwind |
| `app.json` | Cấu hình ứng dụng, icon, splash, plugin |
| `eas.json` | Profile build: development, preview, production |
| `tailwind.config.js` | Dùng preset `nativewind/preset` |
| `AGENTS.md` | Quy ước phát triển cho phần mobile |

Lệnh thường dùng:

```bash
npx expo start          # chạy dev server
npx expo start --android
npx expo start --ios
npx expo start --web
npx expo lint
npx tsc --noEmit        # kiểm tra kiểu
```

Cấu hình đáng chú ý trong `app.json`: bundle Android là `com.beckham205.Mobile_App`, scheme là `mobileapp`, bật `typedRoutes` và `reactCompiler`. Profile `development` trong `eas.json` tạo development client phân phối nội bộ; `production` tự tăng số build.

Không có thư mục `ios/` và `android/` vì dự án dùng Continuous Native Generation. Không tạo hoặc sửa hai thư mục này bằng tay; hãy khai báo trong `app.json` và dùng config plugin.

---

## Web Frontend

`Web_Frontend/` là ứng dụng Vue 3 dùng Vite, viết bằng JavaScript. Cấu hình khai báo alias `@` trỏ về `src/` trong cả `vite.config.js` và `jsconfig.json`.

| Đường dẫn | Nội dung |
| --- | --- |
| `src/main.js` | Khởi tạo ứng dụng Vue, đăng ký router |
| `src/App.vue` | Component gốc |
| `src/router/index.js` | Cấu hình vue-router, chế độ HTML5 |

Lệnh thường dùng:

```bash
npm run dev
npm run build
npm run preview
```

Trong môi trường phát triển, Vite chuyển tiếp các request `/api` tới Backend tại `http://localhost:5114`. Hãy chạy Backend trước khi gọi API từ frontend.

---

## Trạng thái hiện tại

| Phần | Trạng thái |
| --- | --- |
| Backend | Đã có API xác thực hoạt động, đủ model và migration cho toàn bộ nghiệp vụ |
| Backend | Endpoint mới chưa phát triển: khóa học, lớp, buổi học, bài tập, hóa đơn, huy hiệu |
| Backend | Đăng ký JWT Bearer chưa được cấu hình trong `Program.cs`; token được sinh thủ công trong `AuthService` |
| Backend | 57 cảnh báo CS8618 về thuộc tính non-nullable chưa được xử lý |
| Mobile | Màn hình mẫu; `index.tsx` gọi `router.push('/login')` nhưng route `/login` chưa tồn tại |
| Mobile | Phần lớn gói Expo đã cài nhưng chưa dùng: reanimated, gesture-handler, safe-area-context, expo-image, expo-glass-effect, @expo/ui |
| Mobile | Script `lint` khai báo nhưng chưa có tệp cấu hình ESLint |
| Web | Khung dựng, chưa có route, chưa có tích hợp API |

### Việc cần xử lý trước khi triển khai

- Khóa JWT hiện nằm trực tiếp trong `appsettings.json` và có giá trị dự phòng trong `AuthService.GenerateJwtToken`. Cần chuyển sang biến môi trường và thêm kiểm tra độ dài khóa tối thiểu 32 byte.
- `appsettings.json` chứa thông tin nhạy cảm nên không nên commit cho môi trường thật; nên tách cấu hình theo môi trường.
- Chưa có kiểm thử tự động, chưa có tệp solution `.sln` cho phần Backend, chưa có CI.

---

## Quy trình Git

Mỗi phần làm việc trên một nhánh riêng theo dạng `<module>/feat/<mo-ta>`:

| Phần | Nhánh ví dụ |
| --- | --- |
| Backend | `backend/feat/local-sqlserver-db` |
| Mobile | `mobile/feat/add-nativewind` |

Trước khi merge, cần commit hoặc `git stash -u` các thay đổi chưa commit vì Git thường từ chối merge trên cây làm việc bẩn.

