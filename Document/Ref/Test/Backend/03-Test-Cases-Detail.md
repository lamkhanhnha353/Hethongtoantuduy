# 03. Đặc tả Test Cases Chi tiết (xUnit + WebApplicationFactory)

Tài liệu này mô tả chi tiết **Test Data, Request, Preconditions và Assertions** cho các test case trọng yếu (ưu tiên `P0`/`P1`) để triển khai trực tiếp trong `ToanTuDuy.Tests`. Danh sách đầy đủ **98 Test Cases** dạng bảng tra cứu nhanh nằm ở [03-Test-Cases.csv](./03-Test-Cases.csv).

---

## Quy ước chung trong Test Assertions

1. **Error Helper (`ApiAssertHelper`):**
   - Với các controller đi qua `GlobalExceptionHandler`: JSON có `{ "code": <int>, "message": "<string>", "details": <string?> }`.
   - Với các controller tự `try/catch` (`Courses`, `CourseLevels`, `Classes`, `Lessons`, `Parents`, `Teachers`, `Attendances`): JSON có `{ "message": "<string>" }`.
   - Với ModelState validation (`[ApiController]`): status `400`, JSON chuẩn `ValidationProblemDetails` chứa key `"errors"`.
2. **Polling Async Workers:** dùng `EventuallyAsync(..., timeout: TimeSpan.FromSeconds(10), interval: TimeSpan.FromMilliseconds(250))` cho `NotificationFanoutWorker` và `PayslipEmailWorker`.

---

## 1. Cụm Auth, Email Verification & Profile (`TC-AUTH-*`, `TC-PROF-*`)

### `TC-AUTH-001` — Đăng ký `Parent` thành công & tạo Outbox Notification cho Admin
- **Endpoint:** `POST /api/auth/register` (`Anonymous`)
- **Input JSON:**
  ```json
  { "phone": "0981000001", "password": "Password@123", "role": "Parent", "fullName": "Nguyễn Phụ Huynh" }
  ```
- **Assertions:**
  - HTTP `200 OK`, JSON có `accessToken` (non-empty JWT), `refreshToken`, `role == "Parent"`, `profileId > 0`.
  - DB: tồn tại `ACCOUNT` (`UserName == "0981000001"`, `IsActive == true`), `PARENT_PROFILE` tương ứng, và bản ghi `NOTIFICATION` (`Type == "ACCOUNT"`, `RefId == $"ACCOUNT:{userId}:REGISTERED"`) + `OUTBOX`.

### `TC-AUTH-003` — Đăng ký `Teacher` thiếu `Email` bị từ chối `400`
- **Endpoint:** `POST /api/auth/register` (`Anonymous`)
- **Input JSON:**
  ```json
  { "phone": "0981000002", "password": "Password@123", "role": "Teacher", "fullName": "Giáo Viên A", "email": null }
  ```
- **Assertions:** HTTP `400 Bad Request`, `code == 400`, `message` chứa `"Email là thông tin bắt buộc đối với Teacher"`.

### `TC-AUTH-007` — Xác minh Email thành công & Idempotency khi click lặp lại
- **Preconditions:** Đăng ký `Teacher` với `email = "gv.verify@test.local"`.
- **Steps & Assertions:**
  1. Lấy `EmailDeliveryMessage` từ `FakeEmailDeliveryService.SentEmails`, trích URL `/api/email-verification?token=...`.
  2. Gọi `GET /api/email-verification?token={rawToken}` lần 1 → HTTP `200`, `Content-Type` chứa `text/html`, body chứa `"Xác minh thành công"`.
  3. Kiểm tra `GET /api/profile` (bằng JWT Teacher) → `emailConfirm == true` (hoặc kiểm tra DB `ACCOUNT.EmailConfirmed == true`).
  4. Gọi `GET /api/email-verification?token={rawToken}` lần 2 → HTTP `200`, body chứa `"Liên kết đã được sử dụng"`.

### `TC-AUTH-012` — `ForgotPassword` End-to-End & Cooldown `429`
- **Preconditions:** Tạo tài khoản đã `EmailConfirmed = true`, `Email = "reset.user@test.local"`.
- **Steps & Assertions:**
  1. Gọi `POST /api/auth/forgot-password` lần 1 `{ "email": "reset.user@test.local" }` → HTTP `200 OK`.
  2. Gọi ngay lần 2 `< 60s` → HTTP `429 Too Many Requests`.
  3. Đọc mật khẩu tạm từ `FakeEmailDeliveryService.SentEmails.Last().TextBody` (regex `Mật khẩu tạm của bạn:\s*(\S+)`).
  4. Gọi `POST /api/auth/login` với mật khẩu cũ → `401`; với mật khẩu tạm → `200 OK`.

