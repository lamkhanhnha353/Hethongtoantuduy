# 05. Phát hiện về Bảo mật, Phân quyền & Bất nhất Code (Code Findings)

Trong quá trình review mã nguồn phục vụ thiết kế kiểm thử API tự động, các vấn đề dưới đây **được quan sát trực tiếp trong code hiện tại**. Team QA/Dev cần thống nhất xem điểm nào sẽ **sửa trước khi viết test** hay **ghi nhận làm known-issue / test case hồi quy**.

---

## 1. Các lỗ hổng Phân quyền & Bảo mật nghiêm trọng (Critical / High)

### 1.1. `ParentsController` bị vô hiệu hóa xác thực do `[AllowAnonymous]` ở cấp Class
- **Vị trí:** [`Controllers/ParentsController.cs#L10-L11`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/ParentsController.cs#L10-L11)
- **Hiện trạng:** Class gắn đồng thời `[Authorize]` và `[AllowAnonymous]`. Trong ASP.NET Core, `AllowAnonymousAttribute` có mặt trong endpoint metadata sẽ bỏ qua authorization filter/middleware (kể cả trên các action CRUD và rò rỉ dữ liệu phụ huynh/học sinh).
- **Rò rỉ dữ liệu nhạy cảm kèm theo:** [`ParentStudentService.cs#L76`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/ParentStudentService.cs#L76) và `#L110` trả về trường `Pin = p.Pin` trong `ParentResponseDto`. Ngoài ra `CreateParentAsync` / `UpdateParentAsync` lưu `dto.Pin` **dưới dạng plaintext** (không hash bằng `IPasswordHasher<ParentProfile>` như `PinService`).

### 1.2. `ClassesController` hoàn toàn không có `[Authorize]`
- **Vị trí:** [`Controllers/ClassesController.cs#L9-L135`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/ClassesController.cs#L9-L135)
- **Hiện trạng:** Bất kỳ client nào không có JWT đều có thể `POST /api/classes`, `PUT /api/classes/{id}`, `DELETE /api/classes/{id}`, `POST /api/classes/{id}/students`, `DELETE /api/classes/{id}/students/{studentId}`.

### 1.3. `LessonsController` mở công khai toàn bộ CRUD
- **Vị trí:** [`Controllers/LessonsController.cs#L14-L115`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/LessonsController.cs#L14-L115)
- **Hiện trạng:** Chỉ `GetStudentLessons` và `GetStudentRoadmaps` có `[Authorize(Roles = "Parent")]`. Các endpoint `GET /api/lessons*`, `POST /api/lessons`, `PUT /api/lessons/{id}`, `DELETE /api/lessons/{id}` đều không có `[Authorize]`.

### 1.4. `DevSeedController` mở `[AllowAnonymous]` không kiểm tra môi trường
- **Vị trí:** [`Controllers/DevSeedController.cs#L24-L49`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/DevSeedController.cs#L24-L49)
- **Hiện trạng:** `POST /api/dev/seed-data/bogus` mặc định `cleanExisting = true`, sẽ **xóa sạch toàn bộ dữ liệu DB** rồi nạp dữ liệu Bogus. Không kiểm tra `IHostEnvironment.IsDevelopment()` và không có `[Authorize(Roles = "Admin")]`.

### 1.5. Thiếu Role check & Ownership check (IDOR) trên nhiều Controller
| Controller / Endpoint | Vị trí | Vấn đề |
| :--- | :--- | :--- |
| `TeachersController` (`POST`, `PUT`, `DELETE`) | [`TeacherController.cs#L46-L84`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/TeacherController.cs#L46-L84) | Chỉ có `[Authorize]` ở class level (chỉ `UpdateStatus` có `Roles = "Admin"`). `Parent` hoặc `Teacher` đều tạo/sửa/xóa được Teacher khác |
| `AttendancesController` (tất cả action) | [`AttendancesController.cs#L14`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/AttendancesController.cs#L14) | Chỉ có `[Authorize]`. `Parent` có thể gọi `POST /api/attendances/batch` để tự điểm danh hoặc `GET` lịch sử điểm danh của học sinh bất kỳ |
| `AssignmentsController.GetById` | [`AssignmentsController.cs#L35-L44`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/AssignmentsController.cs#L35-L44) | `Parent` gọi `GET /api/assignments/{id}` sẽ nhận được `AssignmentDetailResponse` **chứa đáp án `Answer` của toàn bộ câu hỏi**! |
| `AssignmentsController.GetByStudent` & `GetStudentSubmission` | [`AssignmentsController.cs#L133-L139`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/AssignmentsController.cs#L133-L139), [`#L216-L226`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/AssignmentsController.cs#L216-L226) | Cho phép `Parent` nhưng service không kiểm tra `studentId` có thuộc Parent đang đăng nhập hay không (IDOR), và `GetStudentSubmission` trả kèm đáp án `Answer` |
| `QuestionBanksController` (`GET` endpoints) | [`QuestionBanksController.cs#L12`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/QuestionBanksController.cs#L12) | `Parent` đọc được toàn bộ ngân hàng câu hỏi kèm `Answer` |
| `import-worker` | [`import-worker/app/api/routes.py`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/import-worker/app/api/routes.py) | Không có JWT/API key auth; `/api/parser/process-path` nhận bất kỳ `file_path` cục bộ nào trên server |

### 1.6. Policy `"AdminOrDevelopmentTesting"`
- **Vị trí:** [`Program.cs#L123-L126`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Program.cs#L123-L126)
- Trong môi trường `Development`, `ClassSchedulesController` (`POST/PUT/DELETE`) cho phép truy cập không cần đăng nhập hoặc bất kỳ role nào. Khi chạy automated test cho RBAC của `ClassSchedulesController`, phải chạy server với `ASPNETCORE_ENVIRONMENT=Staging` hoặc `Production`.

---

## 2. Bug Logic & Bất nhất Dữ liệu ảnh hưởng trực tiếp tới Test Cases

1. **`AuthService.LoginAsync` comment out kiểm tra `IsActive`** ([`AuthService.cs#L194-L197`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/AuthService.cs#L194-L197)):
   - Tài khoản đã bị khóa (`IsActive == false`) vẫn login được qua `POST /api/auth/login` (`200 OK`), nhưng khi dùng token đó gọi các endpoint `[Authorize]` thì `OnTokenValidated` trả `401`. Trong khi `GoogleLoginAsync` (dòng 273) có kiểm tra `!user.IsActive` và ném `401`.
2. **`AttendanceService.GetAttendanceByLessonIdAsync` lỗi trùng key `LeaveRequests`** ([`AttendanceService.cs#L46-L48`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/AttendanceService.cs#L46-L48)):
   - Dùng `.ToDictionaryAsync(lr => lr.StudentId)` trên toàn bộ `LeaveRequests` của `lessonId` mà không lọc `lr.Status != "CANCELLED"`. Nếu phụ huynh tạo đơn → hủy đơn (`CANCELLED`) → tạo đơn mới cho cùng buổi học đó, gọi `GET /api/attendances/lesson/{lessonId}` sẽ ném `ArgumentException` (`500`).
3. **`LessonService.CreateLessonAsync` / `UpdateLessonAsync` không gán `Title`** ([`LessonService.cs#L265-L273`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/LessonService.cs#L265-L273)):
   - `LessonRequest` không có trường `Title`, và `LessonResponse` cũng không trả về `Title` (dù DB `LESSON.title` có cột này và `StudentLessonResponse` / `ClassScheduleService` có sử dụng).
   - Lưu ý tên property bị sai chính tả là `IsImportan` trong `Lesson`, `LessonRequest`, `LessonResponse` (nhưng lại là `IsImportant` trong `StudentLessonResponse`).
4. **`ReservationCleanupService` bị comment out trong `Program.cs`** ([`Program.cs#L179`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Program.cs#L179)):
   - Hóa đơn `PENDING` quá `ReservationSettings:ExpirationMinutes` (2 phút) sẽ **không tự động bị hủy** để nhả slot trừ khi job này được bật lại.
5. **Chính tả trạng thái `"CANCELED"` vs `"CANCELLED"`**:
   - `ClassScheduleService` gán `Lesson.Status = "CANCELED"` (1 chữ L), nhưng kiểm tra cả `"CANCELED"` và `"CANCELLED"`.
   - `InvoiceService` và `LeaveRequestService` dùng `"CANCELLED"` (2 chữ L).
6. **`PaymentController.PaymentReturn` lỗi 400 nếu thiếu config `VnPay:MobileDeepLink`**:
   - Trong [`appsettings.json#L58`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/appsettings.json#L58), `VnPay:MobileDeepLink` đang là `""`. Khi gọi `GET /api/payment/vnpay-return`, controller sẽ ném `InvalidOperationException` (`400`) thay vì `302 Redirect` nếu môi trường test không cấu hình giá trị này.
7. **Không có endpoint Refresh Token**:
   - `AuthResponse` trả về `refreshToken` và DB lưu `ACCOUNT.refresh_token`, nhưng `AuthController` không có API `POST /api/auth/refresh` hoặc `logout`.
8. **Chuẩn hóa Format Lỗi không đồng nhất giữa các Controller**:
   - Các controller mới (`Auth`, `Profile`, `AdminAccounts`, `Student`, `Enrollment`, `Invoice`, `Payment`, `Payrolls`, `AdminReports`, `Chat`, `Notifications`, `AdminNotifications`, `QuestionBanks`, `Assignments`, `LeaveRequests`, `ClassSchedules`): exception đi qua `GlobalExceptionHandler` → `{ "code": int, "message": string, "details": string? }`.
   - Các controller có `try/catch` thủ công (`Courses`, `CourseLevels`, `Classes`, `Lessons`, `Parents`, `Teachers`, `Attendances`): trả trực tiếp `{ "message": string }` (hoặc `{ "message": string, "error": string }` ở `AttendancesController` khi 500), không có trường `code`.
9. **Chính sách khóa bí mật & Secrets mặc định trong `appsettings.json` / `docker-compose.yml`**:
   - `appsettings.json` hiện chứa trực tiếp `Jwt:Key`, `Cloudinary:CloudName/ApiKey/ApiSecret`, `Google:ClientId`. Khi đưa vào pipeline CI/CD test, cần override bằng biến môi trường test riêng (tránh upload rác lên Cloudinary production!).
