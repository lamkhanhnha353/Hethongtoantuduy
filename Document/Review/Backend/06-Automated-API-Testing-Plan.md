# 06. Kế hoạch Kiểm thử API Tự động (Automated API Testing Plan)

Tài liệu này đề xuất kế hoạch triển khai bộ kiểm thử API tự động cho cả `ToanTuDuy` (`.NET 10`) và `import-worker` (`FastAPI`) dựa trên đúng hợp đồng API, state machine và các phụ thuộc hạ tầng đã rà soát.

---

## 1. Mục tiêu & Phạm vi kiểm thử

- **Đối tượng kiểm thử:**
  1. `ToanTuDuy` REST API (25 controllers, ~105 endpoints) + SignalR (`/chathub`, `/hubs/notifications` — kiểm tra handshake/auth).
  2. `import-worker` REST API (6 endpoints xử lý `.docx`/`.pdf` và polling job).
- **Không nằm trong phạm vi phase 1 (hoặc tách suite riêng):** Load/Stress test (`DevSeed` 1000 dòng/bảng có thể tận dụng cho perf test sau).

---

## 2. Chiến lược Môi trường & Cô lập Phụ thuộc Ngoài

Để bộ test tự động chạy **nhanh, lặp lại được (deterministic) và không phụ thuộc internet/dịch vụ trả phí**:

| Phụ thuộc ngoài | Hành vi thực trong Backend | Giải pháp cho Môi trường Test Tự động |
| :--- | :--- | :--- |
| **SQL Server** | `ToanTuDuyDb` | Container SQL Server test riêng hoặc DB `ToanTuDuyDb_Test`; chạy EF Core migrations trước suite; mỗi E2E flow tạo dữ liệu mới với prefix/SĐT unique (ví dụ `0988xxxxxx`) hoặc reset DB trước mỗi run |
| **ASPNETCORE_ENVIRONMENT** | `Development` mở policy `AdminOrDevelopmentTesting` và `POST /api/notifications/test` | Tách 2 profile chạy test:<br>- **Production/Staging**: test chặt chẽ RBAC/Security Matrix.<br>- **Development** (hoặc flag test): test `/api/notifications/test` |
| **Redis** | `RedisUnreadCountStore` tự fallback sang SQL nếu Redis lỗi (`abortConnect=false`) | Nên bật container Redis test để test đúng luồng cache increment/decrement/reset; thêm 1 test case tắt Redis để verify fallback SQL |
| **VNPay** | Xác thực chữ ký `HMACSHA512` với `VnPay:HashSecret` | Cấu hình `VnPay:HashSecret`, `VnPay:TmnCode`, `VnPay:BaseUrl`, `VnPay:ReturnUrl`, `VnPay:MobileDeepLink` giả lập trong test env; viết helper ký `vnp_SecureHash` ngay trong test runner để gọi thật `vnpay-ipn` và `vnpay-return` |
| **Email (SMTP / Resend)** | `EmailDeliveryService` ưu tiên `Smtp:*` nếu cấu hình đủ | Dùng **Mailpit** hoặc **Papercut/MailHog** (SMTP container port `1025`, REST API đọc inbox) → giúp test E2E đọc được **magic link xác minh email**, **mật khẩu tạm ForgotPassword**, **email phiếu lương** hoàn toàn tự động! |
| **Import Worker LLM** | Hỗ trợ `LLM_PROVIDER=gemini \| openai \| mock` | Đặt `LLM_PROVIDER=mock` khi chạy automated test để test toàn bộ pipeline extract → chunk → parse → bulk import vào `ToanTuDuy` mà không tốn quota AI |
| **Cloudinary** | Upload avatar, course media, chat attachment | Dùng tài khoản Cloudinary sandbox riêng cho test (có cleanup sau test) hoặc bật test suite riêng cho upload; kiểm tra validation file (sai MIME, vượt dung lượng) hoàn toàn offline vì backend reject trước khi gọi Cloudinary |

