# Hệ thống Toán Tư Duy

Nền tảng quản lý học tập toán tư duy: quản lý khóa học, lớp học, buổi học, bài tập, điểm, chuyên cần và học phí — dành cho phụ huynh, học sinh và giáo viên.

Dự án gồm ba phần độc lập:

| Phần | Thư mục | Vai trò |
| --- | --- | --- |
| Backend API | `Backend/` | REST API xử lý nghiệp vụ và truy cập SQL Server |
| Ứng dụng mobile | `Mobile_App/` | App iOS/Android dùng Expo, cho phụ huynh và học sinh |
| Giao diện web | `Web_Frontend/` | SPA quản trị, dự kiến cho giáo viên và quản trị viên |

---

## Công nghệ sử dụng

| Lớp / Phạm vi | Công nghệ | Phiên bản | Vai trò trong dự án |
| --- | --- | --- | --- |
| **Backend** | .NET | 10.0 (net10.0) | Runtime và framework |
| Backend | ASP.NET Core Web API | 10.0.12 | HTTP API, middleware, DI, routing |
| Backend | Entity Framework Core | 10.0.12 | ORM, ánh xạ model, migration |
| Backend | EF Core SqlServer Provider | 10.0.12 | Kết nối SQL Server |
| Backend | ASP.NET Core Identity | 10.0.12 | Xác thực, hash mật khẩu, quản lý role |
| Backend | Json Web Token | System.IdentityModel | Sinh access token và refresh token |
| Backend | JWT Bearer Authentication | 10.0.12 | Gói tham chiếu cho xác thực bearer |
| Backend | Microsoft.AspNetCore.OpenApi | 10.0.12 | Tài liệu OpenAPI ở môi trường Development |
| Backend | SQL Server | 2022 (16.0) | Cơ sở dữ liệu, xác thực Windows |
| **Mobile** | Expo SDK | ~57.0.25 | Nền tảng và bộ công cụ build |
| Mobile | React Native | 0.86.3 | Framework giao diện native |
| Mobile | React | 19.2.3 | Thư viện UI |
| Mobile | Expo Router | ~57.0.23 | Định tuyến theo cấu trúc thư mục |
| Mobile | TypeScript | ~6.0.3 | Ngôn ngữ, bật `strict` |
| Mobile | NativeWind | 4.2.7 | Tailwind CSS cho React Native |
| Mobile | Tailwind CSS | ^3.4.17 | Hệ thiết kế utility-first |
| Mobile | React Native Reanimated | 4.5.1 | Animation (đã cài, chưa dùng) |
| Mobile | React Native Gesture Handler | ~2.32.0 | Xử lý cử chỉ (đã cài, chưa dùng) |
| Mobile | React Native Safe Area Context | ~5.7.0 | Vùng an toàn (đã cài, chưa dùng) |
| Mobile | React Native Web | ~0.21.0 | Xuất bản web từ cùng mã nguồn |
| Mobile | EAS Build / Submit | CLI >= 24.8.0 | Build và phát hành trên cloud |
| Mobile | Babel + Metro | theo Expo SDK | Bundler và biến đổi JSX |
| **Web** | Vue | ^3.5.42 | Framework giao diện, Composition API |
| Web | Vue Router | ^5.3.1 | Định tuyến phía client, chế độ HTML5 |
| Web | Vite | ^8.2.2 | Dev server và bundler |
| Web | @vitejs/plugin-vue | ^6.0.8 | Biên dịch SFC `.vue` |
| Web | vite-plugin-vue-devtools | ^8.2.1 | Công cụ gỡ lỗi |
| Web | JavaScript (ESM) | — | Ngôn ngữ, dùng `jsconfig.json` |
| Web | Node.js | ^22.18.0 hoặc >=24.12.0 | Môi trường chạy |
| **Công cụ** | Git | — | Quản lý phiên bản, nhánh theo module |
| Công cụ | SQL Server Management Studio / sqlcmd | — | Quản trị database |
| Công cụ | dotnet-ef | 10.0.12 | Tạo và áp dụng migration |

---

## Backend

Cấu trúc mã nguồn trong `Backend/ToanTuDuy/`:

