# 01. Test Plan — Kiểm thử API Tự động với xUnit & WebApplicationFactory

**Hệ thống:** Backend Toán Tư Duy (`ToanTuDuy` — ASP.NET Core 10 Web API)  
**Công nghệ kiểm thử:** `xUnit` + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) + `FluentAssertions` (khuyến nghị) + `Respawn`  
**Cơ sở tham chiếu:** Bộ tài liệu review tại [Document/Review/Backend/README.md](../../../Review/Backend/README.md)

---

## 1. Mục tiêu & Phạm vi

### 1.1. Mục tiêu
1. Tự động hóa kiểm thử hồi quy ở mức **API Integration Test** trong cùng hệ sinh thái `.NET 10`, chạy được trực tiếp bằng `dotnet test` trên máy dev và CI/CD.
2. Xác minh đúng:
   - Hợp đồng API (HTTP status codes, JSON schema, phân trang `PagedResult<T>`, thông báo lỗi).
   - Cơ chế xác thực & phân quyền (JWT, `SecurityStamp` invalidation, `[Authorize(Roles = ...)]`, `[RequirePin]`, ownership Parent–Student).
   - Luồng nghiệp vụ liên hoàn có trạng thái (Đăng ký → Xác minh email → Ghi danh → VNPay IPN → Lịch học/Sinh buổi → Xin nghỉ/Điểm danh → Bài tập/Tự chấm → Tính lương → Thông báo/Chat).
   - Tính toàn vẹn dữ liệu & đồng thời (Idempotency của VNPay IPN, Verify Email, Submit Practice song song).

### 1.2. Phạm vi (Scope)
- **Trong phạm vi (`ToanTuDuy`):** Toàn bộ **25 Controllers** và **2 SignalR Hubs** (`/chathub`, `/hubs/notifications` — kiểm tra kết nối/xác thực JWT và nhận event từ service).
- **Ngoài phạm vi trực tiếp của xUnit (`import-worker`):** Service Python FastAPI đã có bộ `pytest` riêng tại `Backend/import-worker/tests/`. Việc tích hợp giữa dữ liệu đầu ra `import-worker` và `POST /api/question-banks/questions/bulk` sẽ được kiểm thử qua test data mô phỏng JSON từ worker.

---

## 2. Kiến trúc Giải pháp `xUnit + WebApplicationFactory`

### 2.1. Điều kiện tiên quyết ở Project chính (`ToanTuDuy`)
Hiện tại [`Program.cs`](../../../Backend/ToanTuDuy/Program.cs) dùng top-level statements và [`ToanTuDuy.csproj`](../../../Backend/ToanTuDuy/ToanTuDuy.csproj) chưa mở `InternalsVisibleTo`. Để `WebApplicationFactory<Program>` khởi tạo được trong test project, cần thực hiện **1 trong 2** cách:
- **Cách 1 (Khuyến nghị):** Thêm dòng cuối vào `Backend/ToanTuDuy/Program.cs`:
  ```csharp
  public partial class Program { }
  ```
- **Cách 2:** Thêm `<InternalsVisibleTo Include="ToanTuDuy.Tests" />` vào `ToanTuDuy.csproj`.

> Lưu ý: Cách 1 cũng giúp test gọi trực tiếp `UpcomingLessonReminderService.ProcessUpcomingLessonsAsync` nếu kết hợp thêm `InternalsVisibleTo` (vì method này đang là `internal`).

### 2.2. Chiến lược Cơ sở dữ liệu cho Test (`SQL Server` vs `InMemory/SQLite`)
- **KHÔNG dùng `UseInMemoryDatabase` hoặc SQLite** vì code backend sử dụng nhiều tính năng đặc thù EF Core / SQL Server:
  - `ExecuteUpdateAsync` (`EmailVerificationService`, `AssignmentService`, `NotificationService`).
  - `BeginTransactionAsync(IsolationLevel.Serializable)` (`PayrollService`).
  - `EF.Functions.Like`, Filtered Unique Index (`[invoice_id] IS NOT NULL`, `[ref_id] IS NOT NULL`, `[status] IN ('ACTIVE', 'PENDING')`).
- **Giải pháp khuyến nghị:** Dùng **SQL Server thật** cho test:
  - Local dev: SQL Server LocalDB (`(localdb)\mssqllocaldb` với DB `ToanTuDuyDb_Test`) hoặc Docker SQL Server.
  - CI/CD: `Testcontainers.MsSql` (`mcr.microsoft.com/mssql/server:2022-latest`).
  - Chạy `dbContext.Database.MigrateAsync()` 1 lần duy nhất khi khởi tạo `ICollectionFixture` (`DatabaseCollection`).
  - Reset dữ liệu sạch giữa các test bằng **`Respawn`** (`Respawner.CreateAsync`) hoặc cô lập dữ liệu bằng unique identifier theo từng test (`Guid` phone/email/code). *Lưu ý:* tránh dùng chung bảng `__EFMigrationsHistory` khi Respawn.

