# 01. Router, Phân quyền & Shell Giao diện (`Router`, `Layout`, `Sidebar`, `Header`, `Account`)

---

## 1. Bản đồ Route (`src/router/index.js`)

Ứng dụng sử dụng `createWebHistory` chia thành 3 nhóm route: Công khai (`/login`), **Teacher Portal** (`/teacher/*`, `meta: { requiresAuth: true, teacherOnly: true }`), và **Admin Portal** (`/*`, `meta: { requiresAuth: true, adminOnly: true }`).

### 1.1 Route công khai & Teacher Portal (`/teacher`)

| Path đầy đủ | Route Name | Component | Tiêu đề (`meta.title`) | Ghi chú |
| :--- | :--- | :--- | :--- | :--- |
| `/login` | `login` | `LoginView.vue` | — | Đăng nhập SĐT/mật khẩu & Google OAuth |
| `/teacher` | `teacher-dashboard` | `TeacherDashboardView.vue` | `Tổng quan Giáo viên` | Trang chủ Teacher |
| `/teacher/schedule` | `teacher-schedule` | `TeacherScheduleView.vue` | `Thời khóa biểu cá nhân` | Đọc `query.classId` |
| `/teacher/payslip` | `teacher-payslip` | `TeacherPayslipView.vue` | `Phiếu lương cá nhân` | Element Plus |
| `/teacher/classes` | `teacher-classes` | `TeacherClassesView.vue` | `Lớp học đang phụ trách` | Phân trang client (9/trang) |
| `/teacher/students` | `teacher-students` | `TeacherStudentsView.vue` | `Danh sách học sinh theo lớp` | Đọc `query.classId` |
| `/teacher/attendance` | `teacher-attendance` | `TeacherAttendanceView.vue` | `Điểm danh học viên` | Đọc `query.lessonId`, `query.classId` |
| `/teacher/questions` | `teacher-questions` | `TeacherQuestionsView.vue` | `Ngân hàng câu hỏi & Bài tập` | Kèm modal AI Import |
| `/teacher/assignments` | `teacher-assignments` | `TeacherAssignmentsView.vue` | `Giao bài tập về nhà` | CRUD + gán học sinh + clone |
| `/teacher/grading` | `teacher-grading` | `TeacherGradingView.vue` | `Chấm điểm & Nhập nhận xét` | Chấm inline & batch |
| `/teacher/grading/submission/:assignmentId/:studentId` | `teacher-submission-detail` | `TeacherSubmissionDetailView.vue` | `Chi tiết bài làm học sinh` | Đọc params `assignmentId`, `studentId` |
| `/teacher/reports/attendance` | `teacher-attendance-report` | `TeacherAttendanceReportView.vue` | `Thống kê chuyên cần lớp học` | **Dữ liệu 100% Mock** |
| `/teacher/communication/notices` | `teacher-notices` | `TeacherNoticesView.vue` | `Gửi thông báo & Nhắc nhở` | Tab `?tab=inbox` thật; tab `sent`/`center` mock |
| `/teacher/communication/chat` | `teacher-chat` | `TeacherChatView.vue` | `Chat với phụ huynh` | SignalR + đọc `query.parentId` |
| `/teacher/notifications` | — | *Redirect* | — | Redirect tới `/teacher/communication/notices?tab=inbox` |
| `/teacher/account/profile` | `teacher-profile` | `TeacherProfileView.vue` | `Hồ sơ cá nhân & Chuyên môn` | Hồ sơ giáo viên |
| `/teacher/account/password` | `teacher-password` | `TeacherPasswordView.vue` | `Đổi mật khẩu / Mã PIN` | Có bug ẩn banner thành công |

### 1.2 Admin Portal (`/`)

