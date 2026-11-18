# 02. Kiến trúc API Services, Realtime & Utilities (`src/services`, `src/composables`, `src/utils`)

---

## 1. HTTP Client lõi (`src/services/api.service.js`)

- **Base URL**: `import.meta.env.VITE_API_BASE_URL || "http://localhost:5114"`. Các service truyền path tương đối (vd `/api/classes`) sẽ ghép vào `BACKEND_URL`.
- **Headers mặc định**: `Content-Type: application/json`, `Accept: application/json` (riêng upload multipart phải ghi đè header hoặc truyền `FormData` kèm cấu hình `multipart/form-data`).
- **Request Interceptor**: Tự động đính kèm `Authorization: Bearer <accessToken>` lấy từ `auth-storage.js` (`sessionStorage`).
- **Response Interceptor (Điểm lưu ý cực kỳ quan trọng khi test)**:
  - **Không có cơ chế Refresh Token tự động**.
  - Khi nhận HTTP `401`, interceptor chỉ xóa session và `window.location.replace("/login")` nếu thỏa mãn **tất cả** điều kiện: `!error.response?.data` (body rỗng), `!requestUrl.includes("/login")`, và đang có `accessToken`. Nếu server trả JSON body lỗi đi kèm `401`, interceptor sẽ `Promise.reject(error)` để view tự xử lý.

---

## 2. Danh mục đầy đủ API Services (`src/services/*`)

### 2.1 Auth, Tài khoản & Hồ sơ

| Service | Base Path | Method & Endpoint | Params / Body & Ghi chú |
| :--- | :--- | :--- | :--- |
| `auth.service.js` | `/api/auth` | `login(data)` -> `POST /login`<br>`googleLogin(idToken)` -> `POST /google-login`<br>`register(data)` -> `POST /register`<br>`changePassword(data)` -> `POST /change-password` | `login` & `googleLogin` chặn mọi role ngoài `admin`/`teacher`. `login` **không** lưu `userInfo` vào `sessionStorage`, `googleLogin` **có** lưu `{ email, fullName, role, profileId }`. |
| `profile.service.js` | `/api/profile` | `get()` -> `GET /`<br>`update(data)` -> `PUT /`<br>`updateEmail(data)` -> `PUT /email`<br>`resendEmailVerification()` -> `POST /api/email-verification/resend`<br>`updatePin(data)` -> `PUT /pin`<br>`verifyPin(data)` -> `POST /pin/verify`<br>`uploadAvatar(formData)` -> `POST /avatar`<br>`deleteAvatar()` -> `DELETE /avatar` | `verifyPin` đang chú thích là "đặt chỗ tạm". `uploadAvatar` gửi `multipart/form-data`. |
| `admin-account.service.js` | `/api/admin/accounts` | `getAll(keyword)` -> `GET /`<br>`getPaged({ page=1, pageSize=20, keyword, role, isActive })` -> `GET /paged`<br>`updateStatus(id, isActive)` -> `PUT /${id}/status`<br>`resetPassword(id)` -> `POST /${id}/reset-password` | Lọc bỏ giá trị `'ALL'` / rỗng trước khi gửi query. |

### 2.2 Người dùng (Phụ huynh, Học sinh, Giáo viên)

| Service | Base Path | Method & Endpoint | Ghi chú kỹ thuật & Rủi ro |
| :--- | :--- | :--- | :--- |
| `parent.service.js` | `/api/parents` | `getAll(keyword)` -> `GET /?keyword=...`<br>`getPaged({ page=1, pageSize=20, keyword, isActive })` -> `GET /paged`<br>`getStudentsPaged({ page=1, pageSize=20, keyword, parentAccountId })` -> `GET /students/paged`<br>`getAllStudents(keyword)` -> `GET /students?keyword=...`<br>`get(id)` -> `GET /${id}`<br>`create(data)` -> `POST /`<br>`update(id, data)` -> `PUT /${id}`<br>`updateStatus(id, isActive)` -> `PUT /${id}/status`<br>`updateEmail(id, data)` -> `PUT /${id}/email`<br>`delete(id)` -> `DELETE /${id}` | **`getAll` & `getAllStudents` nối chuỗi URL trực tiếp không `encodeURIComponent`** (dễ lỗi nếu keyword có ký tự `&`, `#`, `+`). `getAllStudents` có fallback gọi `getAll()` rồi flatten mảng `students`. |
| `teacher.service.js` | `/api/teachers` | `getAll(keyword)` -> `GET /?keyword=...`<br>`getPaged({ page=1, pageSize=20, keyword, isActive })` -> `GET /paged`<br>`get(id)` -> `GET /${id}`<br>`create(data)` -> `POST /`<br>`update(id, data)` -> `PUT /${id}`<br>`updateStatus(id, isActive)` -> `PUT /${id}/status`<br>`delete(id)` -> `DELETE /${id}` | `getAll` cũng nối chuỗi query thô không encode. |
| `student.service.js` | `/api/students` | `getAll`, `get(id)`, `create`, `update`, `uploadAvatar`, `deleteAvatar`, `delete` | **Chưa được import ở bất kỳ file nào (`0 refs`)**. |