---

## 3. Phân tầng Bộ kiểm thử (Test Pyramid & Suites)

| Tầng | Suite | Mục tiêu | Tần suất chạy |
| :--- | :--- | :--- | :--- |
| **Tầng 1** | **Smoke & Health** | Kiểm tra API sống, DB connect, OpenAPI, `import-worker /health`, login 3 role mẫu (`Admin`, `Teacher`, `Parent`) | Mỗi commit / PR |
| **Tầng 2** | **Auth, Security & RBAC Matrix** | Kiểm tra `401` khi thiếu/sai JWT, thu hồi JWT qua `SecurityStamp` khi khóa TK/reset password, `[RequirePin]` (`400`/`403`), ma trận Role × Endpoint | Mỗi PR |
| **Tầng 3** | **Validation, Pagination & Contract** | Kiểm tra boundary input (`ModelState` 400, `Range`, `RegularExpression`), `PagedResult<T>` (`page<=0`, `pageSize>100`, `totalPages`), error schema | Mỗi PR |
| **Tầng 4** | **Stateful E2E Business Workflows** | Kiểm tra các luồng nghiệp vụ liên hoàn đa bước, side effects (DB, Outbox notification, Email, Attendance, Payroll) | Nightly / Pre-merge |
| **Tầng 5** | **Concurrency & Idempotency** | Nộp bài practice song song, gọi `vnpay-ipn` lặp lại (`RspCode=02`), click verify email 2 lần (`AlreadyUsed`), tạo conversation song song | Nightly / Release |

---

## 4. Ma trận Phân quyền (RBAC Test Matrix) cần kiểm chứng

> [!NOTE]
> Các ô đánh dấu **(!)** là các endpoint đang có lỗ hổng auth/role nêu ở [05-Security-Gaps-And-Code-Findings.md](./05-Security-Gaps-And-Code-Findings.md). Cần xác nhận fix code trước hoặc đánh dấu `xfail` tạm thời.

| Nhóm Endpoint | Anonymous | Parent | Teacher | Admin |
| :--- | :---: | :---: | :---: | :---: |
| `POST /api/auth/register`, `login`, `google-login`, `forgot-password` | `200/400/401` | `—` | `—` | `—` |
| `POST /api/auth/change-password` | `401` | `200` | `200` | `200` |
| `GET /api/profile`, `PUT /api/profile`, `POST/DELETE /api/profile/avatar` | `401` | `200` | `200` | `200` |
| `PUT /api/profile/email` | `401` | `200` | `403` | `200` |
| `PUT /api/profile/pin`, `POST /api/profile/pin/verify` | `401` | `200/204` | `403` | `403` |
| `/api/admin/accounts/*`, `/api/admin/reports/*`, `/api/admin/invoices*`, `/api/admin/notifications*` | `401` | `403` | `403` | `200/202` |
| `POST/PUT/DELETE /api/courses*`, `/api/courselevels*` | `401` | `403` | `403` | `200/201` |
| `GET /api/courses*`, `/api/courselevels*` | `401` | `200` | `200` | `200` |
| `/api/students/*` (riêng `DELETE` cần thêm header `X-Pin`) | `401` | `200/204` | `403` | `403` |
| `/api/enrollment/*`, `GET /api/invoice/history`, `POST /api/invoice/cancel/*`, `POST /api/payment/create-url/*` | `401` | `200` | `403` | `403` |
| `GET /api/payment/vnpay-ipn`, `vnpay-return` | `200/302` | `—` | `—` | `—` |
| `POST/PUT/DELETE /api/question-banks*`, `/api/assignments`, `/clone`, `/grade/*`, `/batch-grade` | `401` | `403` | `200/201` | `200/201` |
| `GET/POST /api/assignments/*/practice*` | `401` | `200` | `403` | `403` |
| `POST/DELETE /api/leaverequests` & `GET /api/leaverequests/student/*` | `401` | `200/204` | `403` | `403` |
| `GET /api/leaverequests/pending*`, `PUT /api/leaverequests/{id}/process` | `401` | `403` | `200` | `403` |
| `GET /api/payrolls/detail` | `401` | `403` | `200` (chính mình) / `403` (GV khác) | `200` |
| Các endpoint `Payrolls` khác | `401` | `403` | `403` | `200/202` |
| `GET /api/chat/conversations`, `POST /api/chat/send*` | `401` | `200` | `200` | `403` |
| `GET /api/chat/admin/conversations` | `401` | `403` | `403` | `200` |
| `POST/PUT/DELETE /api/classschedules*` | `401` **(Production)** | `403` **(Prod)** | `403` **(Prod)** | `200/201` |
| `POST/PUT/DELETE /api/teachers*` | `401` | `403` **(!)** | `403` **(!)** | `200/201` |
| `/api/classes*`, `/api/lessons*` (CRUD), `/api/parents*`, `/api/attendances*`, `/api/dev/seed-data*` | Cần bổ sung Auth **(!)** | — | — | — |