### 2.3. Thiết kế `CustomWebApplicationFactory` & Giả lập Phụ thuộc Ngoài

Trong `ConfigureWebHost(IWebHostBuilder builder)`:

| Thành phần | Trong Production (`Program.cs`) | Thay thế trong `CustomWebApplicationFactory` | Mục đích kiểm thử |
| :--- | :--- | :--- | :--- |
| **Environment** | `Development` / `Production` | Tách 2 factory/profile:<br>1. `ProductionWebAppFactory` (`ASPNETCORE_ENVIRONMENT=Production`)<br>2. `DevWebAppFactory` (`Development`) | Profile `Production` để test **RBAC thật** cho `AdminOrDevelopmentTesting` (`ClassSchedulesController`) và xác nhận `POST /api/notifications/test` trả `404` |
| **Email (`IEmailDeliveryService`)** | `EmailDeliveryService` (SMTP / Resend) | `FakeEmailDeliveryService` (Singleton spy lưu `List<EmailDeliveryMessage> SentEmails`, hỗ trợ bật/tắt `IsConfiguredResult`) | Đọc được **magic link xác minh email**, **mật khẩu tạm ForgotPassword**, **HTML phiếu lương**, và test nhánh `400/502` khi email chưa cấu hình hoặc lỗi gửi |
| **Cloudinary (`IImageStorage`, `IChatAttachmentStorage`, `ICourseMediaStorage`)** | Upload lên `res.cloudinary.com` | `FakeImageStorage`, `FakeChatAttachmentStorage`, `FakeCourseMediaStorage` (vẫn chạy validation thật như `ImageUploadRules.EnsureValid` hoặc tái sử dụng logic validate, chỉ fake bước gọi HTTP Cloudinary và trả URL giả lập đúng cấu trúc `https://res.cloudinary.com/{cloudName}/...`) | Không upload rác lên Cloudinary thật; vẫn test được `400` sai định dạng/dung lượng và `DeletedUrls` khi xóa avatar/tin nhắn |
| **Redis (`IUnreadCountStore`)** | `RedisUnreadCountStore` (`IConnectionMultiplexer`) | Mặc định dùng `InMemoryUnreadCountStore` (hoặc container Redis test thật); 1 test riêng giả lập Redis lỗi để verify fallback đếm SQL | Không phụ thuộc Redis daemon khi chạy unit/integration nhanh trên máy dev |
| **VNPay Config** | `VnPay:*` trống trong `appsettings.json` | Inject cấu hình test: `TmnCode="TESTTMN"`, `HashSecret="TEST_SECRET_KEY_1234567890"`, `BaseUrl="https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"`, `ReturnUrl="http://localhost/api/payment/vnpay-return"`, `MobileDeepLink="toantuduy://payment-result"` | Test đầy đủ `create-url`, `vnpay-ipn` (dùng helper tính HMAC-SHA512), và `vnpay-return` (`302 Redirect`) |
| **Background Workers** | 4 `IHostedService` chạy liên tục | Bỏ đăng ký `MonthlyPayslipEmailScheduler` & `UpcomingLessonReminderService` trong test chung (tránh chạy ngầm lúc đang Respawn DB); giữ `NotificationFanoutWorker` hoặc chủ động kích hoạt trong test Notification | Tránh flaky test & lỗi `ObjectDisposedException` khi DB reset |

### 2.4. Cấu trúc Project Test đề xuất (`Backend/ToanTuDuy.Tests`)

```text
Backend/
├── ToanTuDuy/
└── ToanTuDuy.Tests/
    ├── ToanTuDuy.Tests.csproj
    ├── Infrastructure/
    │   ├── TestWebApplicationFactory.cs      # Production & Development factories
    │   ├── DatabaseTestCollection.cs         # ICollectionFixture + Respawn reset
    │   ├── AuthTestHelper.cs                 # Register/Login/Seed Admin, Teacher, Parent + Bearer header
    │   ├── VnPayTestSigner.cs                # Tạo query string + vnp_SecureHash HMAC-SHA512
    │   ├── ApiAssertHelper.cs                # Parse cả { code, message } lẫn { message } & ValidationProblem
    │   └── Fakes/
    │       ├── FakeEmailDeliveryService.cs
    │       ├── FakeImageStorage.cs
    │       ├── FakeChatAttachmentStorage.cs
    │       ├── FakeCourseMediaStorage.cs
    │       └── InMemoryUnreadCountStore.cs
    ├── Suites/
    │   ├── Smoke/
    │   ├── AuthAndProfile/
    │   ├── UsersAndRbac/
    │   ├── Academic/
    │   ├── FinanceAndPayment/
    │   ├── AttendanceAndLeave/
    │   ├── QuestionBankAndAssignment/
    │   ├── PayrollAndReports/
    │   ├── ChatAndNotification/
    │   └── SecurityRegression/
```