| Đường dẫn | Nội dung |
| --- | --- |
| `Program.cs` | Khởi tạo ứng dụng: controller, DbContext, Identity, DI, middleware |
| `Data/ApplicationDbContext.cs` | Ánh xạ model, đổi tên bảng cột, khóa chính kép, quy tắc xóa |
| `Models/UserEntities.cs` | `ApplicationUser`, `ParentProfile`, `StudentProfile`, `TeacherProfile` |
| `Models/LearningEntities.cs` | Các entity nghiệp vụ: khóa học, lớp, buổi học, bài tập, hóa đơn, huy hiệu |
| `Controllers/AuthController.cs` | Endpoint đăng ký và đăng nhập |
| `Services/AuthService.cs` | Nghiệp vụ đăng ký, đăng nhập, sinh token |
| `DTOs/` | Request và response cho API xác thực |
| `Migrations/` | 4 migration hiện có |
| `ToanTuDuy.http` | File test API cho REST Client |
| `appsettings.json` | Chuỗi kết nối và cấu hình JWT |

### Endpoint hiện có

| Method | Đường dẫn | Mô tả |
| --- | --- | --- |
| POST | `/api/auth/register` | Đăng ký tài khoản Parent hoặc Teacher, tự tạo profile và sinh token |
| POST | `/api/auth/login` | Đăng nhập bằng số điện thoại, trả access token và refresh token |
| GET | `/openapi/v1.json` | Tài liệu OpenAPI, chỉ bật ở môi trường Development |

Đăng ký nhận `phone`, `password`, `role` (`Parent` hoặc `Teacher`), `fullName`, và `email` bắt buộc khi `role` là `Teacher`. Đăng nhập nhận `phone` và `password`.

### Cơ sở dữ liệu

Database: `ToanTuDuyDb` trên SQL Server, xác thực bằng Windows Authentication. Tổng cộng **24 bảng**: 17 bảng nghiệp vụ, 6 bảng của ASP.NET Core Identity và 1 bảng lịch sử migration.

Bảng nghiệp vụ gồm: `ACCOUNT`, `PARENT_PROFILE`, `STUDENT_PROFILE`, `TEACHER_PROFILE`, `COURSE`, `CLASSES`, `CLASS_SCHEDULE`, `CLASS_STUDENT`, `LESSON`, `ATTENDANCE`, `LEAVE_REQUEST`, `ASSIGNMENT`, `STUDENT_ASSIGNMENT`, `QUESTION`, `INVOICE`, `BADGE`, `STUDENT_BADGE`.

Một số quy ước đã cấu hình trong `ApplicationDbContext.cs`:

- Tên bảng và tên cột dùng chữ in hoa, ví dụ `ACCOUNT.password_hash`.
- `ACCOUNT` ánh xạ từ `ApplicationUser` của Identity.
- Khóa chính kép cho `CLASS_STUDENT` và `STUDENT_BADGE`.
- `PARENT_PROFILE.account_id` và `TEACHER_PROFILE.account_id` là duy nhất.
- Tiền tố học phí và số tiền hóa đơn dùng kiểu `decimal(18,2)`.
- Toàn bộ khóa ngoại đặt `Restrict` để tránh nhiều đường xóa dây chuyền; xóa cha phải xử lý thủ công.

### Migration hiện có

| Migration | Nội dung |
| --- | --- |
| `InitDb` | Tạo toàn bộ 17 bảng nghiệp vụ và 6 bảng Identity |
| `InitDb1` | Migration rỗng |
| `AllowNullRefreshToken` | Cho phép `ACCOUNT.refresh_token` nullable |
| `MakeOptionalFieldsNullable` | Cho phép nullable cho 8 cột tùy chọn |

### Chạy Backend

Yêu cầu: .NET SDK 10 và SQL Server đang chạy.

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

Phần này đang ở trạng thái khung dựng: mảng route còn trống, `App.vue` hiển thị nội dung giữ chỗ, chưa có lớp gọi API hay thư viện giao diện. Vite chưa khai báo cổng dev server và chưa có proxy trỏ về Backend.

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
