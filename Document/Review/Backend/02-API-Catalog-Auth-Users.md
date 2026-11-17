# 02. Danh mục API — Auth, Email Verification, Profile, Quản lý người dùng & DevSeed

Tài liệu chi tiết các API thuộc cụm xác thực, hồ sơ cá nhân, quản lý tài khoản và dữ liệu seed.

---

## 1. Auth (`AuthController` — `/api/auth`)

Nguồn: [`Controllers/AuthController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/AuthController.cs), [`Services/AuthService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/AuthService.cs), [`Services/PasswordResetService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/PasswordResetService.cs)

| Method | Endpoint | Auth | Request Body / Query | Response & Status Codes |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Public | `RegisterRequest`: `phone` (req), `password` (req, min 6), `role` (`"Parent"` \| `"Teacher"`), `fullName` (req), `email` (req nếu `Teacher`) | `200 OK`: `AuthResponse`<br>`400`: Role/Email/Identity password policy lỗi |
| `POST` | `/api/auth/login` | Public | `LoginRequest`: `phone` (req — map vào `UserName`), `password` (req) | `200 OK`: `AuthResponse`<br>`401`: Sai SĐT/mật khẩu hoặc corrupt hash |
| `POST` | `/api/auth/google-login` | `[AllowAnonymous]` | `GoogleLoginDto`: `idToken` (req) | `200 OK`: `AuthResponse`<br>`401`: Token sai / email chưa verify / tài khoản không tồn tại hoặc bị khóa<br>`503`: Chưa cấu hình `Google:ClientId` |
| `POST` | `/api/auth/change-password` | `[Authorize]` | `ChangePasswordRequest`: `currentPassword` (req), `newPassword` (req, min 6), `confirmNewPassword` (req, match `NewPassword`) | `200 OK`: `{ message }`<br>`401`: Sai mật khẩu hiện tại<br>`400`: Trùng mật khẩu cũ hoặc lỗi Identity |
| `POST` | `/api/auth/forgot-password` | `[AllowAnonymous]` | `ForgotPasswordRequest`: `email` (req, valid email) | `200 OK`: `{ message }` (trả chung kể cả khi email không tồn tại)<br>`400`: Email chưa xác minh (`EmailConfirmed == false`)<br>`429`: Vi phạm cooldown (`60s`) |

### Chi tiết hành vi & Lưu ý kiểm thử
- **`AuthResponse`**: `{ accessToken, refreshToken, role, profileId, email, fullName }`. Lưu ý: hiện tại **không có endpoint `/api/auth/refresh`** để đổi `refreshToken` lấy `accessToken` mới!
- **Register (`Teacher`)**: gán email vào Identity, phát hành `EmailVerificationToken`, tạo `Notification` (`ACCOUNT:{user.Id}:REGISTERED`) cho `AllAdmins`, và gửi mail xác minh sau khi commit (lỗi SMTP/Resend được log warning, không rollback đăng ký).
- **Login**:
  - `LoginRequest.Phone` thực chất tra cứu theo `ApplicationUser.UserName` (`_userManager.FindByNameAsync(request.Phone)`), do đó có thể đăng nhập bằng `"Admin123"` hoặc `"0901000000"`.
  - **Bất thường đã phát hiện:** Kiểm tra `if (!user.IsActive)` đang bị **comment out** ở [`AuthService.cs#L194-L197`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/AuthService.cs#L194-L197). Vì vậy user bị khóa vẫn login lấy được token, nhưng gọi endpoint `[Authorize]` sẽ bị chặn `401` tại `JwtBearerEvents.OnTokenValidated`.
- **ChangePassword**: Xóa `user.RefreshToken = null`, nhưng **không gọi `UpdateSecurityStampAsync`** (JWT hiện tại vẫn sống đến khi hết hạn `AccessTokenExpirationMinutes`).
- **ForgotPassword**: Chỉ hoạt động khi `EmailConfirmed == true`. Sinh mật khẩu tạm 12 ký tự (bỏ `I, O, l, 0, 1`), reset password ngay, xóa `RefreshToken`, cập nhật `PasswordResetSentAt` (cooldown `60s` → `429`). Nếu email không tồn tại trong DB vẫn trả `200` để tránh lộ danh sách email.

---

## 2. Xác minh Email (`EmailVerificationController` — `/api/email-verification`)

Nguồn: [`Controllers/EmailVerificationController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/EmailVerificationController.cs), [`Services/EmailVerificationService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/EmailVerificationService.cs)

| Method | Endpoint | Auth | Input | Output & Lưu ý |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/email-verification?token=...` | `[AllowAnonymous]` | Query `token` | `200 OK` **`text/html; charset=utf-8`** (kể cả khi lỗi `Expired` / `AlreadyUsed` / `EmailChanged` / `NotFound`!) |
| `POST` | `/api/email-verification` &<br>`POST` `/api/email-verification/resend` | `[Authorize]` | Không cần body (lấy `PendingEmail ?? Email` của user hiện tại) | `200 OK`: `{ message }`<br>`400`: Tài khoản chưa có email<br>`429`: Gửi lại trước `ResendCooldownSeconds` (`60s`) |

### Chi tiết hành vi & Lưu ý kiểm thử
- `Verify` dùng `ExecuteUpdateAsync` nguyên tử (`UsedAt == null`) để chống click đúp. Nếu là `PendingEmail` (luồng đổi email của Admin), sau khi verify thành công sẽ gọi `SetEmailAsync` và xóa `PendingEmail = null`, set `EmailConfirmed = true`.
- Vì `GET /api/email-verification` **luôn trả HTTP 200 HTML**, test tự động phải kiểm tra nội dung HTML (ví dụ tiêu đề `"Xác minh thành công"` vs `"Liên kết đã hết hạn"` / `"Liên kết đã được sử dụng"`) và kiểm chứng lại bằng `GET /api/profile` (`emailConfirm == true`).

---

## 3. Hồ sơ cá nhân & Mã PIN (`ProfileController` — `/api/profile`)

Nguồn: [`Controllers/ProfileController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/ProfileController.cs), [`Services/ProfileService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/ProfileService.cs), [`Services/PinService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/PinService.cs)