### `TC-AUTH-014` — Thu hồi JWT động qua `SecurityStamp` khi Admin khóa tài khoản
- **Steps & Assertions:**
  1. Login tài khoản `Parent` lấy `parentToken`. Gọi `GET /api/profile` với `parentToken` → `200 OK`.
  2. Admin gọi `PUT /api/admin/accounts/{parentAccountId}/status` với `{ "isActive": false }` → `200 OK`.
  3. Dùng lại `parentToken` cũ gọi `GET /api/profile` → `401 Unauthorized` (do `JwtBearerEvents.OnTokenValidated` kiểm tra `IsActive` & `SecurityStamp`).

### `TC-PROF-005` — Xóa học sinh với `[RequirePin]` (`X-Pin` header)
- **Preconditions:** `Parent` đã set PIN `"123456"` qua `PUT /api/profile/pin`, tạo 2 học sinh: `studentClean` (chưa có lớp/hóa đơn) và `studentEnrolled` (đã ghi danh).
- **Assertions:**
  - `DELETE /api/students/{studentClean.Id}` không có header `X-Pin` → `400 BadRequest` (`"Thiếu mã PIN"`).
  - `DELETE /api/students/{studentClean.Id}` với `X-Pin: 999999` → `403 Forbidden` (`"Mã PIN không đúng"`).
  - `DELETE /api/students/{studentEnrolled.Id}` với `X-Pin: 123456` → `409 Conflict`.
  - `DELETE /api/students/{studentClean.Id}` với `X-Pin: 123456` → `204 No Content`.

---

## 2. Cụm Đào tạo & Sinh lịch tự động (`TC-ACAD-*`)

### `TC-ACAD-009` — Tạo `ClassSchedule` tự động sinh `Lesson` từ `Syllabus` & tính `EndDate`
- **Preconditions:** `ProductionWebAppFactory`. Tạo `Course`, `CourseLevel` (`TotalSessions = 4`, `Syllabus = [{"title":"Buổi A"},{"title":"Buổi B"}]`), `Class` (`StartDate = DateTime.UtcNow.Date.AddDays(1)`).
- **Steps & Assertions:**
  1. Admin gọi `POST /api/classschedules` (`dayOfWeek` khớp ngày mai, `09:00:00` → `10:30:00`, `room: "P101"`) → `201 Created`.
  2. Gọi `GET /api/lessons/class/{classId}` → có đúng `4` buổi `SCHEDULED` cách nhau 7 ngày.
  3. Gọi `GET /api/classes/{classId}` → `endDate` bằng ngày của buổi thứ 4.
  4. Parent (có HS `ACTIVE` trong lớp) gọi `GET /api/lessons/student/{studentId}/roadmap` → kiểm tra `totalSessions == 4`, buổi 1 `status == "IN_PROGRESS"` (`title == "Buổi A"`), buổi 2 `status == "LOCKED"` (`title == "Buổi B"`), buổi 3–4 `title == "Buổi học 3"`, `"Buổi học 4"`.

### `TC-ACAD-010` — Kiểm tra trùng lịch lớp / phòng / giáo viên (`409 Conflict`)
- **Preconditions:** Lớp `C1` (GV `T1`) đã có lịch Thứ 2 `09:00–10:30` phòng `P101`. Lớp `C2` có GV `T1` (và lớp `C3` có GV `T2`).
- **Assertions:**
  - Tạo lịch cho `C1` Thứ 2 `10:00–11:00` phòng `P102` → `409` (trùng ca cùng lớp).
  - Tạo lịch cho `C3` Thứ 2 `09:30–11:00` phòng `P101` → `409` (trùng phòng `P101`).
  - Tạo lịch cho `C2` Thứ 2 `09:30–11:00` phòng `P202` → `409` (trùng lịch GV `T1`).

---

## 3. Cụm Ghi danh & Thanh toán VNPay (`TC-FIN-*`)

### `TC-FIN-004` & `TC-FIN-005` & `TC-FIN-006` — Luồng VNPay IPN đầy đủ & Idempotency
- **Preconditions:** `CourseLevel.Fee = 1500000`, `Class.MaxCapacity = 10`. Parent gọi `POST /api/enrollment/register` nhận `invoiceId`. Sau đó gọi `POST /api/payment/create-url/{invoiceId}` để lấy `paymentUrl` và trích `vnp_TxnRef` (`INV...`), `vnp_Amount` (`150000000`).
- **Steps & Assertions:**
  1. `GET /api/payment/vnpay-ipn` với `vnp_SecureHash = "wrong"` → `200 OK`, `{ "RspCode": "97", "Message": "Invalid signature" }`.
  2. Ký đúng `HMACSHA512` bằng `VnPay:HashSecret` nhưng `vnp_Amount = 100` → `200 OK`, `{ "RspCode": "04", "Message": "Invalid amount" }`.
  3. Ký đúng với `vnp_ResponseCode = "24"`, `vnp_TransactionStatus = "02"` → `200 OK`, `{ "RspCode": "00" }`. DB: `Invoice.Status == "FAILED"`, `ClassStudent.Status == "PENDING"`.
  4. Gọi lại `POST /api/payment/create-url/{invoiceId}` → `200 OK` (tự khôi phục `Invoice.Status = "PENDING"` và đổi `TxnRef` mới!).
  5. Ký đúng với `TxnRef` mới, `vnp_ResponseCode = "00"`, `vnp_TransactionStatus = "00"`, `vnp_TransactionNo = "14000001"` → `200 OK`, `{ "RspCode": "00", "Message": "Confirm Success" }`.
  6. Kiểm tra DB: `Invoice.Status == "PAID"`, `PaymentMethod == "VNPAY"`, `ClassStudent.Status == "ACTIVE"`. Kiểm tra `FakeEmailDeliveryService` nhận email cho PH & GV.
  7. Gọi lại chính query IPN bước 5 lần 2 → `200 OK`, `{ "RspCode": "02", "Message": "Order already confirmed" }`.