| Path đầy đủ | Route Name | Component | Tiêu đề (`meta.title`) | Ghi chú |
| :--- | :--- | :--- | :--- | :--- |
| `/` | `dashboard` | `DashboardView.vue` | `Tổng quan` | Trang chủ Admin |
| `/users/students` | — | `ParentStudentView.vue` | `Phụ huynh & Học sinh` | CRUD phụ huynh & xem học sinh |
| `/users/teachers` | — | `TeacherManagementView.vue` | `Giáo viên` | CRUD giáo viên |
| `/users/roles` | — | `AccountSecurityView.vue` | `Tài khoản & Bảo mật` | Khóa/mở khóa, reset MK |
| `/training/courses` | `courses` | `CoursesView.vue` | `Danh mục Khóa học` | Grid/List + lọc client |
| `/training/courses/:id` | `course-detail` | `CourseDetailView.vue` | `Chi tiết Khóa học` | Chi tiết + cấp độ + lớp |
| `/training/classes` | `classes` | `ClassesView.vue` | `Danh sách Lớp học` | Phân trang server (12/trang) |
| `/training/classes/:id` | `class-detail` | `ClassDetailView.vue` | `Chi tiết Lớp học` | Lịch học, buổi học, học viên |
| `/training/schedule` | `schedule` | `ScheduleView.vue` | `Lịch học & Thời khóa biểu` | Lịch tuần/ngày & bảng; đọc `query.classId` |
| `/finance/invoices` | — | `InvoicesView.vue` | `Hóa đơn & Biên lai` | Xem + in biên lai |
| `/finance/payroll` | `admin-payroll` | `AdminPayrollView.vue` | `Quản lý Lương Giáo viên` | Element Plus + gửi mail lương |
| `/communication/notifications` | — | `AdminNotificationsView.vue` | `Quản lý Thông báo` | Gửi thông báo (`SPECIAL`, `ANNOUNCEMENT`, `SYSTEM`) + tab `?tab=inbox` |
| `/notifications` | — | *Redirect* | — | Redirect tới `/communication/notifications?tab=inbox` |
| `/reports/revenue` | — | `RevenueReportView.vue` | `Báo cáo Doanh thu` | Kèm xuất CSV |
| `/reports/growth` | — | `StudentGrowthReportView.vue` | `Tăng trưởng học viên` | Kèm biểu đồ |
| `/reports/attendance` | — | `AttendanceReportView.vue` | `Thống kê chuyên cần` | Lọc kỳ & lớp |
| `/account/profile` | `account-profile` | `AdminProfileView.vue` | `Thông tin cá nhân` | Hồ sơ Admin |

### 1.3 View chưa gắn vào Router (Orphan / Dead Code)
- `src/views/PlaceholderView.vue` — Không còn route nào sử dụng.
- `src/views/account/ProfileView.vue` & `src/views/account/PasswordView.vue` — Không được `router/index.js` tham chiếu (router dùng `AdminProfileView`, `TeacherProfileView`, `TeacherPasswordView`); bên trong còn link cứng tới `/account/password` vốn **không tồn tại** trong router (nếu truy cập sẽ rơi về trang chủ theo fallback `!to.matched.length`).

---

## 2. Cơ chế Xác thực, Session & Navigation Guard

### 2.1 Lưu trữ Session (`src/services/auth-storage.js`)
Ứng dụng **chỉ dùng `sessionStorage`** (không dùng `localStorage` hay cookie). Ý nghĩa rất lớn với E2E test: mỗi tab/context trình duyệt mới mặc định chưa đăng nhập, và đóng tab là mất phiên.

| Khóa `sessionStorage` | Kiểu dữ liệu | Mô tả |
| :--- | :--- | :--- |
| `toanTuDuy.accessToken` | `string` | JWT Access Token |
| `toanTuDuy.role` | `string` | Vai trò chuẩn hóa từ Backend (`Admin`, `Teacher`, ...) |
| `toanTuDuy.profileId` | `string` (parse sang `int`) | ID của `AdminProfile` hoặc `TeacherProfile` |
| `toanTuDuy.userInfo` | JSON string | Chỉ được lưu khi đăng nhập qua **Google OAuth** (`{ email, fullName, role, profileId }`); login thường không ghi khóa này |

### 2.2 Logic Guard (`router.beforeEach`)
1. `authService.isAuthenticated()` kiểm tra có `accessToken` và `role.toLowerCase()` thuộc `["admin", "teacher"]`. Nếu có token nhưng role không hợp lệ (ví dụ `parent`), session bị xóa ngay lập tức.
2. Chưa đăng nhập mà vào route khác `login` -> chuyển hướng `{ name: "login", query: { redirect: to.fullPath } }`.
3. Đã đăng nhập mà vào `/login` -> chuyển về `dashboard` (Admin) hoặc `teacher-dashboard` (Teacher).
4. Route `meta.adminOnly` mà `!isAdmin` -> chuyển về `teacher-dashboard`.
5. Route `meta.teacherOnly` mà `!isTeacher && !isAdmin` -> chuyển về `dashboard` (lưu ý: **Admin vẫn có quyền mở các route `/teacher/*`** theo điều kiện `!isTeacher && !isAdmin`).
6. Route không khớp (`!to.matched.length`) -> chuyển về trang chủ theo role.