Tất cả endpoint đều yêu cầu `[Authorize]`.

| Method | Endpoint | Roles | Input | Response & Rules |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/profile` | Any authenticated (`Parent`/`Teacher`/`Admin`) | — | `200 OK`: `ProfileResponse` (`accountId, phone, role, fullName, email, pendingEmail, emailConfirm, avatar, hasPin`) |
| `PUT` | `/api/profile` | Any authenticated | `UpdateProfileRequest`: `fullName` (req, max 100), `phone` (optional, `[Phone]`, max 20 — chỉ Admin được cập nhật SĐT!) | `200 OK`: `ProfileResponse`<br>`409`: Trùng SĐT (Admin) |
| `PUT` | `/api/profile/email` | `Parent,Admin` | `UpdateEmailRequest`: `email` (req, email, max 256) | `200 OK`: `ProfileResponse`<br>Admin: lưu `PendingEmail` + gửi mail xác minh.<br>Parent: ghi đè `Email` (`SetEmailAsync`), gửi mail xác minh; nếu `EmailConfirmed == true` → `403` |
| `POST` | `/api/profile/avatar` | `Parent,Admin,Teacher` | Multipart `file` (`[RequestSizeLimit(6MB)]`; rule kiểm tra max `5MB`, `image/jpeg\|jpg\|png\|webp`) | `200 OK`: `ProfileResponse` (kèm URL Cloudinary)<br>`400`: Sai định dạng / >5MB<br>`503`/`502`: Lỗi cấu hình/gọi Cloudinary |
| `DELETE` | `/api/profile/avatar` | `Parent,Admin,Teacher` | — | `200 OK`: `ProfileResponse` (`avatar = null`) |
| `PUT` | `/api/profile/pin` | `Parent` | `SetPinRequest`: `newPin` (req, regex `^[0-9]{4,6}$`), `currentPin` (bắt buộc nếu đã có PIN) | `204 No Content`<br>`400`: Thiếu hoặc sai `currentPin` |
| `POST` | `/api/profile/pin/verify` | `Parent` | `VerifyPinRequest`: `pin` (req, regex `^[0-9]{4,6}$`) | `200 OK`: `{ valid: true \| false }`<br>`400`: Chưa thiết lập PIN |

---

## 4. Quản trị Tài khoản (`AdminAccountsController` — `/api/admin/accounts`)

Nguồn: [`Controllers/AdminAccountsController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/AdminAccountsController.cs) — Toàn bộ class có `[Authorize(Roles = "Admin")]`.

