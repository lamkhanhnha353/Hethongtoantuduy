# 02. Test Scenarios — Kiểm thử API Backend (`ToanTuDuy`)

Tài liệu này mô tả toàn bộ các **Test Scenarios (Kịch bản kiểm thử)** làm cầu nối giữa đặc tả hệ thống và bộ Test Cases chi tiết (xem thêm bản bảng CSV tại [02-Test-Scenarios.csv](./02-Test-Scenarios.csv)).

---

## Nhóm A — Smoke & Hạ tầng (`INF`)
- **SC-INF-01:** Khởi động `WebApplicationFactory` (Production & Development), kiểm tra OpenAPI chỉ bật ở `Development`, database migrate thành công và seed được 3 tài khoản gốc (`Admin`, `Teacher`, `Parent`).
- **SC-INF-02:** Kiểm tra `GlobalExceptionHandler` trả về đúng cấu trúc `{ code, message, details }` với `ApiException` (`400/401/403/404/409/429`) và `ModelState` trả `400 ValidationProblemDetails`.
- **SC-INF-03:** Kiểm tra cơ chế phân trang `PagedResult<T>` (`page < 1` chuẩn hóa thành `1`, `pageSize <= 0` về mặc định `20`/`12`, `pageSize > 100` giới hạn `100`, `totalPages` & flags `hasPreviousPage`/`hasNextPage` chính xác).

## Nhóm B — Xác thực, Xác minh Email & Khôi phục mật khẩu (`AUTH`)
- **SC-AUTH-01:** Đăng ký tài khoản `Parent` thành công (tạo `ApplicationUser` + `ParentProfile`, phát hành JWT, enqueue notification `ACCOUNT` cho Admin); từ chối role không hợp lệ (`Admin`, rỗng) và SĐT trùng.
- **SC-AUTH-02:** Đăng ký tài khoản `Teacher` bắt buộc có `Email`, tạo `TeacherProfile` + cấp `EmailVerificationToken` + gửi email xác minh qua `FakeEmailDeliveryService`.
- **SC-AUTH-03:** Đăng nhập (`POST /api/auth/login`) thành công cho `Admin`, `Teacher`, `Parent`; từ chối `401` khi sai mật khẩu, không tồn tại SĐT, hoặc hash bị hỏng (`FormatException`).
- **SC-AUTH-04:** Đăng nhập Google (`POST /api/auth/google-login`) trả `401` khi ID token không hợp lệ và trả `503` khi thiếu `Google:ClientId`.
- **SC-AUTH-05:** Xác minh email (`GET /api/email-verification?token=...`) thành công lần đầu (HTML `"Xác minh thành công"`, `EmailConfirmed = true`); kiểm tra idempotency khi click lại lần 2 (`"Liên kết đã được sử dụng"`), token hết hạn, token sai, và gửi lại link (`POST /api/email-verification/resend`) bị chặn `429` trong cooldown `60s`.
- **SC-AUTH-06:** Đổi mật khẩu (`POST /api/auth/change-password`) thành công, xóa `RefreshToken`; từ chối `401` khi sai `currentPassword` và `400` khi mật khẩu mới trùng mật khẩu cũ.
- **SC-AUTH-07:** Quên mật khẩu (`POST /api/auth/forgot-password`): email không tồn tại vẫn trả `200` (anti-enumeration); email chưa xác minh trả `400`; email đã xác minh nhận mật khẩu tạm qua `FakeEmailDeliveryService` và đăng nhập được bằng mật khẩu tạm; gọi lại ngay trong `60s` bị `429`.
- **SC-AUTH-08:** Thu hồi phiên JWT động (`OnTokenValidated`): khi Admin khóa tài khoản hoặc Admin reset password (`SecurityStamp` thay đổi), JWT cũ đang giữ lập tức bị từ chối `401`.

## Nhóm C — Hồ sơ cá nhân, Mã PIN & Học sinh (`PROF`)
- **SC-PROF-01:** Lấy và cập nhật hồ sơ (`GET/PUT /api/profile`) cho cả 3 role `Parent`, `Teacher`, `Admin`; chỉ `Admin` cập nhật được `Phone` (và chặn `409` nếu trùng SĐT).
- **SC-PROF-02:** Cập nhật email (`PUT /api/profile/email`):
  - `Admin`: lưu vào `PendingEmail`, giữ `Email` cũ cho tới khi verify link thành công; chặn `409` nếu email đang dùng/pending bởi người khác.
  - `Parent`: ghi đè email khi chưa confirm và gửi link xác minh; sau khi đã `EmailConfirmed == true` thì chặn `403`.
  - `Teacher`: bị từ chối `403`.
