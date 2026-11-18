# 04. Danh mục API — Tài chính, Lương, Báo cáo, Chat, Thông báo & Import Worker

---

## 1. Ghi danh lớp học (`EnrollmentController` — `/api/enrollment`)

Nguồn: [`Controllers/EnrollmentController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/EnrollmentController.cs), [`Services/EnrollmentService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/EnrollmentService.cs)

Toàn bộ controller yêu cầu `[Authorize(Roles = "Parent")]`.

| Method | Endpoint | Input | Response & Business Rules |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/enrollment/course-level/{courseLevelId}/registrations` | `courseLevelId` | `200 OK`: `List<EnrollmentRegistrationResponse>` (gộp `Invoice` `PENDING/PAID` và `ClassStudent` `ACTIVE/PENDING` của các con thuộc Parent hiện tại, ưu tiên `ACTIVE`) |
| `POST` | `/api/enrollment/register` | `EnrollmentRequest`: `studentId` (>=1), `classId` (>=1) | `200 OK`: `{ message, invoiceId }`<br>`401`: Student không thuộc Parent hiện tại<br>`404`: Không tìm thấy Class<br>`400`: Đã có `ClassStudent` `ACTIVE` hoặc `Invoice` `PENDING/PAID` cho **cùng `CourseLevelId`**; hoặc `CourseLevel.Fee <= 0`<br>Side effects: tạo `ClassStudent(PENDING)` + `Invoice(PENDING, TxnRef = "TEMP_...")`, gửi notification `ENROLLMENT` cho Admins + GV và email cho GV + PH |

> **Lưu ý kiểm thử:** `RegisterClassAsync` **không kiểm tra `MaxCapacity`** của lớp tại bước giữ chỗ (chỉ kiểm tra ở `PaymentService.CreatePaymentUrl`).

---

## 2. Hóa đơn (`InvoiceController` — `/api/invoice` & `/api/admin/invoices`)

Nguồn: [`Controllers/InvoiceController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/InvoiceController.cs), [`Services/InvoiceService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/InvoiceService.cs)

| Method | Endpoint | Roles | Query / Params | Response & Rules |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/invoice/history` | `Parent` | — | `200 OK`: `List<InvoiceHistoryResponse>` (chỉ hóa đơn của Parent hiện tại) |
| `POST` | `/api/invoice/cancel/{invoiceId}` | `Parent` | `invoiceId` | `200 OK`: `{ message }`<br>`404`: Không thấy hoặc không thuộc Parent<br>`400`: Chỉ hủy được khi `Invoice.Status == "PENDING"` → set `Invoice.Status = "CANCELLED"` và `ClassStudent.Status = "CANCELLED"` |
| `GET` | `/api/admin/invoices` | `Admin` | — | `200 OK`: `List<AdminInvoiceResponse>` |
| `GET` | `/api/admin/invoices/paged` | `Admin` | `page=1, pageSize=20, keyword?, status?` | `200 OK`: `AdminInvoicePagedResponse` (kèm `totalAllCount, paidCount, pendingCount, paidTotalAmount` toàn bảng) |

---

## 3. Thanh toán VNPay (`PaymentController` — `/api/payment`)

Nguồn: [`Controllers/PaymentController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/PaymentController.cs), [`Services/PaymentService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/PaymentService.cs), [`Utils/VnPayLibrary.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Utils/VnPayLibrary.cs)

Class có `[Authorize(Roles = "Parent")]`.

| Method | Endpoint | Auth | Input | Response & Business Rules |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/payment/create-url/{invoiceId}` | `Parent` | `invoiceId` (> 0) | `200 OK`: `{ paymentUrl }`<br>`404`: Không tìm thấy hóa đơn của Parent<br>`400`: `invoiceId <= 0` hoặc hóa đơn không ở trạng thái `PENDING` (nếu `FAILED` nhưng `ClassStudent.Status == "PENDING"`, tự khôi phục về `PENDING`)<br>`409`: Lớp đã đủ `MaxCapacity` (`ACTIVE >= MaxCapacity`)<br>Side effect: cập nhật `Invoice.TxnRef = "INV{id}_{UtcNow.Ticks}"` |
| `GET` | `/api/payment/vnpay-return` | `[AllowAnonymous]` | VNPay query params (`vnp_*`, `vnp_SecureHash`) | `302 Redirect` tới `VnPay:MobileDeepLink?code=...` (`00` nếu `PAID`, `99` nếu `vnp_ResponseCode=00` nhưng không `PAID`, hoặc giữ `vnp_ResponseCode`)<br>**Lưu ý:** ném `InvalidOperationException` (`400`) nếu thiếu `VnPay:MobileDeepLink` trong config! |
| `GET` | `/api/payment/vnpay-ipn` | `[AllowAnonymous]` | VNPay query params (`vnp_*`, `vnp_SecureHash`) | `200 OK`: `VnPayIpnResponse { RspCode, Message }` (PascalCase `RspCode`, `Message`!)<br>`97`: Invalid signature<br>`01`: Order not found (`vnp_TxnRef`)<br>`04`: Invalid amount (`vnp_Amount != invoice.Amount * 100`)<br>`02`: Order already confirmed (`invoice.Status != "PENDING"`)<br>`00`: Confirm Success |

### Chi tiết xử lý & Ký HMAC-SHA512 (`VnPayLibrary`)
- Điều kiện thanh toán thành công: `vnp_ResponseCode == "00"` **VÀ** `vnp_TransactionStatus == "00"`.
- Khi thành công: `Invoice.Status = "PAID"`, `PaymentMethod = "VNPAY"`, `TransactionId = vnp_TransactionNo`, `PaidAt = UtcNow`, `ClassStudent.Status = "ACTIVE"`. Sau đó gửi notification `PAYMENT` (Admins + GV) và `ENROLLMENT` (Parent) + email xác nhận cho PH & GV.
- Khi thất bại: `Invoice.Status = "FAILED"` (giữ `ClassStudent.Status = "PENDING"` để cho phép tạo URL thanh toán lại).
- **Cách tính chữ ký hợp lệ trong test tự động:**
  1. Thu thập tất cả query key bắt đầu bằng `vnp_` (loại `vnp_SecureHash` và `vnp_SecureHashType`), sắp xếp theo `string.CompareOrdinal(x, y)`.
  2. Nối `WebUtility.UrlEncode(k) + "=" + WebUtility.UrlEncode(v)` bằng `&` (không có `&` cuối).
  3. Tính `HMACSHA512(VnPay:HashSecret, signData)` ở dạng hex lowercase để gán vào `vnp_SecureHash`.

---

## 4. Lương Giáo viên (`PayrollsController` — `/api/payrolls`)

Nguồn: [`Controllers/PayrollsController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/PayrollsController.cs), [`Services/PayrollService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/PayrollService.cs), [`Services/PayslipMailService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/PayslipMailService.cs)

Class có `[Authorize]`.

| Method | Endpoint | Roles | Input | Response & Business Rules |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/payrolls/admin/summary` | `Admin` | Query `month` (`1..12`), `year` (`1..9998`) | `200 OK`: `List<PayslipDto>` (tính toán & upsert bảng `PAYROLL_SUMMARY` trong transaction `Serializable`) |
| `GET` | `/api/payrolls/admin/summary/paged` | `Admin` | `month, year, page=1, pageSize=20, keyword?, emailStatus?` | `200 OK`: `PagedResult<PayslipDto>` |
| `PUT` | `/api/payrolls/teachers/{teacherId}/base-rate` | `Admin` | `UpdateTeacherBaseRateRequestDto`: `baseRatePerSession` (`0..9999999999999999.99`) | `200 OK`: `TeacherBaseRateDto` / `400` / `404` |
| `GET` | `/api/payrolls/detail` | `Admin,Teacher` | Query `teacherId` (>=1), `month`, `year` | `200 OK`: `PayslipDetailDto` (`payslip, sessions[], adjustments[]`)<br>`403`: Teacher xem của người khác |
| `POST` | `/api/payrolls/adjustments` | `Admin` | `CreateAdjustmentRequestDto`: `teacherId, type ("Penalty"\|"Bonus"), amount (>0), reason (1..500), appliedDate (DateOnly)` | `201 Created`: `SalaryAdjustmentDto` / `400` / `404` |
| `GET` | `/api/payrolls/{id}/preview-email` | `Admin` | `id` (`PayrollSummary.Id`) | `200 OK` **`text/html; charset=utf-8`** (HTML từ `Templates/Payslip.html`) / `404` |
| `POST` | `/api/payrolls/{id}/send-email` | `Admin` | `id` | `202 Accepted`: `{ payrollId, queued, message }`<br>`400`: Chưa cấu hình SMTP hoặc Resend |
| `POST` | `/api/payrolls/send-batch-emails` | `Admin` | Query `month, year` | `202 Accepted`: `{ queuedCount, skippedCount }`<br>`400`: Chưa cấu hình email |

### Công thức tính lương (`PayrollService`)
- Khoảng thời gian: `[DateTime(year, month, 1, 0, 0, 0, Utc), AddMonths(1))`.
- `TotalValidSessions`: số `Lesson` có `Class.TeacherId == teacherId`, `Status == "COMPLETED"` **và** `Attendances.Any()`.
- `BaseSalary = TotalValidSessions * BaseRatePerSession`.
- `FinalSalary = BaseSalary + TotalBonus - TotalPenalty`.
- Nếu số liệu tài chính thay đổi khi tính lại mà `EmailStatus` đang là `Sent` hoặc `Failed`, hệ thống tự reset `EmailStatus = "NotSent"`.

---

## 5. Báo cáo Admin (`AdminReportsController` — `/api/admin/reports`)

Nguồn: [`Controllers/AdminReportsController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/AdminReportsController.cs), [`Services/AdminReportService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/AdminReportService.cs)

Toàn bộ controller yêu cầu `[Authorize(Roles = "Admin")]`.

| Method | Endpoint | Query | Response |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/admin/reports/revenue` | `months=6` (`Range(1, 24)`) | `AdminRevenueReportResponse`: `totalRevenue, paidInvoiceCount, pendingInvoiceCount, monthlyRevenue[], topClasses[] (top 5), paidInvoices[]` |
| `GET` | `/api/admin/reports/payroll` | `months=6` (`Range(1, 24)`) | `AdminPayrollTrendResponse`: `monthlyPayroll[]` (đọc từ `PAYROLL_SUMMARY` đã lưu) |
| `GET` | `/api/admin/reports/student-growth` | `months=6` (`Range(1, 24)`) | `AdminStudentGrowthReportResponse`: `activeStudentCount, newEnrollmentCount, classCount, monthlyEnrollments[], classSummaries[], recentEnrollments[] (max 50)` |
| `GET` | `/api/admin/reports/attendance` | `days=90` (`1..365`), `classId?` | `AdminAttendanceReportResponse`: `classes[], lessonCount, totalStudents, presentCount, absentCount, excusedCount, lateCount, unmarkedCount, lessons[]` |

---

## 6. Chat Real-time (`ChatController` — `/api/chat` & Hub `/chathub`)

Nguồn: [`Controllers/ChatController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/ChatController.cs), [`Services/ChatService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/ChatService.cs)

Class có `[Authorize]`. Vai trò được dùng chat: `Parent` và `Teacher` (riêng `admin/conversations` cho `Admin`).

| Method | Endpoint | Roles / Quyền | Input | Response & Business Rules |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/chat/conversations` | `Parent`, `Teacher` (`Admin` → `403`) | — | `200 OK`: `{ success: true, data: List<ConversationDto> }` (`UnreadCount` luôn = `0`) |
| `GET` | `/api/chat/admin/conversations` | `Admin` | — | `200 OK`: `{ success: true, data: List<ConversationDto> }` |
| `GET` | `/api/chat/{conversationId}/messages` | `Parent`/`Teacher` thuộc hội thoại | Query `page=1` (`1..10000`, pageSize cố định `30`) | `200 OK`: `{ success: true, data: List<ChatMessageDto> }` (đã reverse tăng dần theo thời gian)<br>`400` / `403` / `404` |
| `POST` | `/api/chat/send` | `Parent`, `Teacher` | `SendMessageRequest` (`receiverId` = ProfileId, `receiverType` = `"PARENT"`\|`"TEACHER"`, `messageType` = `"TEXT"`, `content` max 5000) | `200 OK`: `{ success: true, data: ChatMessageDto }`<br>Kiểm tra: `Parent` chỉ nhắn được cho `Teacher` đang phụ trách lớp có con mình (`ClassStudent` bất kỳ status, không lọc `ACTIVE`!) → nếu không → `403`. Tự tạo `Conversation` nếu chưa có; bắn SignalR `"ReceiveNewMessage"` |
| `POST` | `/api/chat/send-attachment` | `Parent`, `Teacher` | `multipart/form-data`: `receiverId`, `receiverType`, `file` (Limit `21MB`, rule `20MB`; ảnh `.jpg/.jpeg/.heic/.heif/.png/.webp` hoặc tài liệu `.pdf/.doc/.docx/.xls/.xlsx/.ppt/.pptx` khớp MIME) | `200 OK`: `{ success: true, data: ChatMessageDto }` (upload Cloudinary + bắn SignalR `"ReceiveNewMessage"`) |
| `DELETE` | `/api/chat/{conversationId}/messages/{messageId}` | `Parent`/`Teacher` thuộc hội thoại | `conversationId, messageId` | `200 OK`: `{ success: true }`. Xóa file Cloudinary nếu có, cập nhật lại `LastMessage`/`LastMessageAt` của Conversation, bắn SignalR `"MessageDeleted"` |

---

## 7. Thông báo (`NotificationsController` & `AdminNotificationsController`)

Nguồn: [`NotificationsController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/NotificationsController.cs), [`AdminNotificationsController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/AdminNotificationsController.cs)

### 7.1. Inbox người dùng (`/api/notifications`) — `[Authorize(Roles = "Parent,Teacher,Admin")]`
| Method | Endpoint | Query / Body | Response & Rules |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/notifications` | `page=1, pageSize=20 (max 100), unreadOnly=false, type?` | `200 OK`: `NotificationListResponse { items, page, pageSize, total, unreadCount }` |
| `GET` | `/api/notifications/unread-count` | — | `200 OK`: `{ unreadCount }` (đọc Redis cache, fallback SQL `USER_INBOX`) |
| `POST` | `/api/notifications/test` | `CreateTestNotificationRequest` (`title?, body?, type?, refId?`) | **Chỉ hoạt động khi `IsDevelopment()`** (môi trường khác trả `404`). Tạo thông báo vào Outbox cho chính account hiện tại |
| `PUT` | `/api/notifications/{id}/read` | `id` (`UserInbox.Id`) | `204 No Content` (idempotent nếu đã đọc) / `404` |
| `PUT` | `/api/notifications/read-all` | — | `200 OK`: `{ updated }` (reset unread count về `0`) |
| `GET` | `/api/notifications/settings` | — | `200 OK`: `List<NotificationSettingResponse>` (mặc định 5 loại `IN_APP_NOTIFICATION, EMAIL_NOTIFICATION, PUSH_NOTIFICATION, SMS_ALERT, REPORT_WEEKLY` đều `true`) |
| `PUT` | `/api/notifications/settings` | `UpdateNotificationSettingsRequest` (`settings[]`: `{ type, enabled }`) | `200 OK`: `List<NotificationSettingResponse>` |

### 7.2. Quản trị gửi thông báo (`/api/admin/notifications`) — `[Authorize(Roles = "Admin")]`
| Method | Endpoint | Query / Body | Response & Rules |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/admin/notifications` | `page=1, pageSize=20, keyword?, type?` | `200 OK`: `AdminNotificationHistoryResponse` (kèm KPI `pendingCount, failedCount, deliveredCount, readCount`) |
| `POST` | `/api/admin/notifications` | `CreateAdminNotificationRequest` (`recipientGroup`: `"PARENT"\|"TEACHER"\|"PARENT_AND_TEACHER"`, `type`: `"SYSTEM"\|"ANNOUNCEMENT"\|"SPECIAL"`, `title`, `body?`, `sendEmail`) | `202 Accepted`: `{ queued, emailQueued, emailConfigured, message }`. Nếu `type == "SPECIAL"`, ép `sendEmail = true`. Báo `400` nếu không có tài khoản active trong nhóm nhận |

---

## 8. Python Import Worker (`Backend/import-worker` — Port `8000`)

Nguồn: [`import-worker/app/main.py`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/import-worker/app/main.py), [`import-worker/app/api/routes.py`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/import-worker/app/api/routes.py)

Tất cả endpoint đều **không có authentication** (CORS `*`).

| Method | Endpoint | Input | Output & Behavior |
| :--- | :--- | :--- | :--- |
| `GET` | `/` | — | `200 OK`: `{ service, status: "online", docs_url: "/docs", provider }` |
| `GET` | `/health` | — | `200 OK`: `{ status: "healthy" }` |
| `POST` | `/api/parser/process-file` | Multipart: `file` (`.docx` \| `.pdf`), `bank_id` (default `1`), `job_id?` | `202 Accepted`: `ProcessFileResponse { job_id, status: "PENDING", check_status_url, ... }`<br>`400`: Thiếu file hoặc sai đuôi |
| `POST` | `/api/parser/process-path` | JSON `ProcessFileRequest { file_path, job_id?, bank_id=1 }` | `202 Accepted`: `ProcessFileResponse`<br>`400` / `404` |
| `GET` | `/api/parser/job/{job_id}` | `job_id` | `200 OK`: `JobDetailResponse { job_id, status, progress_percentage, total_chunks, total_questions, questions[], error_message, ... }`<br>`404`: Không thấy Job (hoặc worker vừa restart vì lưu in-memory) |
| `GET` | `/static/images/{filename}` | — | Ảnh trích xuất từ `.docx`/`.pdf` |
| `POST` | `/api/parser/test-gemini` | Chỉ loopback IP + Origin cục bộ | `200 OK`: trạng thái từng Gemini key / `403` / `400` |

### Vòng đời Job & Error Prefixes
- Status: `PENDING` → `EXTRACTING` (15%) → `CHUNKING` (35%) → `PARSING` (40–98%) → `COMPLETED` (100%) hoặc `FAILED`.
- Mã lỗi trong `error_message`:
  - `[SCAN_DETECTED]`: PDF có `total_text_chars < 50` hoặc `avg_chars_per_page < 15` (`PdfExtractor`).
  - `[EXTRACT_ERROR]`: file `.doc` cũ, file hỏng/rỗng hoặc không chunk được câu nào.
  - `[SYSTEM_ERROR]`: lỗi LLM / thiếu API key / hệ thống.
- **Chế độ kiểm thử không tốn quota AI:** Đặt `LLM_PROVIDER=mock` (hoặc `heuristic`) trong môi trường worker để parse bằng rule offline.
