# Tổng quan Review Frontend (`Web_Frontend`) — Phục vụ Hiểu Hệ thống & Kiểm thử Tự động UI

Tài liệu trong thư mục `Document/Review/Frontend` tổng hợp kết quả rà soát toàn bộ mã nguồn **90 tệp** trong `Web_Frontend/src` cùng cấu hình build/deploy (`package.json`, `vite.config.js`, `.env`, `Dockerfile`, `nginx.conf`, `UI_RULE.md`), phục vụ:
1. Nắm rõ kiến trúc ứng dụng Web (`Admin` + `Teacher` portals), cơ chế xác thực/phân quyền và luồng dữ liệu.
2. Tra cứu toàn bộ API REST, SignalR Realtime và AI Import Worker đang được Frontend tiêu thụ.
3. Chuẩn bị **kế hoạch kiểm thử giao diện tự động (Playwright / Cypress)**: selector strategy, xử lý dialog/toast, nhận diện dữ liệu thật vs mock/stub và danh sách lỗi thực tế cần lưu ý hoặc sửa trước khi viết test.

---

## 1. Mục lục bộ tài liệu

| Tệp tài liệu | Nội dung chính | Đối tượng đọc chính |
| :--- | :--- | :--- |
| [00-README.md](./00-README.md) | Kiến trúc tổng thể, tech stack, cấu hình môi trường, tổng hợp nhanh các phát hiện lớn | Tech Lead, Dev, QA |
| [01-Router-Shell-Va-Phan-Quyen.md](./01-Router-Shell-Va-Phan-Quyen.md) | Toàn bộ bảng route (`router/index.js`), role guard, Sidebar/Header, Layout, Common UI & trang Account | Dev, QA |
| [02-API-Services-Va-Realtime.md](./02-API-Services-Va-Realtime.md) | Chi tiết `api.service.js`, toàn bộ 26 module trong `src/services/`, SignalR Hubs, AI Import Worker, `usePagination` & `utils/*` | Backend/Frontend Dev, QA |
| [03-Man-Hinh-Va-Luong-Admin.md](./03-Man-Hinh-Va-Luong-Admin.md) | Review 16 màn hình Admin + 10 component đào tạo/lịch học (UI, validation, API, lỗi/rủi ro test) | Frontend Dev, QA |
| [04-Man-Hinh-Va-Luong-Teacher.md](./04-Man-Hinh-Va-Luong-Teacher.md) | Review 15 màn hình Teacher + `ImportQuestionsModal.vue` (phân tách Real API vs Mock, bẫy tự động hóa) | Frontend Dev, QA |
| [05-Ke-Hoach-Kiem-Thu-Giao-Dien-Tu-Dong.md](./05-Ke-Hoach-Kiem-Thu-Giao-Dien-Tu-Dong.md) | Chiến lược kỹ thuật Playwright/Cypress, xử lý selector/dialog/realtime, danh sách bug/mock gap & ma trận E2E test cases | QA Automation, Dev |

---

## 2. Stack công nghệ & Cấu hình chạy

### 2.1 Thư viện chính (`Web_Frontend/package.json`)
- **Framework**: `vue` `^3.5.42` (Composition API `<script setup>`), `vue-router` `^5.3.1` (Web History mode).
- **UI & Styling**:
  - Tailwind CSS v4 (`tailwindcss` `^4.3.3`, `@tailwindcss/vite` `^4.3.3`) — dùng ở phần lớn màn hình.
  - Element Plus (`element-plus` `^2.14.7`) — đăng ký global trong `main.js`, dùng tập trung ở **Quản lý lương (`AdminPayrollView`)**, **Phiếu lương (`TeacherPayslipView`)** và toast/confirm (`ElMessage`, `ElMessageBox`).
  - Icons: `@heroicons/vue` `^2.2.0`.
- **Biểu đồ, Realtime, HTTP, Auth**:
  - `axios` `^1.20.0` (HTTP client).
  - `@microsoft/signalr` `^8.0.29` (2 hub realtime: `/chathub`, `/hubs/notifications`).
  - `chart.js` `^4.6.1` + `vue-chartjs` `^5.3.4` (biểu đồ ở `DashboardView.vue`).
  - `vue3-google-signin` `^3.0.1` (Google OAuth tại `LoginView.vue`).

### 2.2 Biến môi trường & Proxy (`Web_Frontend/.env`, `vite.config.js`, `nginx.conf`)
- **Development (`npm run dev` trên cổng `5173`)**:
  - `.env` hiện tại đặt `VITE_API_BASE_URL=http://localhost:5114`.
  - `vite.config.js` cấu hình proxy:
    - `/api/parser` -> `http://127.0.0.1:8000` (Python AI Import Worker).
    - `/api` -> `VITE_API_BASE_URL` (mặc định `http://localhost:5114`).
  - Lưu ý: `api.service.js`, `chat.service.js`, `notification.service.js` tạo URL tuyệt đối theo `import.meta.env.VITE_API_BASE_URL || "http://localhost:5114"`, còn `importWorker.service.js` dùng `import.meta.env.VITE_IMPORT_WORKER_URL || "http://127.0.0.1:8000"` và chỉ fallback sang đường dẫn tương đối `/api/parser/...` khi gọi thẳng thất bại mạng.