- **SC-PROF-03:** Upload & xóa Avatar hồ sơ (`POST/DELETE /api/profile/avatar`): chấp nhận JPG/PNG/WEBP `<= 5MB`, từ chối `400` file rỗng / `> 5MB` / sai MIME (`.txt`, `.pdf`).
- **SC-PROF-04:** Thiết lập & xác thực PIN phụ huynh (`PUT /api/profile/pin`, `POST /api/profile/pin/verify`): thiết lập lần đầu không cần `currentPin`, đổi PIN bắt buộc đúng `currentPin`, validate regex `4–6` chữ số.
- **SC-PROF-05:** CRUD Học sinh của phụ huynh (`GET/POST/PUT /api/students*` + avatar): cô lập dữ liệu hoàn toàn giữa Parent A và Parent B (Parent B truy cập học sinh của Parent A nhận `404`).
- **SC-PROF-06:** Xóa học sinh với `[RequirePin]` (`DELETE /api/students/{id}`): thiếu header `X-Pin` (`400`), chưa thiết lập PIN (`400`), sai PIN (`403`), đúng PIN nhưng học sinh đã có lớp/hóa đơn/điểm danh (`409`), đúng PIN và sạch dữ liệu (`204`).

## Nhóm D — Quản trị Tài khoản, Phụ huynh & Giáo viên (`USR`)
- **SC-USR-01:** Admin tra cứu danh sách & phân trang tài khoản (`/api/admin/accounts/paged`) theo `keyword`, `role`, `isActive` và kiểm tra số đếm `activeCount` / `blockedCount`.
- **SC-USR-02:** Admin khóa/mở khóa tài khoản (`PUT /api/admin/accounts/{id}/status`): khóa thành công tài khoản khác, chặn `409` khi Admin tự khóa chính mình.
- **SC-USR-03:** Admin reset mật khẩu hộ (`POST /api/admin/accounts/{id}/reset-password`): thành công với `Teacher`/`Parent` đã verify email; chặn `400` với `Admin` hoặc email chưa verify; trả `502` nếu `FakeEmailDeliveryService` lỗi gửi.
- **SC-USR-04:** Quản lý giáo viên (`TeachersController`): tạo, tìm kiếm, phân trang, cập nhật, khóa/mở khóa trạng thái (`Admin`), xóa khi không có lớp phụ trách.
- **SC-USR-05:** Quản lý phụ huynh (`ParentsController`): tạo, xem chi tiết, cập nhật, khóa/mở khóa (`Admin`), xóa khi chưa có dữ liệu ràng buộc.

## Nhóm E — Đào tạo: Khóa học, Cấp độ, Lớp học & Lịch học (`ACAD`)
- **SC-ACAD-01:** CRUD Khóa học (`CoursesController`): chỉ `Admin` được tạo/sửa/xóa/upload media; chặn trùng `Code`, chặn xóa khi còn `CourseLevel`; validate media upload (`image <= 10MB`, `video <= 100MB`).
- **SC-ACAD-02:** CRUD Cấp độ (`CourseLevelsController`): gắn đúng `CourseId`, trả kèm thông tin `Course` cha + JSON `Syllabus`/`Faqs`; chặn xóa khi còn `Classes`.
- **SC-ACAD-03:** CRUD Lớp học & Phân trang (`ClassesController`): lọc theo `courseId`, `courseLevelId`, `teacherId`, `status`, `capacity` (`available/full/low`), `sort`; tạo/đổi giáo viên tự động phát notification + email phân bổ lớp; chặn xóa khi lớp đã có học viên.
- **SC-ACAD-04:** Thêm/gỡ học viên thủ công vào lớp (`POST/DELETE /api/classes/{id}/students`): thêm thành công (`ACTIVE`, gửi thông báo + email cho PH & GV), chặn trùng `ACTIVE`/`PENDING`, chặn vượt `MaxCapacity`.
- **SC-ACAD-05:** Tạo/sửa/xóa Lịch học cố định (`ClassSchedulesController`) trong môi trường `Production`: chặn `401/403` với non-Admin; phát hiện xung đột `409` khi trùng ca cùng lớp, trùng phòng học, hoặc trùng lịch giáo viên.
- **SC-ACAD-06:** Thuật toán tự động sinh `Lesson` & tính `Classes.EndDate` khi thay đổi `ClassSchedule`: sinh đủ `TotalSessions` buổi `SCHEDULED` với tên buổi từ `Syllabus`, giữ nguyên buổi quá khứ/đã `COMPLETED`, chuyển buổi dư thành `CANCELED`, và chặn `409` nếu buổi sắp sửa/hủy đã có `LeaveRequest`.
- **SC-ACAD-07:** CRUD `LessonsController`: tạo/cập nhật buổi `IsImportan = true` tự động gửi notification `SPECIAL` + email; chặn xóa buổi đã có `Attendance` hoặc `LeaveRequest`.
- **SC-ACAD-08:** Timeline & Roadmap học tập của học sinh (`GET /api/lessons/student/{id}`, `/roadmap`): chỉ trả buổi của lớp `ACTIVE` thuộc Parent hiện tại; buổi `SCHEDULED` đầu tiên là `IN_PROGRESS`, các buổi sau là `LOCKED`.