### 2.3 Đào tạo (Khóa học, Lớp học, Lịch học, Buổi học)

| Service | Base Path | Method & Endpoint | Ghi chú kỹ thuật |
| :--- | :--- | :--- | :--- |
| `course.service.js` | `/api/courses` | `getAll()` -> `GET ""`<br>`get(id)` -> `GET /${id}`<br>`uploadMedia(file, type)` -> `POST /media`<br>`create(data)` -> `POST ""`<br>`update(id, data)` -> `PUT /${id}`<br>`delete(id)` -> `DELETE /${id}` | `uploadMedia` gửi `FormData` (`file`, `type`) lên Cloudinary thông qua Backend. |
| `courseLevel.service.js` | `/api/courselevels` | `getByCourse(courseId)` -> `GET /course/${courseId}`<br>`get(id)` -> `GET /${id}`<br>`create/update/delete` | Quản lý cấp độ trong khóa học. |
| `class.service.js` | `/api/classes` | `getAll()` -> `GET /`<br>`getPaged({ page=1, pageSize=12, ... })` -> `GET /paged`<br>`getByCourseLevel(levelId)` -> `GET /course-level/${levelId}`<br>`get/create/update/delete`<br>`getStudents(classId)` -> `GET /${classId}/students`<br>`getStudentsPaged(classId, { page=1, pageSize=20, keyword })` -> `GET /${classId}/students/paged`<br>`addStudent(classId, studentId)` -> `POST /${classId}/students`<br>`removeStudent(classId, studentId)` -> `DELETE /${classId}/students/${studentId}`<br>`getAssignedClasses(userProfile, teacherProfileId)` | **`getPaged` mặc định `pageSize = 12`**. Lưu ý: **`getAssignedClasses` trả về toàn bộ danh sách lớp nếu bộ lọc theo giáo viên ra rỗng (`assigned.length === 0`)** — có thể làm giáo viên chưa được phân lớp vẫn thấy mọi lớp trên màn hình gọi hàm này. |
| `classSchedule.service.js` | `/api/classschedules` | `getByClass(classId)` -> `GET /class/${classId}`<br>`get(id)` -> `GET /${id}`<br>`create` -> `POST ""`<br>`update/delete` | Tạo lịch sẽ kích hoạt Backend tự sinh `LESSON` theo `Syllabus`. |
| `lesson.service.js` | `/api/lessons` | `getAll()` -> `GET ""`<br>`getPaged({ page=1, pageSize=20, classId, status, keyword })` -> `GET /paged`<br>`getByClass(classId)` -> `GET /class/${classId}`<br>`get/create/update/delete` | Buổi học đánh dấu `isImportan = true` sẽ kích hoạt thông báo đặc biệt (`SPECIAL`) + email. |
| `scheduleSync.helper.js` | — | `calculateScheduleSessions(startDate, schedules, totalSessions)`<br>`syncClassLessonsAndEndDate(classId, opts)` | Gọi `classService` + `lessonService` để tính lại `endDate` (chỉ cập nhật khi lệch > 60s) và đồng bộ các buổi học `SCHEDULED` từ hôm nay. Luôn gửi kèm field chính tả `isImportan`. |

### 2.4 Học vụ, Điểm danh, Nghỉ phép, Ngân hàng câu hỏi & Bài tập