- **Production / Docker (`Dockerfile`, `nginx.conf`)**:
  - Multi-stage `node:24-alpine` build ra static -> phục vụ bởi `nginx:1.29-alpine` ở cổng `80` (Docker Compose ánh xạ cổng `8088`).
  - `nginx.conf` cho phép upload tối đa `50m`, proxy `/api/`, `/chathub`, `/hubs/` -> `backend:8080`, `/api/ai/` -> `ai-backend:8001`, `/api/parser/` -> `import-worker:8000/api/parser/`, và SPA fallback về `/index.html` (`no-store, no-cache`).

---

## 3. Quy chuẩn UI & Tác động tới Kiểm thử Tự động

Theo `Web_Frontend/UI_RULE.md` và `src/assets/main.css`:
- Thiết kế theo phong cách **cam trên nền đá (`orange-600` `#ea580c` + `stone-*`)**, hạn chế bo tròn quá mức (`rounded-lg`, `rounded-md`).
- **Điểm cực kỳ quan trọng cho QA Automation**:
  1. **Không có `data-testid` / `data-cy`**: Toàn bộ 90 tệp hiện chưa gắn thuộc tính test chuyên biệt (`data-testid`). Test script phải dựa vào ARIA (`role="dialog"`, `role="switch"`, `role="alert"`, `role="status"`, `aria-label`), `label`/`placeholder`, hoặc text hiển thị.
  2. **Hai hệ UI song song**: Tailwind/HTML thuần + Element Plus (`el-date-picker`, `el-select`, `el-table`, `el-dialog`, `el-pagination`), đòi hỏi chiến lược selector riêng cho popups của Element Plus (teleport ra `body`).
  3. **Trộn lẫn cơ chế thông báo**: Có màn hình dùng banner inline (`role="alert"` / `role="status"`), có nơi dùng `ElMessage`/`ElMessageBox`, và một số màn hình quan trọng vẫn gọi **`window.alert()` / `window.confirm()`** (sẽ treo Playwright/Cypress nếu không đăng ký listener xử lý dialog trình duyệt).

---

## 4. Tóm tắt nhanh các phát hiện trọng yếu

- **Tỷ lệ tích hợp API thật cao**:
  - **16/16** màn hình Admin và **10** component training/schedule dùng API thật.
  - **13/15** màn hình Teacher dùng API thật hoàn toàn; **1 màn hình hoàn toàn Mock** (`TeacherAttendanceReportView.vue`); **1 màn hình Hybrid** (`TeacherNoticesView.vue`: tab `inbox` dùng API thật, tab `sent`/`center` & modal soạn dùng mock cục bộ); **1 hành động Stub** (`exportSelectedQuestions()` trong `TeacherQuestionsView.vue`).
- **Một số lỗi code hiện hữu cần lưu ý khi test**:
  1. `LoginView.vue`: Google OAuth điều hướng `admin` sang `{ name: "admin-payroll" }` thay vì `{ name: "dashboard" }` như đăng nhập thường; chưa có luồng "Quên mật khẩu".
  2. `TeacherAssignmentsView.vue` (Modal Sửa bài tập): Form tham chiếu `editForm.maxAttempt` nhưng object khởi tạo lại thiếu `maxAttempt`, dẫn đến validate fail nếu người dùng không tự nhập lại ô số lần làm tối đa, và khi lưu lại gửi cứng `maxAttempt: 1`.
  3. `TeacherPasswordView.vue`: `changePassword()` gán `passwordMessage` rồi gọi ngay `resetForm()`, xóa trắng thông báo thành công trước khi người dùng kịp nhìn thấy.
  4. `ImportQuestionsModal.vue` (Bước 3): Dùng index của danh sách đã lọc `filteredStagingQuestions` để thao tác trên mảng gốc `stagingQuestions`, gây lệch phần tử khi đang bật bộ lọc loại câu hỏi.
  5. `GoogleCalendarView.vue`: `miniCalendarDays` cố định 35 ô (cắt mất ngày 30/31 ở các tháng 6 tuần), thẻ ngăn xếp (`cluster`) không cập nhật lại `maxEnd` khi gộp nhiều lịch trùng giờ.
  6. `ParentStudentView.vue`, `TeacherManagementView.vue`, và modal inline ở `ClassDetailView.vue` thiếu `role="dialog"`.
  7. Mã chết / chưa nối router: `PlaceholderView.vue`, `views/account/ProfileView.vue`, `views/account/PasswordView.vue`, `services/student.service.js`, `services/yeucaumuonsach.service.js`.