---

## 5. Các Kịch bản Kiểm thử E2E Trọng tâm (Stateful Flows)

### Flow 1: Vòng đời Tài khoản, Email Verification, PIN & Học sinh
1. `POST /api/auth/register` (`Role = "Parent"`) → lấy `accessToken` Parent.
2. `PUT /api/profile/email` với email hợp lệ → nhận email qua Mailpit → bóc token trong link `GET /api/email-verification?token=...` → assert HTML `"Xác minh thành công"`.
3. Gọi lại `GET /api/email-verification?token=...` lần 2 → assert HTML `"Liên kết đã được sử dụng"` (idempotency).
4. `GET /api/profile` → assert `emailConfirm == true`. Thử `PUT /api/profile/email` lần nữa → assert `403` (email đã khóa).
5. `POST /api/auth/forgot-password` → gọi lần 2 ngay lập tức assert `429`; lấy mật khẩu tạm từ Mailpit → login bằng mật khẩu tạm → `POST /api/auth/change-password`.
6. `PUT /api/profile/pin` (`"123456"`) → `POST /api/profile/pin/verify` (đúng → `true`, sai → `false`).
7. `POST /api/students` → `DELETE /api/students/{id}` không header `X-Pin` (`400`), sai `X-Pin` (`403`), đúng `X-Pin` (`204`).
8. Admin `PUT /api/admin/accounts/{id}/status` (`isActive: false`) → dùng token cũ của Parent gọi `GET /api/profile` → assert `401`.

### Flow 2: Thiết lập Đào tạo & Thuật toán Sinh Lịch học
1. Admin tạo `Course` → tạo `CourseLevel` (`TotalSessions = 4`, `Fee = 1000000`, `Syllabus` 4 buổi) → tạo `Class` (`MaxCapacity = 2`, gán `TeacherId`).
2. `POST /api/classschedules` (ca 1) → verify `GET /api/lessons/class/{classId}` sinh đúng `4` buổi `SCHEDULED` với `title` khớp syllabus và `Classes.EndDate` được cập nhật.
3. Tạo `ClassSchedule` trùng giờ cùng lớp / trùng phòng / trùng giờ GV cho lớp khác → assert `409 Conflict`.
4. Kiểm tra Admin `POST /api/classes/{id}/students` vượt `MaxCapacity` → assert lỗi; thêm 1 HS đã có dữ liệu → thử `DELETE /api/students/{id}` (Parent với `X-Pin`) → assert `409 Conflict`.

