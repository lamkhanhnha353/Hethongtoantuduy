# 03. Review Màn hình & Luồng Nghiệp vụ Admin (16 Views + 10 Shared Components)

> Toàn bộ **16/16 màn hình Admin** đều gọi **API thật**, không dùng mock. Tuy nhiên một số modal hoặc nút xóa dùng `window.alert`/`window.confirm` thuần, hoặc Element Plus (`AdminPayrollView.vue`).

---

## 1. Tổng quan & Tài khoản (`DashboardView`, `AdminProfileView`)

### 1.1 `DashboardView.vue` (`/`)
- **API**: `GET /api/payrolls/admin/summary`, `adminReportService.getRevenue(6)`, `adminReportService.getStudentGrowth(6)`, `adminReportService.getPayrollTrend(6)`, `chatService.getAdminConversations()`.
- **UI**: 4 thẻ KPI (`aria-label="Chỉ số tổng quan"`), 2 biểu đồ `vue-chartjs` (`Bar`, `Doughnut`, `aria-label="Biểu đồ thống kê"`), bảng Top 5 bảng lương và tối đa 4 hội thoại từ Phụ huynh (`lastMessageSenderType === 'PARENT'`).
- **Rủi ro E2E**:
  - `revenueChartData` lấy trục tháng theo `monthlyRevenue`; tháng nào chỉ có trong `payrollTrend` sẽ bị bỏ qua.
  - Nếu 1 trong 2 API `/admin/summary` hoặc `getPayrollTrend(6)` lỗi, `dashboardErrors.payroll` chặn hiển thị cả biểu đồ lẫn bảng lương gần đây.