---

## 4. Cụm Xin nghỉ, Điểm danh, Bài tập & Lương (`TC-ATT-*`, `TC-ASGN-*`, `TC-PAY-*`)

### `TC-ATT-004` — Duyệt đơn xin nghỉ tự động lưu điểm danh `EXCUSED`
- **Preconditions:** Lesson `SCHEDULED` có `StartTime = UtcNow.AddHours(7 + 5)` (cách giờ VN hiện tại 5 tiếng). HS `ACTIVE` trong lớp.
- **Steps & Assertions:**
  1. Parent gọi `POST /api/leaverequests` → `200 OK`, lấy `leaveRequestId`.
  2. Teacher không dạy lớp gọi `PUT /api/leaverequests/{id}/process?isApproved=true` → `404`.
  3. Teacher dạy lớp gọi `PUT /api/leaverequests/{id}/process?isApproved=true` → `200 OK`.
  4. Gọi `GET /api/attendances/lesson/{lessonId}` → học sinh có `status == "EXCUSED"`, `hasLeaveRequest == true`, `leaveRequestStatus == "APPROVED"`.

### `TC-ASGN-005` & `TC-ASGN-006` — Nộp bài Practice tự chấm 4 loại câu & Concurrency
- **Preconditions:** Tạo Assignment gồm 4 câu hỏi (mỗi câu `2.5` điểm, tổng `10`):
  1. `MULTIPLE_CHOICE`: `"2 + 3 = ? A. 4 B. 5 C. 6 D. 7"`, `Answer = "B. 5"` → trả lời `"b"`.
  2. `TRUE_FALSE`: `"5 > 3 là đúng hay sai?"`, `Answer = "Đúng vì 5 lớn hơn 3"` → trả lời `"đúng"`.
  3. `FILL_IN`: `"Điền số thích hợp: 10 - 4 = ?"`, `Answer = "6\nGiải thích: ..."` → trả lời `" 6 "`.
  4. `TIMED_SEQUENCE`: `"Tính nhẩm theo nhịp: 5, +3, -2"`, `Answer = "6"` → trả lời `"99"` (cố tình sai 1 câu).
- **Steps & Assertions:**
  1. `GET /api/assignments/{id}/student/{studentId}/practice` → `200 OK`, câu 1 có `options.Count == 4`, câu 4 có `sequenceTerms.Count == 3`, không có field `answer`.
  2. `POST .../practice/submit` với 4 câu trả lời trên → `200 OK`, `score == 7.5`, `totalPoints == 10`, `correctCount == 3`, `totalQuestions == 4`, `status == "SUBMITTED"`.
  3. (Concurrency test với Assignment khác chưa nộp): dùng `Task.WhenAll` gửi 3 request `submit` cùng lúc → assert `Count(r => r.StatusCode == 200) == 1` và `Count(r => r.StatusCode == 400) == 2`.

### `TC-PAY-002` — Tổng hợp lương tháng chỉ tính buổi `COMPLETED` có điểm danh
- **Preconditions:** Teacher `T1` có `BaseRatePerSession = 250000`, `Bonus = 100000`, `Penalty = 50000` trong tháng M/Y. Có 3 buổi trong tháng M/Y:
  - Buổi 1: `Status = "COMPLETED"`, có 1 bản ghi `Attendance`. (Đủ điều kiện)
  - Buổi 2: `Status = "COMPLETED"`, **không** có `Attendance`. (Không đủ điều kiện)
  - Buổi 3: `Status = "SCHEDULED"`, có 1 bản ghi `Attendance`. (Không đủ điều kiện)
- **Steps & Assertions:**
  - Admin gọi `GET /api/payrolls/admin/summary?month={M}&year={Y}` → `totalValidSessions == 1`, `baseSalary == 250000`, `totalBonus == 100000`, `totalPenalty == 50000`, `finalSalary == 300000`.