### Flow 3: Ghi danh & Thanh toán VNPay End-to-End
1. Parent `POST /api/enrollment/register` (`studentId, classId`) → nhận `invoiceId`.
2. Gọi lại `POST /api/enrollment/register` cùng `studentId` vào lớp khác cùng `CourseLevel` → assert `400` (chống ghi danh trùng level).
3. `POST /api/payment/create-url/{invoiceId}` → lấy `paymentUrl`, trích `vnp_TxnRef` và `vnp_Amount`.
4. Test `GET /api/payment/vnpay-ipn` các nhánh:
   - Sai `vnp_SecureHash` → `RspCode = "97"`.
   - Sai `vnp_TxnRef` → `RspCode = "01"`.
   - Sai `vnp_Amount` → `RspCode = "04"`.
   - Đúng chữ ký nhưng `vnp_ResponseCode = "24"` (khách hủy) → `RspCode = "00"`, `Invoice.Status = "FAILED"`, `ClassStudent.Status = "PENDING"`.
   - Tạo lại URL (`create-url`) → `Invoice.Status` reset về `PENDING` và cấp `TxnRef` mới!
   - Gọi IPN hợp lệ (`00` / `00`) → `RspCode = "00"`, `Invoice.Status = "PAID"`, `ClassStudent.Status = "ACTIVE"`.
   - Gọi lại IPN lần 2 cùng `TxnRef` → `RspCode = "02"` (`Order already confirmed`).
5. Kiểm tra `GET /api/enrollment/course-level/{levelId}/registrations` → `Status == "ACTIVE"`.
6. Test luồng hủy: tạo đơn khác → `POST /api/invoice/cancel/{id}` → verify giải phóng slot cho phép đăng ký lại.

### Flow 4: Xin nghỉ phép, Điểm danh, Báo cáo & Tính lương Giáo viên
1. Tạo 2 buổi học: 1 buổi sắp diễn ra `< 2 tiếng` (hoặc quá khứ), 1 buổi tương lai `> 3 tiếng`.
2. Parent `POST /api/leaverequests` cho buổi `< 2 tiếng` → assert `400`.
3. Parent `POST /api/leaverequests` cho buổi `> 3 tiếng` → `200 PENDING`. Thử gửi trùng → `409`.
4. (Test edge case hủy & gửi lại): Parent `DELETE /api/leaverequests/{id}` (`204`) → gửi đơn mới → Teacher `GET /api/leaverequests/pending` → `PUT /api/leaverequests/{id}/process?isApproved=true` → kiểm tra `GET /api/attendances/lesson/{lessonId}` tự động có `EXCUSED`.
5. Teacher điểm danh batch (`PRESENT`, `ABSENT`, `LATE`) → kiểm tra Notification Outbox cho PH (`ABSENT`/`LATE`) và hoàn thiện điểm danh (`ATTENDANCE_COMPLETED`).
6. Đánh dấu buổi học `Status = "COMPLETED"` → Admin `PUT /api/payrolls/teachers/{id}/base-rate` (`200000`) + `POST /api/payrolls/adjustments` (`Bonus: 100000`, `Penalty: 50000`) → `GET /api/payrolls/admin/summary` → assert `totalValidSessions >= 1`, `finalSalary = sessions * 200000 + 100000 - 50000`.
7. `GET /api/payrolls/{id}/preview-email` (assert HTML chứa tên GV & số tiền) → `POST /api/payrolls/{id}/send-email` (`202`) → kiểm tra mail tới Mailpit và `EmailStatus == "Sent"`.

### Flow 5: Import Worker → QuestionBank → Assignment → Practice Submit → Grade
1. `import-worker` (`LLM_PROVIDER=mock`):
   - `POST /api/parser/process-file` với `.docx` / `.pdf` mẫu (`Backend/import-worker/tests/data/de_thi_toan_tu_duy.docx`) → poll `GET /api/parser/job/{job_id}` tới `COMPLETED` → verify `questions.length > 0`.
   - `POST /api/parser/process-file` với `scanned_sample.pdf` → poll tới `FAILED` → assert `error_message` bắt đầu bằng `[SCAN_DETECTED]`.
