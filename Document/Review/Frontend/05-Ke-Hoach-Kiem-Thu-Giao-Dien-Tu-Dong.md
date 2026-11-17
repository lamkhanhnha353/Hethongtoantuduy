# 05. Kế hoạch & Hướng dẫn Kỹ thuật Kiểm thử Giao diện Tự động (Playwright / Cypress)

Tài liệu này tổng hợp chiến lược kỹ thuật cụ thể cho `Web_Frontend` dựa trên các hành vi DOM, API, trạng thái và lỗi đã kiểm chứng ở 4 phần trước.

---

## 1. Chiến lược Kỹ thuật Đặc thù của Dự án

### 1.1 Chiến lược Locator / Selector (Không có `data-testid`)
Hiện không có thuộc tính `data-testid` trong source. Thứ tự ưu tiên selector ổn định nên dùng trong Page Object Model (POM):
1. **ARIA & Role chuẩn sẵn có**:
   - Thông báo lỗi & trạng thái: `page.getByRole('alert')`, `page.getByRole('status')`.
   - Công tắc tài khoản: `page.getByRole('switch', { name: ... })` (tại `AccountSecurityView.vue`).
   - Modal chuẩn: `page.getByRole('dialog')` — **Lưu ý ngoại lệ**: các modal inline ở `ParentStudentView`, `TeacherManagementView`, `ClassDetailView` (Add Student / Assign Teacher), và drawer ở `GoogleCalendarView` **không có `role="dialog"`**, cần bắt theo tiêu đề `heading` bên trong modal hoặc thêm `role="dialog"` / `data-testid` trước khi viết suite lớn.
2. **Nhãn form, `aria-label` và `placeholder`**:
   - Đăng nhập: `#phone`, `#password`.
   - `PaginationBar`: `getByLabel('Số dòng mỗi trang')`.
   - `SearchBar`: `getByPlaceholder(...)`.
   - `AdminNotificationsView`: `#notif-inbox-type-filter` (trong `NotificationInboxPanel`).
3. **Element Plus (`AdminPayrollView`, `TeacherPayslipView`, `ElMessage`)**:
   - Popup của `el-select` và `el-date-picker` được render trực tiếp dưới `<body>`, **không nằm trong form/modal cha**. Cần chọn `.el-select-dropdown__item`, `.el-picker-panel`, `.el-message-box`, `.el-message`.

### 1.2 Xử lý Native Browser Dialog (`alert` / `confirm`)
Một số màn hình quan trọng sẽ **treo toàn bộ test script** nếu không gắn `page.on('dialog', ...)` trước khi click:
- **Gọi `window.confirm`**: `AccountSecurityView` (khóa/reset), `ParentStudentView` (xóa), `TeacherManagementView` (xóa), `ClassDetailView` (xóa lịch/xóa buổi/gỡ HS), `ClassModal` (xóa lớp), `CourseModal` (xóa khóa), `LessonModal` (xóa buổi), `ScheduleModal` (xóa lịch), `TeacherAssignmentsView` (xóa bài tập/gỡ HS), `TeacherAttendanceView` (lưu hàng loạt khi còn HS chưa điểm danh), `ProfileView` (xóa avatar).
- **Gọi `window.alert`**: `ParentStudentView`, `TeacherManagementView`, `ClassDetailView` / `LessonModal` (chặn xóa buổi đã điểm danh), `TeacherAttendanceReportView` (nút xuất báo cáo), `TeacherNoticesView` (gửi/validate modal soạn mock).

### 1.3 Quản lý Session (`sessionStorage`) & Tăng tốc Đăng nhập Test
- Phiên đăng nhập chỉ lưu trong **`sessionStorage`** (`toanTuDuy.accessToken`, `toanTuDuy.role`, `toanTuDuy.profileId`, `toanTuDuy.userInfo`).
- **Mẹo tốc độ cho E2E**: Sau khi chạy 1 test đăng nhập UI (`LoginView`) riêng biệt, các spec nghiệp vụ có thể gọi API `POST /api/auth/login` trong fixture rồi `addInitScript` ghi trực tiếp 3 khóa vào `window.sessionStorage` để nhảy thẳng vào màn hình cần test.
- **Lưu ý 401 khi Backend tắt / token giả**: `api.service.js` chỉ tự đá về `/login` khi HTTP `401` đi kèm **response body rỗng** (`!error.response?.data`). Nếu Backend trả `401` kèm JSON problem details, trang sẽ giữ nguyên và hiện lỗi.