## Nhóm F — Ghi danh, Hóa đơn & Thanh toán VNPay (`FIN`)
- **SC-FIN-01:** Ghi danh giữ chỗ (`POST /api/enrollment/register`): tạo đồng thời `ClassStudent(PENDING)` và `Invoice(PENDING)`, gửi notification + email; kiểm tra `GET /api/enrollment/course-level/{id}/registrations` trả trạng thái `PENDING`.
- **SC-FIN-02:** Chống ghi danh trùng cấp độ & ownership: chặn `401` nếu Student không thuộc Parent; chặn `400` nếu Student đã có `ACTIVE` hoặc `Invoice` `PENDING/PAID` trong cùng `CourseLevelId` (kể cả lớp khác).
- **SC-FIN-03:** Hủy hóa đơn (`POST /api/invoice/cancel/{id}`): chỉ hủy được `PENDING`, chuyển cả `Invoice` và `ClassStudent` về `CANCELLED`, cho phép đăng ký lại sau khi hủy; chặn `400` nếu hóa đơn đã `PAID`/`CANCELLED`.
- **SC-FIN-04:** Tạo URL thanh toán VNPay (`POST /api/payment/create-url/{id}`): kiểm tra `MaxCapacity` tại bước tạo URL (`409` nếu lớp đã đầy), sinh `TxnRef` mới, cho phép tạo lại URL khi hóa đơn đang `FAILED` (khôi phục `PENDING`).
- **SC-FIN-05:** Xử lý VNPay IPN (`GET /api/payment/vnpay-ipn`):
  - Sai chữ ký → `RspCode = "97"`.
  - Không thấy `TxnRef` → `RspCode = "01"`.
  - Sai `vnp_Amount` → `RspCode = "04"`.
  - Thanh toán thất bại (`vnp_ResponseCode != "00"`) → `RspCode = "00"`, `Invoice = FAILED`, `ClassStudent` vẫn `PENDING`.
  - Thanh toán thành công (`00/00`) → `RspCode = "00"`, `Invoice = PAID`, `ClassStudent = ACTIVE`, gửi thông báo + email.
  - Gọi lặp lại IPN (Idempotency) → `RspCode = "02"` (`Order already confirmed`).
- **SC-FIN-06:** VNPay Return (`GET /api/payment/vnpay-return`): redirect `302` tới `MobileDeepLink?code=00` khi thành công hoặc mã lỗi tương ứng.
- **SC-FIN-07:** Lịch sử hóa đơn Parent (`GET /api/invoice/history`) và tra cứu/KPI hóa đơn Admin (`GET /api/admin/invoices/paged`).