---

## 3. Chiến lược Phân tầng & Thứ tự Triển khai

| Giai đoạn | Nhóm Test | Số lượng ước tính | Điều kiện chạy |
| :--- | :--- | :---: | :--- |
| **Phase 1 — Nền tảng & Smoke** | Test Factory, SQL Server + Respawn, Fake Services, Smoke + Auth/Profile/PIN | ~25 TCs | Chạy mọi PR (`< 30s`) |
| **Phase 2 — Core Business & RBAC** | Ma trận phân quyền, Course/Level/Class/Schedule/Lesson, Enrollment + VNPay IPN/Return | ~45 TCs | Chạy mọi PR |
| **Phase 3 — Learning, Payroll & Comms** | LeaveRequest, Attendance, QuestionBank, Assignment Practice/Grade, Payroll, AdminReports, Chat, Notifications | ~40 TCs | Chạy Nightly / Pre-merge |
| **Phase 4 — Concurrency & Security Regression** | Song song Submit Practice, VNPay IPN double-call, Security Gaps regression | ~15 TCs | Chạy Nightly / Release |

---

## 4. Rủi ro Kỹ thuật & Biện pháp Kiểm soát

1. **Known Security Gaps làm fail RBAC assertions:**
   - Như đã liệt kê ở [05-Security-Gaps-And-Code-Findings.md](../../../Review/Backend/05-Security-Gaps-And-Code-Findings.md), các controller `ParentsController` (`[AllowAnonymous]`), `ClassesController` (thiếu `[Authorize]`), `LessonsController` CRUD (thiếu `[Authorize]`), `DevSeedController`, `TeachersController`, `AttendancesController`, `AssignmentsController` (`GetByStudent`/`GetStudentSubmission`) đang mở quyền quá rộng.
   - **Quyết định cần chốt trước khi code test:** Fix phân quyền ở backend trước, hoặc đánh dấu các testcase RBAC tương ứng là `[Fact(Skip = "Known issue - xem 05-Security-Gaps")]` trong Phase 1 để không làm đỏ pipeline CI.
2. **Lệch múi giờ (`UtcNow` vs `UTC+7`):**
   - `LeaveRequestService` và `AdminReportService` tính theo `UtcNow.AddHours(7)`, trong khi `AssignmentService` và `ClassScheduleService` tính theo `UtcNow`.
   - Test data phải truyền rõ `DateTimeKind.Utc` / ISO `Z` và cộng bù `+7h` khi test `LeaveRequest` (quy tắc >= 2 tiếng trước `StartTime`).
3. **Lỗi trùng key `LeaveRequests` trong `AttendanceService`:**
   - Khi test luồng "Hủy đơn xin nghỉ → Tạo đơn mới → Xem điểm danh buổi học", có thể kích hoạt bug `ToDictionaryAsync` ở `AttendanceService.cs#L48`. Cần viết test case hồi quy riêng cho kịch bản này.
4. **Hành vi `ReservationCleanupService`:**
   - Hiện bị comment out trong `Program.cs#L179`, nên không viết test tự động chờ 2 phút hủy slot trên HTTP API cho đến khi service này được bật lại.
5. **Async Timing với `NotificationFanoutWorker` & `PayslipEmailWorker`:**
   - Dùng helper `EventuallyAsync(Func<Task<bool>>, timeout: 10s, interval: 250ms)` thay vì `Task.Delay` cố định.

---

## 5. Tiêu chí Nghiệm thu (Exit Criteria)
- **100%** các testcase ưu tiên `P0` và `P1` đạt Pass trên môi trường `ProductionWebAppFactory`.
- Không còn lỗi `500 Internal Server Error` ngoài dự kiến ở bất kỳ test case Negative/Boundary nào.
- Bộ `dotnet test` chạy ổn định 3 lần liên tiếp không flaky.
- Có báo cáo TRX/Coverage xuất tự động từ `dotnet test --collect:"XPlat Code Coverage"`.