### 1.4 Đồng bộ Async, Debounce & Dịch vụ Ngoài
- **Debounce `300ms`**: Mọi thanh tìm kiếm server-side (`InvoicesView`, `AccountSecurityView`, `ParentStudentView`, `TeacherManagementView`, `ClassesView`, `CourseDetailView`, `ClassDetailView` Add Student modal, `AdminPayrollView`, `TeacherQuestionsView`, `TeacherStudentsView`) đều chờ `300ms` mới gọi API. Trong Playwright nên `Promise.all([page.waitForResponse(...), input.fill(...)])` thay vì `waitForTimeout`.
- **AI Import Worker (`:8000`) & Cloudinary Upload**:
  - `CourseModal` (upload ngay khi chọn tệp) và `ImportQuestionsModal` (polling `GET /api/parser/job/{id}` mỗi `2.5s`) nên có bộ test **mock network route (`page.route`)** cho `/api/parser/*` và `POST /api/courses/media` khi chạy CI, kèm 1 bộ smoke integration test thật khi docker stack chạy đủ.
- **SignalR (`/hubs/notifications`, `/chathub`)**: Có thể kiểm tra WebSocket handshake `101 Switching Protocols` hoặc dùng 2 BrowserContext (Admin tạo thông báo `SPECIAL` -> Teacher nhận Event `"ReceiveNotification"` trên `Header.vue` mà không cần reload trang).

---

## 2. Danh sách Lỗi & Khoảng trống cần Sửa / Lưu ý trước khi chạy Auto Test

| Mức độ | Vị trí | Vấn đề quan sát được | Tác động tới Auto Test |
| :--- | :--- | :--- | :--- |
| **Cao** | `TeacherAssignmentsView.vue` | `editForm` thiếu khởi tạo `maxAttempt`, gửi cứng `maxAttempt: 1` khi PUT. | Test luồng **Sửa bài tập** sẽ bị lỗi validation nếu không gõ lại ô số lần làm tối đa, và không giữ giá trị `maxAttempt > 1`. |
| **Cao** | `TeacherPasswordView.vue` | `changePassword()` gọi `resetForm()` ngay sau khi set `passwordMessage`, xóa luôn thông báo thành công. | Test case đổi mật khẩu thành công sẽ **fail khi assert banner `role="status"`**. |
| **Cao** | `ImportQuestionsModal.vue` | Bước 3 dùng `idx` của `filteredStagingQuestions` để thao tác trên `stagingQuestions`. | Test lọc loại câu hỏi ở Bước 3 rồi bỏ tick / xóa câu hỏi sẽ xóa nhầm câu hỏi khác. |
| **Trung bình** | `LoginView.vue` | Google OAuth redirect Admin tới `admin-payroll` thay vì `dashboard`; không dùng `route.query.redirect`; chưa có Quên mật khẩu. | Cần viết assertion điều hướng khớp từng luồng hoặc thống nhất về `dashboard`. |
| **Trung bình** | `TeacherAttendanceReportView.vue` & `TeacherNoticesView.vue` | Báo cáo chuyên cần Teacher 100% Mock; Tab `sent`/`center` & Compose thông báo của Teacher cũng Mock. | Không viết E2E kiểm chứng lưu DB cho 2 phần này cho tới khi nối API thật; chỉ test tab `?tab=inbox` của `TeacherNoticesView`. |
| **Trung bình** | `ParentStudentView`, `TeacherManagementView`, `ClassDetailView` | Các modal inline thiếu `role="dialog"`, `ParentStudentView` thiếu `isSaving` lock. | Khó bắt modal chuẩn theo ARIA và dễ dính race nếu test bấm Lưu 2 lần. |
| **Thấp** | `GoogleCalendarView.vue` | Mini-calendar 35 ô cắt mất ngày 30/31; cluster giữ giờ kết thúc lịch đầu tiên; vạch giờ hiện tại vẽ ở mọi tuần. | Không nên assert số lượng ngày cuối tháng trên mini-calendar 6 tuần hoặc giờ kết thúc trên thẻ ngăn xếp trùng lịch. |
| **Thấp** | `AdminProfileView.vue` | Lỗi form Thông tin cơ bản hiển thị ở card Avatar phía trên. | Selector assert lỗi form phải quét phạm vi trang hoặc card Avatar, không chỉ trong `<form>`. |

---

## 3. Ma trận Kế hoạch Kiểm thử Tự động E2E Đề xuất

### Nhóm P0 — Smoke, Auth & Bảo mật Route
1. **Login thành công Admin & Teacher**: nhập SĐT/mật khẩu hợp lệ (`#phone`, `#password`), kiểm tra lưu `sessionStorage`, điều hướng đúng `/` và `/teacher`, tên hiển thị ở `Header.vue`.
2. **Login thất bại & Validate input**: sai định dạng SĐT, bỏ trống mật khẩu, sai thông tin đăng nhập -> assert `role="alert"`; thử login tài khoản Parent -> assert từ chối truy cập web.
3. **Phân quyền Route Guard**:
   - Chưa đăng nhập truy cập `/finance/payroll` -> chuyển về `/login?redirect=/finance/payroll`.
   - Teacher truy cập `/users/roles`, `/finance/invoices` -> tự chuyển về `/teacher`.