## Nhóm G — Điểm danh & Đơn xin nghỉ phép (`ATT`)
- **SC-ATT-01:** Phụ huynh gửi đơn xin nghỉ (`POST /api/leaverequests`): thành công (`PENDING`) khi HS `ACTIVE` trong lớp, buổi `SCHEDULED`, cách `StartTime` >= `2 tiếng` (giờ VN), gửi notification cho GV.
- **SC-ATT-02:** Các chốt chặn xin nghỉ phép: từ chối `400` nếu HS không thuộc lớp / buổi không phải `SCHEDULED` / buổi trong quá khứ hoặc `< 2 tiếng`; từ chối `409` nếu đã gửi đơn chưa hủy; từ chối `404` nếu HS của phụ huynh khác.
- **SC-ATT-03:** Hủy đơn (`DELETE /api/leaverequests/{id}`): chuyển `CANCELLED` khi còn `PENDING`, chặn `409` nếu đơn đã `APPROVED`/`REJECTED`.
- **SC-ATT-04:** Giáo viên xem danh sách chờ & duyệt/từ chối đơn (`GET /api/leaverequests/pending*`, `PUT /api/leaverequests/{id}/process`): GV chỉ duyệt được đơn thuộc lớp mình dạy; `isApproved=true` tự động tạo/ghi đè `Attendance(EXCUSED)` và gửi notification cho PH; `isApproved=false` giữ nguyên không tạo `Attendance`.
- **SC-ATT-05:** Điểm danh hàng loạt & đơn lẻ (`AttendancesController`): validate HS thuộc lớp (`ACTIVE`), status hợp lệ (`PRESENT|ABSENT|LATE|EXCUSED`), upsert bản ghi, phát notification `ABSENT`/`LATE` cho PH và `ATTENDANCE_COMPLETED` cho GV+Admin khi đủ 100% sĩ số.
- **SC-ATT-06:** Lịch sử điểm danh học sinh (`GET /api/attendances/student/{id}`) lọc theo `classId` tùy chọn.

## Nhóm H — Ngân hàng câu hỏi & Bài tập / Luyện tập (`ASGN`)
- **SC-ASGN-01:** CRUD Ngân hàng câu hỏi & Câu hỏi (`QuestionBanksController`): tạo bank, tạo câu hỏi đơn & `bulk`, lọc/phân trang theo `courseId`, `difficulty`, `type`, `keyword`; xóa đơn & `bulk-delete` tự gỡ liên kết `QuestionAssignment`.
- **SC-ASGN-02:** Tạo & Nhân bản Bài tập (`POST /api/assignments`, `/clone`): validate có câu hỏi, `MaxAttempt == 1`, `Deadline > StartTime`, chỉ nhận 4 loại câu hỏi hỗ trợ (từ chối `ESSAY`), kiểm tra câu hỏi `MULTIPLE_CHOICE` đủ 4 đáp án `A–D` và `TIMED_SEQUENCE` đúng format; giao cho cả lớp hoặc `SpecificStudentIds`.
- **SC-ASGN-03:** Cập nhật, Xóa, Giao thêm & Hủy giao bài (`PUT/DELETE /api/assignments/{id}`, `POST/DELETE .../students`): chặn hủy giao (`400`) nếu học sinh đã `SUBMITTED`/`GRADED`.
- **SC-ASGN-04:** Phụ huynh xem danh sách & đề luyện tập (`GET .../practice`): kiểm tra ownership; tính `isAvailable`; ẩn đáp án đúng (`Answer`), parse sẵn `options` (`A..D`) và `sequenceTerms`. Chặn `400` khi bài chưa mở / hết hạn / đã nộp.
- **SC-ASGN-05:** Nộp bài Practice & Tự chấm điểm (`POST .../practice/submit`): chấm chính xác cả 4 loại (`MULTIPLE_CHOICE`, `TRUE_FALSE`, `FILL_IN` chuẩn hóa khoảng trắng/Unicode, `TIMED_SEQUENCE`); từ chối `400` nếu thiếu/thừa câu trả lời; gửi notification cho GV.
- **SC-ASGN-06:** Kiểm tra đồng thời khi nộp bài (`Concurrency`): bắn nhiều request `submit` song song cho cùng `(assignmentId, studentId)` → chỉ đúng 1 request thành công `200`, các request còn lại trả `400` nhờ atomic `ExecuteUpdateAsync`.
- **SC-ASGN-07:** Giáo viên chấm điểm đơn lẻ & hàng loạt (`PUT .../grade/{studentId}`, `PUT .../batch-grade`): cập nhật `Score`, `TeacherFeedback`, `Status = GRADED` và gửi notification `ACADEMIC` cho PH.