| Service | Base Path | Method & Endpoint | Ghi chú |
| :--- | :--- | :--- | :--- |
| `attendance.service.js` | `/api/attendance` | `getByLesson(lessonId)` -> `GET /lesson/${lessonId}`<br>`saveBatchByLesson(lessonId, items)` -> `POST /lesson/${lessonId}`<br>`saveBatch(payload)` -> `POST /batch`<br>`updateSingle(lessonId, studentId, data)` -> `PUT /lesson/${lessonId}/student/${studentId}`<br>`getStudentHistory(studentId, classId)` -> `GET /student/${studentId}` | UI điểm danh cho phép lưu hàng loạt hoặc từng học sinh, khóa chỉnh sửa sau 24h kết thúc buổi học. |
| `leaveRequest.service.js` | `/api/leaverequests` | `getPending()` -> `GET /pending`<br>`getPendingPaged({ page=1, pageSize=20 })` -> `GET /pending/paged`<br>`process(id, isApproved)` -> `PUT /${id}/process?isApproved=...` | Chỉ mới thấy `TeacherDashboardView` gọi `getPending()`. |
| `questionBank.service.js` | `/api/question-banks` | Bank CRUD: `getAll`, `getPaged` (`pageSize=10`), `getById`, `create`, `update`, `delete`<br>Questions: `getQuestions`, `getQuestionsPaged` (`pageSize=20`), `getQuestionById`, `createQuestion`, `bulkCreateQuestions` (`POST /questions/bulk`), `updateQuestion`, `deleteQuestion`, `bulkDeleteQuestions` (`POST /questions/bulk-delete`) | Hỗ trợ phân trang cả danh sách ngân hàng lẫn câu hỏi bên trong. |
| `assignment.service.js` | `/api/assignments` | `getByLesson`, `getById`, `create`, `clone` (`POST /clone`), `update`, `delete`<br>`assignStudents` (`POST /${id}/students`), `unassignStudent` (`DELETE /${id}/students/${studentId}`)<br>`getByStudent`, `gradeStudent` (`PUT /${id}/grade/${studentId}`), `batchGrade` (`PUT /${id}/batch-grade`), `getStudentSubmission` (`GET /${id}/submission/${studentId}`) | Phục vụ đầy đủ tạo đề, clone sang buổi khác, gán học sinh & chấm điểm. |

### 2.5 Tài chính, Báo cáo, Giao tiếp & Service gọi trực tiếp ở View

| Service / View tiêu thụ trực tiếp | Base Path | Method & Endpoint | Ghi chú |
| :--- | :--- | :--- | :--- |
| `admin-invoice.service.js` | `/api/admin/invoices` | `getAll()` -> `GET /`<br>`getPaged({ page=1, pageSize=20, keyword, status })` -> `GET /paged` | Phục vụ `InvoicesView.vue`. |
| `admin-report.service.js` | `/api/admin/reports` | `getRevenue(months=6)` -> `GET /revenue`<br>`getPayrollTrend(months=6)` -> `GET /payroll`<br>`getStudentGrowth(months=6)` -> `GET /student-growth`<br>`getAttendance(days=90, classId)` -> `GET /attendance` | Phục vụ `DashboardView` & 3 màn hình Reports. |
| **View gọi thẳng `createApiClient("/api/payrolls")`** *(chưa tách file service)* | `/api/payrolls` | `DashboardView.vue`: `GET /admin/summary`<br>`AdminPayrollView.vue`: `GET /admin/summary/paged`, `GET /detail`, `PUT /teachers/${id}/base-rate`, `POST /adjustments`, `GET /${id}/preview-email` (`responseType: 'text'`), `POST /${id}/send-email`, `POST /send-batch-emails`<br>`TeacherPayslipView.vue`: `GET /detail` | 3 view gọi trực tiếp `createApiClient('/api/payrolls')` thay vì qua service chung. |
| `admin-notification.service.js` | `/api/admin/notifications` | `getHistory(params)` -> `GET ""`<br>`create(notification)` -> `POST ""` | Payload mới hỗ trợ `type: "SPECIAL"` và `sendEmail: boolean`. |
| `notification.service.js` | `/api/notifications` | `getInbox({ page=1, pageSize=20, unreadOnly=false, type })` -> `GET ""`<br>`getUnreadCount()` -> `GET /unread-count`<br>`markRead(id)` -> `PUT /${id}/read`<br>`markAllRead()` -> `PUT /read-all`<br>`getSettings()` -> `GET /settings`<br>`updateSettings(settings)` -> `PUT /settings` | Không dùng `/paged` mà trả thẳng `{ items, total, unreadCount }` từ `GET /api/notifications`. |
| `chat.service.js` | `/api/chat` | `getConversations()`, `getAdminConversations()`, `getMessages(id, page=1)`, `sendMessage(...)`, `sendAttachment(...)` (FormData), `deleteMessage(convId, msgId)` | Có helper `unwrapList` xử lý mảng hoặc `.items`. |
| `yeucaumuonsach.service.js` | `/api/yeucaumuonsach` | `getAll`, `create`, `get`, `update`, `delete` | **Legacy / không dùng (`0 refs`)**. |

---