2. Admin/Teacher `POST /api/question-banks` → `POST /api/question-banks/questions/bulk` đẩy câu hỏi vừa parse (hoặc chuẩn bị đủ 4 loại `MULTIPLE_CHOICE`, `TRUE_FALSE`, `FILL_IN`, `TIMED_SEQUENCE`).
3. `POST /api/assignments`:
   - Thử đưa câu `ESSAY` hoặc `MaxAttempt = 2` hoặc `Deadline <= StartTime` → assert `400`.
   - Tạo hợp lệ → `POST /api/assignments/clone` sang buổi khác.
4. Parent `GET /api/assignments/{id}/student/{studentId}/practice` → verify không trả `answer`, có đủ `options` (4 lựa chọn) / `sequenceTerms`.
5. Parent `POST /api/assignments/{id}/student/{studentId}/practice/submit`:
   - Thiếu 1 câu hoặc sai `questionAssignmentId` → assert `400`.
   - Gửi hợp lệ → assert `score`, `correctCount` đúng theo công thức; bắn **2 request song song** → đúng 1 request `200`, request còn lại `400` (`"Bài tập này đã được nộp."`).
6. Teacher `PUT /api/assignments/{id}/grade/{studentId}` → verify status `GRADED` & notification `ACADEMIC` cho PH.

### Flow 6: Chat & Notification Fanout
1. Parent chưa có con học lớp của Teacher B → `POST /api/chat/send` tới Teacher B → assert `403`.
2. Parent có con học lớp của Teacher A → `POST /api/chat/send` tới Teacher A → `200`; test `POST /api/chat/send-attachment` với file `.exe` / `>20MB` → assert `400`; test `GET /api/chat/{convId}/messages` với Teacher B → assert `403`.
3. Poll `GET /api/notifications/unread-count` sau khi các sự kiện nghiệp vụ ở Flow 1–5 chạy xong (chờ `NotificationFanoutWorker` 2–4s) → `PUT /api/notifications/{id}/read` → verify `unreadCount` giảm `1` → `PUT /api/notifications/read-all` → `unreadCount == 0`.

---

## 6. Lưu ý kỹ thuật khi viết Test Runner & Assertions

1. **Error Schema Wrapper:** Viết helper parse lỗi linh hoạt hỗ trợ cả `{ code, message, details }` (từ `GlobalExceptionHandler`) và `{ message }` / `ValidationProblemDetails.errors` (từ các controller tự `try/catch` hoặc ModelState validation).
2. **Timezone:** `LeaveRequestService`, `UpcomingLessonReminderService` và `AdminReportService` tính theo **giờ Việt Nam (`UTC+7`)**, trong khi `AssignmentService`, `PaymentService` và `EnrollmentService` so sánh theo **`DateTime.UtcNow`**. Test data tạo `StartTime`/`Deadline`/`LessonDate` phải quy đổi chính xác tránh lệch 7 tiếng!
3. **Async Workers Timing:** `NotificationFanoutWorker` poll mỗi `2s`, `PayslipEmailWorker` chạy async sau `202 Accepted`. Khi assert notification trong `USER_INBOX` hoặc email từ `PayslipEmailWorker`, test cần cơ chế polling retry (timeout ~10s, interval 500ms) thay vì assert ngay lập tức hoặc `sleep` cứng.
4. **Dữ liệu sẵn có để tận dụng:**
   - `import-worker/tests/data/` đã có sẵn `de_thi_toan_tu_duy.docx`, `scanned_sample.pdf`, `sample_geometry.png` và `import-worker/tests/` đã có pytest suite (`test_api.py`, `test_pipeline.py`, `test_question_parser.py`).
   - `Backend/ToanTuDuy/ToanTuDuy.http` và `BogusDataSeeder` có thể tham khảo để dựng fixture dữ liệu mẫu nhanh.