4. **Logout sạch phiên**: bấm Đăng xuất ở `Header`/`Sidebar` -> kiểm tra xóa sạch `sessionStorage`, ngắt SignalR, không nhấn Back vào lại trang cũ được.

### Nhóm P1 — Luồng Đào tạo & Học vụ Cốt lõi (End-to-End Xuyên Vai)
1. **Khóa học -> Cấp độ -> Lớp học -> Lịch học -> Buổi học**:
   - Admin tạo/lọc Khóa học (`/training/courses`), tạo Lớp học (`/training/classes`), vào `ClassDetailView` gán Giáo viên, thêm Học viên, tạo Lịch học cố định (kiểm tra Backend sinh danh sách buổi học không lỗi), tạo 1 Buổi học đặc biệt (`isImportan = true`).
2. **Điểm danh & Chuyên cần**:
   - Chuẩn bị dữ liệu buổi học trong vòng 24h tại `/teacher/attendance`: cập nhật trạng thái từng học viên, lưu hàng loạt (xử lý dialog `confirm` nếu còn `UNMARKED`), mở lịch sử điểm danh học sinh.
   - Verify buổi học quá 24h phải bị khóa (`disabled`).
3. **Ngân hàng câu hỏi -> Giao bài tập -> Chấm điểm**:
   - Tạo ngân hàng + câu hỏi (`MULTIPLE_CHOICE`, `TRUE_FALSE`, `FILL_IN`) tại `/teacher/questions` (kèm test AI Import bằng route mock `/api/parser/*`).
   - Tạo bài tập từ câu hỏi vừa tạo cho một buổi học (`/teacher/assignments`), tìm kiếm học viên trong modal quản lý HS, nhân bản bài tập (`Clone`).
   - Mở `/teacher/grading` & `/teacher/grading/submission/:assignmentId/:studentId` chấm điểm + nhận xét và lưu hàng loạt.

### Nhóm P2 — Tài chính, Thông báo & Giao tiếp Realtime
1. **Bảng lương & Phiếu lương**:
   - Admin tại `/finance/payroll`: chọn tháng (Element Plus picker), lọc trạng thái email, sửa đơn giá cơ bản, thêm Thưởng/Phạt, mở modal `preview-email` (`iframe`), bấm gửi email (kiểm tra cảnh báo rõ nếu thiếu SMTP/Resend hoặc trạng thái `Pending`/`Sent` nếu có cấu hình), xử lý `ElMessageBox`.
   - Teacher tại `/teacher/payslip`: chọn đúng tháng vừa chỉnh, kiểm tra số tiền thực nhận và bảng buổi dạy khớp với Admin.
2. **Thông báo, Thông báo đặc biệt & Xem nhanh Header**:
   - Admin tại `/communication/notifications`: soạn thông báo loại `SPECIAL` (tự tick `Gửi đồng thời ra email cá nhân`), kiểm tra lịch sử hiển thị badge `Thông báo đặc biệt`.
   - Teacher đăng nhập ở session/context thứ 2: kiểm tra chuông `Header.vue` tăng badge `unreadCount`, mở dropdown xem nhanh -> bấm `Xem chi tiết` mở `NotificationDetailModal` -> `unreadCount` giảm -> bấm `Xem tất cả thông báo` đi đúng sang `/teacher/communication/notices?tab=inbox`.
3. **Chat Realtime (`/teacher/communication/chat`)**:
   - Tải danh sách hội thoại, gửi tin nhắn text, tải tin nhắn cũ, xóa tin nhắn qua modal xác nhận.
4. **Quản lý Tài khoản, Hóa đơn & Báo cáo Admin**:
   - `/users/roles`: lọc role/status, bật tắt `role="switch"` (tài khoản khác), kiểm tra admin hiện tại bị khóa thao tác tự vô hiệu hóa.
   - `/finance/invoices`: lọc trạng thái, mở modal biên lai.
   - `/reports/revenue`: đổi kỳ `3/6/12` tháng và kiểm tra download `bao-cao-doanh-thu.csv`.

---

## 4. Khuyến nghị Cải tiến Code trước khi Triển khai Automation Lớn
1. Bổ sung `data-testid` cho các nút hành động chính, modal và dòng bảng (đặc biệt thay `window.alert`/`window.confirm` sang modal/toast đồng nhất).
2. Sửa 3 bug thực tế ở Teacher: `maxAttempt` trong modal Sửa của `TeacherAssignmentsView.vue`, thứ tự `resetForm()` trong `TeacherPasswordView.vue`, và lỗi lệch index khi lọc Bước 3 của `ImportQuestionsModal.vue`.
3. Thêm `role="dialog"` và `aria-modal="true"` cho toàn bộ modal ở `ParentStudentView`, `TeacherManagementView` và `ClassDetailView`.
4. Thay thế hoặc gắn cờ rõ cho 2 màn hình mock (`TeacherAttendanceReportView` và tab `sent`/`center` của `TeacherNoticesView`) bằng API thật.