## Nhóm I — Lương Giáo viên & Báo cáo Admin (`PAY`)
- **SC-PAY-01:** Cấu hình đơn giá/buổi (`PUT /api/payrolls/teachers/{id}/base-rate`) & thêm Thưởng/Phạt (`POST /api/payrolls/adjustments`).
- **SC-PAY-02:** Tổng hợp bảng lương tháng (`GET /api/payrolls/admin/summary` & `/paged`): chỉ đếm buổi `COMPLETED` **và** có ít nhất 1 bản ghi `Attendance`; tính đúng `BaseSalary + Bonus - Penalty`; khi dữ liệu tài chính thay đổi sau khi đã gửi mail thì tự reset `EmailStatus = NotSent`.
- **SC-PAY-03:** Xem chi tiết phiếu lương (`GET /api/payrolls/detail`): Admin xem được mọi GV; Teacher chỉ xem được của chính mình (`403` nếu xem GV khác).
- **SC-PAY-04:** Preview HTML & Hàng đợi gửi email phiếu lương (`GET /{id}/preview-email`, `POST /{id}/send-email`, `POST /send-batch-emails`): kiểm tra `400` khi email chưa cấu hình; `202 Accepted` khi hợp lệ; worker render HTML từ `Templates/Payslip.html`, gửi email qua `FakeEmailDeliveryService` và cập nhật `EmailStatus = Sent`.
- **SC-PAY-05:** Báo cáo Admin (`AdminReportsController`): xác nhận số liệu `revenue`, `payroll`, `student-growth`, `attendance` khớp với dữ liệu vừa tạo ở các bước trước.

## Nhóm J — Chat & Hệ thống Thông báo (`COMM`)
- **SC-COMM-01:** Quy tắc phân quyền Chat (`POST /api/chat/send`): Parent chỉ nhắn được cho Teacher đang phụ trách lớp của con mình (`403` nếu không dạy); `Admin` bị chặn `403` ở API chat user nhưng xem được `GET /api/chat/admin/conversations`.
- **SC-COMM-02:** Nhắn tin văn bản, tạo Conversation tự động (idempotent khi nhắn song song), phân trang tin nhắn (`pageSize = 30`, thứ tự tăng dần), từ chối `403` khi user ngoài cuộc hội thoại đọc/xóa tin.
- **SC-COMM-03:** Gửi & Xóa tệp đính kèm Chat (`POST /api/chat/send-attachment`, `DELETE /api/chat/{convId}/messages/{msgId}`): validate định dạng/MIME/size (`<= 20MB`), xóa kèm file trên `FakeChatAttachmentStorage` và cập nhật lại `LastMessage` của Conversation.
- **SC-COMM-04:** SignalR Hubs Auth (`/chathub`, `/hubs/notifications`): từ chối kết nối thiếu token; kết nối thành công với `?access_token=...` và nhận được event `ReceiveNewMessage` / `ReceiveNotification`.
- **SC-COMM-05:** Outbox Fanout & Idempotency (`NotificationFanoutWorker` + `NotificationsController`): tạo thông báo qua `POST /api/admin/notifications` hoặc `POST /api/notifications/test` (Development), worker phát vào `USER_INBOX`, cập nhật `unread-count`, bỏ qua user đã tắt `IN_APP_NOTIFICATION`, không tạo trùng nếu trùng `(AccountId, Type, RefId)`.
- **SC-COMM-06:** Đọc & Cài đặt thông báo (`PUT /{id}/read`, `PUT /read-all`, `GET/PUT /settings`): kiểm tra `unreadCount` giảm đúng, idempotent khi đọc lại.
- **SC-COMM-07:** Nhắc buổi học sắp diễn ra (`UpcomingLessonReminderService.ProcessUpcomingLessonsAsync`): tạo buổi `SCHEDULED` bắt đầu sau 30 phút (giờ VN) → kiểm tra phát notification & email cho GV + PH đúng 1 lần (chống trùng bằng `RefId`).

## Nhóm K — Security Regression (Các lỗ hổng đã phát hiện) (`SEC`)
- **SC-SEC-01:** `AuthService.LoginAsync` với tài khoản bị khóa (`IsActive = false`) — xác minh login và sử dụng token đều bị từ chối `401`.
- **SC-SEC-02:** `ParentsController` — xác minh hành vi hiện tại (`[AllowAnonymous]`) và `ParentResponseDto` có rò rỉ `Pin` hay không (regression sau khi fix).
- **SC-SEC-03:** `ClassesController`, `LessonsController` CRUD, `DevSeedController`, `TeachersController`, `AttendancesController` — kiểm tra ma trận RBAC mục tiêu sau khi vá auth.
- **SC-SEC-04:** `AssignmentsController.GetByStudent` & `GetStudentSubmission` — kiểm tra Parent A không thể xem bài/đáp án của học sinh thuộc Parent B.
- **SC-SEC-05:** `AttendanceService.GetAttendanceByLessonIdAsync` — regression test cho trường hợp học sinh có 1 đơn `CANCELLED` + 1 đơn `PENDING` trong cùng 1 buổi học.