| Method | Endpoint | Query / Body | Response & Rules |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/admin/accounts` | Query: `keyword` (optional) | `200 OK`: `List<AdminAccountResponse>` |
| `GET` | `/api/admin/accounts/paged` | Query: `page=1`, `pageSize=20`, `keyword`, `role`, `isActive` (`bool?`) | `200 OK`: `AdminAccountPagedResponse` (gồm `activeCount`, `blockedCount` toàn hệ thống) |
| `PUT` | `/api/admin/accounts/{id}/status` | `UpdateAccountStatusRequest`: `isActive` (req `bool?`) | `200 OK`: `{ message, isActive }`<br>`404`: Không tìm thấy<br>`409`: Admin tự khóa chính mình (`id == User.GetAccountId() && !isActive`) |
| `POST` | `/api/admin/accounts/{id}/reset-password` | — | `200 OK`: `{ message }`<br>`400`: Không phải role `Teacher`/`Parent`, chưa có email hoặc email chưa xác minh<br>`502`: Đặt lại xong nhưng gửi mail lỗi |

> **Lưu ý kiểm thử:** `UpdateStatus` khóa tài khoản sẽ đặt `LockoutEnd = DateTimeOffset.MaxValue`, `RefreshToken = null` và gọi `UpdateSecurityStampAsync` → mọi JWT của tài khoản đó lập tức bị từ chối `401`.

---

## 5. Quản lý Phụ huynh (`ParentsController` — `/api/parents`)

Nguồn: [`Controllers/ParentsController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/ParentsController.cs), [`Services/ParentStudentService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/ParentStudentService.cs)

> [!WARNING]
> Class `ParentsController` hiện gắn đồng thời `[Authorize]` (dòng 10) và **`[AllowAnonymous]`** (dòng 11), dẫn tới bỏ qua kiểm tra JWT trên các endpoint của controller này! Xem chi tiết ở [05-Security-Gaps-And-Code-Findings.md](./05-Security-Gaps-And-Code-Findings.md).

| Method | Endpoint | Auth thực tế | Query / Body | Response & Lưu ý |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/parents` | Anonymous (do class attr) | `keyword` | `200 OK`: `List<ParentResponseDto>` (**chứa trường `pin`!**) |
| `GET` | `/api/parents/paged` | Anonymous | `page=1, pageSize=20, keyword, isActive` | `200 OK`: `PagedResult<ParentResponseDto>` |
| `GET` | `/api/parents/students` | Anonymous | `keyword` | `200 OK`: `List<StudentResponseDto>` |
| `GET` | `/api/parents/students/paged` | Anonymous | `page=1, pageSize=20, keyword, parentAccountId` | `200 OK`: `PagedResult<StudentResponseDto>` |
| `GET` | `/api/parents/{id}` | Anonymous | `id` (`ParentProfile.Id`) | `200 OK` / `404` |
| `POST` | `/api/parents` | Anonymous | `CreateParentDto` (`phone, password, fullName, pin?, avatar?`) | `201 Created` / `400 { message }` (try/catch) |
| `PUT` | `/api/parents/{id}` | Anonymous | `UpdateParentDto` (`fullName, pin?, avatar?`) | `200 OK` / `404` |
| `PUT` | `/api/parents/{id}/status` | `[Authorize(Roles = "Admin")]` (vẫn có metadata `[AllowAnonymous]` từ class!) | `UpdateParentStatusDto` (`isActive`) | `200 OK` / `404` |
| `PUT` | `/api/parents/{id}/email` | Anonymous | `UpdateParentEmailDto` (`email`) | `200 OK` / `404` / `400` / `403` / `409` |
| `DELETE` | `/api/parents/{id}` | Anonymous | `id` | `200 OK` / `404` (xóa luôn `StudentProfiles` và `ApplicationUser`, nhưng sẽ lỗi FK 500 nếu đã có `Invoice`/`ClassStudent`) |

---

## 6. Quản lý Học sinh của Phụ huynh (`StudentController` — `/api/students`)

Nguồn: [`Controllers/StudentController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/StudentController.cs), [`Services/StudentService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/StudentService.cs)

Toàn bộ controller yêu cầu `[Authorize(Roles = "Parent")]`, và mọi thao tác đều lọc theo ownership `s.Parent.AccountId == User.GetAccountId()`.

| Method | Endpoint | Filter đặc biệt | Input | Response & Rules |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/students` | — | — | `200 OK`: `List<StudentResponse>` |
| `GET` | `/api/students/{id}` | — | `id` (`StudentProfile.Id`) | `200 OK` / `404` (nếu không tồn tại hoặc không thuộc Parent hiện tại) |
| `POST` | `/api/students` | — | `CreateStudentRequest`: `fullName` (req, max 100) | `200 OK`: `StudentResponse` |
| `PUT` | `/api/students/{id}` | — | `UpdateStudentRequest`: `fullName` (req, max 100) | `200 OK`: `StudentResponse` / `404` |
| `POST` | `/api/students/{id}/avatar` | — | Form file (`RequestSizeLimit(6MB)`, rule `5MB`, JPG/PNG/WEBP) | `200 OK`: `StudentResponse` |
| `DELETE` | `/api/students/{id}/avatar` | — | — | `200 OK`: `StudentResponse` |
| `DELETE` | `/api/students/{id}` | **`[RequirePin]`** (`X-Pin` header) | Header `X-Pin` | `204 No Content`<br>`400`: Thiếu `X-Pin` hoặc chưa set PIN<br>`403`: Sai PIN<br>`409`: Học sinh đã có dữ liệu liên quan (`ClassStudent`, `Invoice`, `StudentAssignment`, `Attendance`, `LeaveRequest`, `StudentBadge`) |

---

## 7. Quản lý Giáo viên (`TeachersController` — `/api/teachers`)

Nguồn: [`Controllers/TeacherController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/TeacherController.cs), [`Services/TeacherService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/TeacherService.cs)

Class có `[Authorize]` (không giới hạn Role ở cấp class — tức mọi user đăng nhập đều gọi được `Create`, `Update`, `Delete`!).

| Method | Endpoint | Roles | Query / Body | Response & Lưu ý |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/teachers` | Any auth | `keyword` | `200 OK`: `List<TeacherResponseDto>` |
| `GET` | `/api/teachers/paged` | Any auth | `page=1, pageSize=20, keyword, isActive` | `200 OK`: `PagedResult<TeacherResponseDto>` |
| `GET` | `/api/teachers/{id}` | Any auth | `id` (`TeacherProfile.Id`) | `200 OK` / `404` |
| `POST` | `/api/teachers` | Any auth (thiếu `Roles = "Admin"`) | `CreateTeacherDto` (`phone, password, fullName, email?`) | `201 Created` / `400 { message }` |
| `PUT` | `/api/teachers/{id}` | Any auth (thiếu `Roles = "Admin"`) | `UpdateTeacherDto` (`fullName, email?, specialize?`) | `200 OK` / `404` |
| `PUT` | `/api/teachers/{id}/status` | **`Admin`** | `UpdateTeacherStatusDto` (`isActive`) | `200 OK` / `404` (cập nhật `IsActive`, `LockoutEnd`, `RefreshToken`, `SecurityStamp`) |
| `DELETE` | `/api/teachers/{id}` | Any auth (thiếu `Roles = "Admin"`) | `id` | `200 OK` / `404` (lỗi FK nếu giáo viên đang gắn vào `Classes` hoặc `PayrollSummary`) |

---

## 8. Seeding Dữ liệu Dev (`DevSeedController` — `/api/dev/seed-data`)

Nguồn: [`Controllers/DevSeedController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/DevSeedController.cs)

| Method | Endpoint | Auth | Query | Chức năng & Cảnh báo |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/dev/seed-data/bogus` | `[AllowAnonymous]` | `rowsPerTable=1000` (`1..10000`), `cleanExisting=true` | Chạy `BogusDataSeeder.SeedAsync`. **CẢNH BÁO:** Không check `IsDevelopment()` và không cần Auth; mặc định `cleanExisting=true` sẽ xóa sạch dữ liệu DB hiện tại! |
| `POST` | `/api/dev/seed-data/bogus/export-sql` | `[AllowAnonymous]` | `rowsPerTable=1000` (`1..10000`), `fileName?` | Xuất SQL seed ra `AppContext.BaseDirectory` |