> **Lưu ý khi test `LoginView.vue`**:
> - Form thường dùng `#phone` (`pattern="(\+84|0)[0-9]{9}"`, `maxlength="12"`) và `#password` (`maxlength="128"`). Sau khi login thành công: `admin` -> `dashboard` (`/`), `teacher` -> `teacher-dashboard` (`/teacher`). **Chú ý**: `LoginView` hiện **không đọc `route.query.redirect`** sau khi đăng nhập thành công mà luôn đẩy về trang chủ theo role.
> - Đăng nhập Google (`handleGoogleSuccess`) điều hướng `admin` sang **`admin-payroll`** (`/finance/payroll`) thay vì `dashboard` (`/`), còn `teacher` sang `teacher-dashboard`.
> - **Chưa có tính năng "Quên mật khẩu"** trên UI `LoginView.vue`.

---

## 3. Shell Giao diện (`Layout.vue`, `Sidebar.vue`, `Header.vue`)

### 3.1 `Layout.vue` & `Sidebar.vue`
- Breakpoint responsive là **`lg` (`1024px`)**: `<1024px` Sidebar là fixed drawer (`w-72`), có backdrop đóng mở qua `mobileOpen`; `>=1024px` tự tắt `isMobileOpen` và hỗ trợ thu gọn (`lg:w-20` vs `lg:w-64`).
- `Sidebar.vue` xác định portal hiển thị theo `isTeacherPortal`: URL bắt đầu bằng `/teacher` hoặc không thuộc `/training`, `/users`, `/reports`, `/finance` kèm role là `teacher`.
- Các nhóm menu mở mặc định: `['class-schedule', 'training', 'users']`.
- Cả `Sidebar.vue` và `Header.vue` khi bấm Đăng xuất đều thực hiện tuần tự: `await disconnectRealtime()` -> `resetNotificationState()` -> `authService.logout()` -> `router.replace({ name: 'login' })`.

### 3.2 `Header.vue`
- Tiêu đề trang hiển thị theo `route.meta?.title ?? routeTitleMap[route.path] ?? 'Toán Tư Duy'`.
- Đồng hồ thời gian thực (`vi-VN`, 24h) cập nhật mỗi `1000ms` (ẩn trên mobile `< sm`).
- Tự động gọi `profileService.get()` khi mount, đổi `route.fullPath`, và lắng nghe sự kiện `window` `'admin-profile-updated'`.
- **Chuông thông báo**:
  - Mount gọi `fetchRecentNotifications({ pageSize: 10 })` + `connectRealtime()` (SignalR).
  - Mở dropdown sẽ gọi lại `fetchRecentNotifications({ pageSize: 10, force: true })`.
  - Hỗ trợ tab `Tất cả` / `Chưa đọc` (hiển thị tối đa 8 mục), nút làm mới, nút "Đánh dấu tất cả đã đọc", và link "Xem tất cả thông báo →" đi tới:
    - Teacher: `/teacher/communication/notices?tab=inbox`
    - Admin: `/communication/notifications?tab=inbox`
  - Nhấn vào một thông báo hoặc nút `Xem chi tiết` sẽ mở `NotificationDetailModal` và tự gọi `markNotificationRead(notif.id)`.

---

## 4. Components Dùng Chung (`common/*` & `notification/*`)

| Component | Props chính | Events | Điểm phục vụ Selector E2E |
| :--- | :--- | :--- | :--- |
| `SearchBar.vue` | `modelValue`, `placeholder` | `update:modelValue` | `<input maxlength="100">` tìm theo `placeholder` |
| `PaginationBar.vue` | `page` (req), `pageSize` (req), `totalCount`, `totalPages`, `pageSizeOptions` (`[10,20,50,100]`), `itemLabel`, `disabled`, `showWhenEmpty` | `update:page`, `update:pageSize`, `change` | `<select aria-label="Số dòng mỗi trang">`, nút trang `Trước`/`Sau` hoặc số trang |
| `ViewToggle.vue` | `modelValue` (`'grid'`) | `update:modelValue` (`'list'`\|`'grid'`) | Nút có thuộc tính `:title="mode.label"` |
| `EmptyState.vue` | `icon`, `message`, `sub` | — | Text `message` & `sub` |
| `LoadingSpinner.vue` | `label` (`'Đang tải...'`) | — | Text `label` |
| `NotificationDetailModal.vue` | `open`, `notification`, `userRole`, `showViewAll` | `close` | Teleport vào `body`, `role="dialog"`, `aria-modal="true"`, `aria-labelledby="notif-detail-title"`, nút `aria-label="Đóng"`, đóng bằng `Escape`/backdrop |
| `NotificationInboxPanel.vue` | — | — | Dùng `<select id="notif-inbox-type-filter">`, tab `Tất cả` / `Chưa đọc`, đọc `route.query.id` để tự mở `NotificationDetailModal` |