## 3. Tầng Realtime (SignalR) & External AI Worker

### 3.1 SignalR Hubs
Cả hai hub đều dùng `@microsoft/signalr` với `accessTokenFactory: () => getAccessToken() || ""` và lịch tự động kết nối lại `[0, 2000, 5000, 10000, 20000]`:
1. **Notification Hub**: `${VITE_API_BASE_URL}/hubs/notifications` (khởi tạo từ `Header.vue` qua `notification-state.js`)
   - Event lắng nghe: `"ReceiveNotification"` -> normalize và chèn đầu mảng, tăng `unreadCount`.
2. **Chat Hub**: `${VITE_API_BASE_URL}/chathub` (khởi tạo khi mở `TeacherChatView.vue`)
   - Events lắng nghe: `"ReceiveNewMessage"`, `"MessageDeleted"`.

### 3.2 AI Question Import Worker (`src/services/importWorker.service.js`)
- Dùng `axios.create` riêng (timeout `120000ms`), **không gắn JWT token** của hệ thống.
- Host mặc định: `import.meta.env.VITE_IMPORT_WORKER_URL || "http://127.0.0.1:8000"`; nếu lỗi mạng sẽ fallback sang proxy tương đối `/api/parser/...` hoặc `/health`.
- Endpoints:
  - `GET /health` (timeout `3000ms`, kiểm tra `status === "healthy"`).
  - `POST /api/parser/process-file` (`FormData`: `file`, `bank_id`, optional `job_id`). Lưu ý: hàm này `throw` về chuỗi string thô thay vì `Error` object khi lỗi.
  - `GET /api/parser/job/${jobId}` + `pollJobUntilDone` (poll mỗi `2500ms`, tối đa `240` lần, thử lại tối đa 5 lỗi mạng liên tiếp theo exponential backoff tối đa `10000ms`).

---

## 4. State Singleton, Phân trang & Helpers

- **`notification-state.js` (`useNotificationState`)**: Singleton reactive chứa `{ items, unreadCount, total, isLoading, isInitialized, error }`. Thực hiện **optimistic update + rollback** khi đánh dấu đã đọc. Cần gọi `disconnectRealtime()` + `resetNotificationState()` khi logout.
- **`user-state.js` (`useUserState`)**: Singleton reactive `{ profile, isLoading, error, isInitialized }`.
  - **Cảnh báo rủi ro test**: Khi `profileService.get()` lỗi, `fetchProfile()` nuốt lỗi vào `console.warn` và tự gán **`getDefaultProfile(role)` với dữ liệu giả hardcoded** (`accountId: 1`, `phone: "0901234567"`, email `@toantuduy.edu.vn`), còn `updateProfile()` gán ngược `fullName` ngay cả trong khối `catch` trước khi throw lỗi.
- **`usePagination.js`**: Quản lý phân trang đồng bộ cho cả server-side (`applyPagedResult` đọc được cả camelCase & PascalCase) và client-side (`applyClientList`), mặc định `pageSize = 20`, options `[10, 20, 50, 100]`.
- **`utils/validation.helper.js`**:
  - `PHONE_REGEX`: `/^(?:\+84|0)(?:\d){9}$/`
  - `EMAIL_REGEX`: `/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/` (tối đa `256` ký tự)
  - `PIN_REGEX`: `/^\d{4,6}$/`
  - `CODE_REGEX`: `/^[A-Za-z0-9_-]+$/`
  - `validateTextLength`, `validateNumberRange`, `validateDateRange`, `validateNotFuture`.
- **`utils/media.helper.js`**:
  - `DIFFICULTY_POINTS`: `{ EASY: 1.0, MEDIUM: 1.5, HARD: 2.0 }`.
  - `parseMultipleChoice` tách đáp án A/B/C/D bằng regex; `calculateExamScore` & `normalizeScore` chuẩn hóa điểm trong `[0, 100]` làm tròn 1 chữ số thập phân; `getYouTubeVideoId` yêu cầu video ID đúng 11 ký tự.
- **`utils/notification.helper.js`**:
  - Định nghĩa `NOTIFICATION_TYPE_OPTIONS` (`SPECIAL`, `ANNOUNCEMENT`, `SYSTEM`, `ATTENDANCE`, `LEAVE_REQUEST`, `ACADEMIC`, `TUITION`, `PAYMENT`, `ENROLLMENT`, `ACCOUNT`) và `resolveNotificationActionRoute(notif, role)` điều hướng sang module nghiệp vụ tương ứng khi bấm nút hành động trong `NotificationDetailModal`.