### 1.2 `AdminProfileView.vue` (`/account/profile`)
- **API**: `profileService.get()`, `profileService.update({ fullName, phone })`, `profileService.updateEmail({ email })`, `profileService.resendEmailVerification()`, `profileService.uploadAvatar(FormData)`, `profileService.deleteAvatar()`, `authService.changePassword(passwordForm)`.
- **Validation**: Avatar `image/jpeg|png|webp` (<= `5MB`); `fullName` (2–100 ký tự); `phone` (`validatePhone`); `email` (`validateEmail`); mật khẩu mới `6..128` ký tự & khớp xác nhận. Phát sự kiện `window` `'admin-profile-updated'` để `Header.vue` đồng bộ lại tên/avatar.
- **Lỗi UX cần chú ý khi test**:
  - `profileError` và `profileMessage` được render trong **card Avatar (#1)** thay vì card **Thông tin cơ bản (#2)** — khi test validation lỗi ở form đổi tên/SĐT, phải kiểm tra `role="alert"` ở card phía trên.
  - Khi đang có `pendingEmail`, ô Email điền sẵn `pendingEmail` và nút submit bị disable nếu giá trị trùng `profile.pendingEmail`.

---

## 2. Quản lý Người dùng (`users/*`)

| Màn hình & Route | API sử dụng | Tìm kiếm / Lọc / Phân trang | Dialog / Alert | Lưu ý & Rủi ro E2E |
| :--- | :--- | :--- | :--- | :--- |
| **`ParentStudentView.vue`**<br>`/users/students` | `parentService.getPaged`, `update`, `delete` | `SearchBar` (debounce `300ms`) + `PaginationBar` (20/trang) | **Native `alert` / `confirm`** | Modal Sửa (`fullName` req max 100, `pin` optional 4–6 số) **thiếu `role="dialog"`**, không đóng được khi bấm backdrop và **thiếu cờ `isSaving`** (có thể double-submit). |
| **`TeacherManagementView.vue`**<br>`/users/teachers` | `teacherService.getPaged`, `create`, `update`, `delete` | `SearchBar` (debounce `300ms`) + status filter (`ALL\|true\|false`) + `PaginationBar` (20/trang) | **Native `alert` / `confirm`** | Trường chuyên môn (`specialize`, max 100) **chỉ hiển thị khi Sửa**, không thể nhập lúc Tạo mới giáo viên. Modal Tạo/Sửa **thiếu `role="dialog"`** và backdrop close. |
| **`AccountSecurityView.vue`**<br>`/users/roles` | `adminAccountService.getPaged`, `updateStatus`, `resetPassword` | Search (debounce `300ms`) + Role (`ALL\|Admin\|Teacher\|Parent`) + Status (`ALL\|true\|false`) + `PaginationBar` | **Native `confirm`** + banner `role="status"` | Toggle khóa/mở dùng `role="switch"` + `:aria-checked`. Tự bảo vệ không cho khóa chính Admin đang đăng nhập (`isCurrentAdmin`). Nút Reset mật khẩu chỉ bật cho `Teacher`/`Parent` đã xác minh email. |

---

## 3. Quản lý Đào tạo & Lịch học (`training/*` + Shared Components)

### 3.1 Danh mục Khóa học & Chi tiết (`CoursesView`, `CourseDetailView`, `CourseModal`)
- **`CoursesView.vue` (`/training/courses`)**:
  - Gọi `courseService.getAll()` (không phân trang server). Tìm kiếm, lọc (`courseId`, `status`, `feeMin`, `feeMax`), sắp xếp và chuyển đổi `ViewToggle` (`grid` | `list`) xử lý toàn bộ ở client.
  - Quirk: Khi `feeMin > feeMax`, hiển thị `feeRangeError` nhưng bỏ qua bộ lọc học phí thay vì trả danh sách rỗng; banner lỗi kết nối hardcode text `"cổng localhost 5114"`.
- **`CourseDetailView.vue` (`/training/courses/:id`)**:
  - Gọi `courseService.get(id)`, `courseLevelService.getByCourse(id)` (nếu thiếu trong `course`), `classService.getPaged({ pageSize: 12, ... })`.
  - Chọn thẻ Cấp độ sẽ toggle `selectedLevel` và cuộn tới `#classes-section`.
  - Quirk: `totalClassStudents` chỉ cộng `currentStudents` của **trang lớp học hiện tại**, không tính toàn bộ các trang; không có `watch(() => route.params.id)`; lỗi `loadClasses` chỉ `console.error` (hiển thị `EmptyState` thay vì thông báo lỗi).
- **`CourseModal.vue` (`role="dialog"`, thiếu `aria-modal`)**:
  - Upload ảnh bìa (`<=10MB`) và video intro (`<=100MB`) hoặc ảnh cơ sở vật chất **ngay khi chọn file** qua `courseService.uploadMedia` lên Cloudinary (trước khi bấm Lưu); nút Đóng/Lưu bị khóa trong lúc đang upload. Delete dùng **native `window.confirm`**. Có `setTimeout(700ms)` đóng modal sau khi hiện thông báo thành công (nhưng `saving` tắt ở `finally` trước khi hết 700ms).

### 3.2 Quản lý Lớp học & Chi tiết (`ClassesView`, `ClassDetailView`, `ClassModal`, `LessonModal`)
- **`ClassesView.vue` (`/training/classes`)**:
  - Gọi `classService.getPaged({ pageSize: 12, ... })` + `courseService.getAll()`, tìm kiếm debounce `300ms`, bộ lọc (`courseId`, `status`, `capacity`, `sort`). Lỗi API chỉ `console.error` và hiển thị `EmptyState`.
- **`ClassModal.vue` (`role="dialog"`)**:
  - Có thanh `SearchBar` lọc nhanh danh sách giáo viên ngay trên form tạo/sửa lớp; validate `maxCapacity` (1..100), `startDate <= endDate`. Xóa lớp dùng **native `window.confirm`**. `formatDateForInput` dùng `.toISOString().split('T')[0]` (có nguy cơ lệch ngày 1 hôm theo múi giờ UTC).
- **`ClassDetailView.vue` (`/training/classes/:id`)**:
  - Màn hình nặng nhất của phân hệ Đào tạo: nạp song song lớp, cấp độ, khóa học, giáo viên, lịch cố định (`classScheduleService`), buổi học (`lessonService`), danh sách học sinh trong lớp (`classService.getStudents`).
  - Có **3 tabs**:
    1. `schedules`: Danh sách lịch cố định + lưới trực quan 7 ngày + nút mở `ScheduleModal`.
    2. `lessons`: Tìm kiếm client + danh sách buổi học + nút mở `LessonModal` + nút xóa buổi (chặn xóa bằng **native `alert`** nếu `attendancesCount > 0`).
    3. `students`: Tìm kiếm client + bảng học sinh + modal thêm học sinh (`parentService.getStudentsPaged` có `SearchBar` debounce `300ms`) + modal gán giáo viên (`teacherService.getAll`, có thanh tìm kiếm client).
  - Rủi ro E2E: Hai modal inline (Thêm học sinh, Phân công giáo viên) **không có `role="dialog"`**; `triggerAutoSync()` lỗi chỉ ghi `console.error`; chưa `watch(() => route.params.id)`.
- **`LessonModal.vue` (`role="dialog"`)**:
  - Form buổi học (`lessonDate` giới hạn `-1 năm .. +2 năm`, `startTime < endTime`, `status`, checkbox `isImportan` — khi bật cờ này Backend tự thả Thông báo đặc biệt + email cá nhân). Chặn xóa ở client bằng `alert` nếu đã có điểm danh; dùng `confirm` khi xóa.

### 3.3 Lịch học Tổng hợp (`ScheduleView`, `GoogleCalendarView`, `ScheduleModal`)
- **`ScheduleView.vue` (`/training/schedule`)**:
  - Nạp `classService.getAll()`, sau đó dùng `Promise.allSettled` gọi **N+1 request** `classScheduleService.getByClass(c.id)` cho từng lớp (bỏ qua âm thầm nếu lớp lẻ bị lỗi). Đọc `route.query.classId` (dùng cho chế độ `table`).
- **`GoogleCalendarView.vue`**:
  - Lưới `07:00–21:00` theo `Tuần` / `Ngày`, thanh bên mini-calendar và bộ lọc lớp bằng checkbox, gộp lịch trùng giờ thành thẻ ngăn xếp ("Ngăn xếp") mở drawer bên phải (drawer **không có `role="dialog"`**).
  - **Các bug UI đã phát hiện**:
    1. `getClustersForDay` không cập nhật lại `maxEnd` hiển thị khi gộp nhiều lịch khác giờ kết thúc, nên thẻ/drawer chỉ hiện giờ của lịch đầu tiên.
    2. `miniCalendarDays` cố định 35 ô (`5x7`), bị mất ngày `30`/`31` ở những tháng kéo dài 6 hàng tuần.
    3. `currentTimeTop` vẽ đường chỉ giờ hiện tại trên **tất cả các tuần** bất kể tuần đang xem có chứa hôm nay hay không.
    4. `watch(() => props.classes)` chỉ auto-tick checkbox lớp khi `selectedClassIds` đang rỗng.
- **`ScheduleModal.vue` (`role="dialog"`)**:
  - 3 chế độ `view` / `edit` / `create`, có `SearchBar` lọc danh sách Lớp học. Nút Đóng chưa khóa khi đang `saving`/`deleting`; sau khi lưu ở mode `edit` sẽ chờ `700ms` rồi chuyển về `view` (lúc đó có thể hiện lại data cũ vài trăm ms chờ parent reload xong).

---

## 4. Tài chính (`InvoicesView`, `AdminPayrollView`)

- **`InvoicesView.vue` (`/finance/invoices`)**:
  - Pure Tailwind + `PaginationBar` (20/trang), tìm kiếm debounce `300ms`, lọc `ALL|PENDING|PAID|FAILED|CANCELLED|REFUNDED`, modal chi tiết hóa đơn (`role="dialog"`, `aria-modal="true"`) hỗ trợ in biên lai (`window.print()`). Có guard `requestToken` và tự lùi trang nếu trang hiện tại vượt `totalPages`.
- **`AdminPayrollView.vue` (`/finance/payroll`)**:
  - Dùng hoàn toàn **Element Plus** (`el-date-picker`, `el-table`, `el-select`, `el-dialog`, `el-input-number`, `ElMessage`, `ElMessageBox.confirm`).
  - Gọi trực tiếp `createApiClient('/api/payrolls')` cho: xem bảng lương phân trang, xem chi tiết buổi dạy + thưởng/phạt, cập nhật đơn giá/buổi (`0..100,000,000` integer), thêm thưởng/phạt (`Bonus|Penalty`, `1..1,000,000,000` integer, ngày áp dụng `<=` hôm nay, lý do max 500), xem trước email HTML (`<iframe :srcdoc="previewHtml" sandbox="">`), gửi email từng giáo viên và gửi hàng loạt.
  - Quirk: `appliedDate` của điều chỉnh lương không bị khóa phải nằm trong tháng lương đang chọn (chỉ kiểm tra không vượt hôm nay).

---

## 5. Giao tiếp & Báo cáo (`AdminNotificationsView`, `reports/*`)

- **`AdminNotificationsView.vue` (`/communication/notifications`)**:
  - Hai tab: `manage` (Quản lý & gửi thông báo) và `inbox` (`<NotificationInboxPanel />` — đồng bộ qua `route.query.tab` / `route.query.id`).
  - Modal Soạn thông báo (`role="dialog"`, `aria-modal="true"`): chọn nhóm nhận (`PARENT`, `TEACHER`, `PARENT_AND_TEACHER`), loại (`ANNOUNCEMENT`, `SPECIAL`, `SYSTEM`), checkbox **"Gửi đồng thời ra email cá nhân người nhận"** (tự động bật & khóa khi chọn `SPECIAL`), tiêu đề (max 200), nội dung (max 2000). Nếu server chưa cấu hình SMTP/Resend, hiển thị banner cảnh báo màu hổ phách (`statusBannerIsWarning`).
  - Lưu ý E2E: Chuyển loại từ `SPECIAL` sang `ANNOUNCEMENT` không tự bỏ tick checkbox `sendEmail`; `onMounted` luôn gọi `loadHistory()` dù đang mở thẳng `?tab=inbox`; `loadHistory()` chưa có `requestToken` chống race.
- **3 màn hình Báo cáo (`AttendanceReportView`, `RevenueReportView`, `StudentGrowthReportView`)**:
  - Đều dùng Pure Tailwind + thanh progress CSS, gọi API thật từ `admin-report.service.js`.
  - `RevenueReportView` có nút **"Xuất CSV"** (`bao-cao-doanh-thu.csv`, BOM UTF-8) và **thiếu `requestId` guard** khi đổi nhanh `period`; `AttendanceReportView` và `StudentGrowthReportView` có `requestId` guard. Ở `AttendanceReportView`, nếu API lỗi thì `report = null` làm rỗng `classOptions` trong khi `selectedClassId` vẫn giữ ID cũ.
