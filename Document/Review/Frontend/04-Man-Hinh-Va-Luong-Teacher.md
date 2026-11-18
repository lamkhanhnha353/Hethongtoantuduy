# 04. Review Màn hình & Luồng Nghiệp vụ Teacher (15 Views + `ImportQuestionsModal`)

> Đa số màn hình Teacher (**13/15**) gọi **API thật**. Tuy nhiên QA **bắt buộc phải tách riêng 2 màn hình còn Mock/Hybrid** (`TeacherAttendanceReportView` hoàn toàn mock; `TeacherNoticesView` tab `sent`/`center` & Compose là mock) và xử lý các hộp thoại `window.alert`/`window.confirm` cùng các bug cụ thể dưới đây.

---

## 1. Tổng quan & Phân tách Trạng thái API của 15 Views

| Màn hình | Route | Trạng thái API | Cơ chế Alert / Dialog | Vấn đề / Rủi ro Automation |
| :--- | :--- | :--- | :--- | :--- |
| `TeacherDashboardView` | `/teacher` | **100% Real** | Banner inline + `NotificationDetailModal` | **Fan-out rất nặng khi mount** (nạp tất cả lớp -> buổi học/lịch từng lớp -> tối đa 12 API điểm danh + 24 API bài tập theo buổi + 12 chi tiết bài tập). Dễ timeout nếu dữ liệu lớn. |
| `TeacherScheduleView` | `/teacher/schedule` | **100% Real** | Không có banner lỗi (chỉ `console.error`) | Mặc định tuần hiện tại (`weekOffset = 0`); test buổi cũ/tương lai phải bấm điều hướng tuần. |
| `TeacherClassesView` | `/teacher/classes` | **100% Real** | Không có banner lỗi | Phân trang client `9`/trang, sau đó mới gọi thêm API lịch & buổi học theo trang. Dùng `getAssignedClasses` (fallback trả tất cả lớp nếu GV chưa được gán lớp nào). |
| `TeacherStudentsView` | `/teacher/students` | **100% Real** | Không có banner lỗi | Phân trang server (`20`/trang), tìm kiếm debounce `300ms`, modal lịch sử điểm danh, nút **Xuất CSV** (browser download). |
| `TeacherAttendanceView` | `/teacher/attendance` | **100% Real** | Banner inline + **Native `confirm`** (khi còn HS `UNMARKED`) | **Khóa chỉnh sửa sau 24h** kể từ giờ kết thúc buổi học (`lockState.isLocked`) -> bắt buộc dùng data test là buổi học hôm nay hoặc trong vòng 24h qua. |
| `TeacherQuestionsView` | `/teacher/questions` | **100% Real** *(trừ Export)* | Banner inline + Modal xác nhận riêng | Debounce `300ms` ở cả bank & câu hỏi; **`exportSelectedQuestions()` là Stub** (chỉ hiện banner chứ không tải file); loại `TIMED_SEQUENCE` có trong dropdown nhưng **thiếu ô nhập đáp án** trong modal. |
| `ImportQuestionsModal` | *(Modal trong Questions)* | **Real AI Worker + Backend** | Emit `'notify'` + box lỗi inline | Phụ thuộc Worker FastAPI (`:8000` hoặc proxy `/api/parser/*`) + polling `2500ms`. **Bug Bước 3**: thao tác tick/xóa khi đang bật bộ lọc loại câu hỏi sẽ lệch index sang `stagingQuestions`. |
| `TeacherAssignmentsView` | `/teacher/assignments` | **100% Real** | Banner inline + **Native `confirm`** (xóa / gỡ HS) | **Bug Modal Sửa**: `editForm` thiếu khai báo `maxAttempt` lúc mở modal nên submit sửa không nhập lại sẽ trượt validation, và payload PUT lại gửi cứng `maxAttempt: 1`. Câu `ESSAY` bị disable khi chọn đề. |
| `TeacherGradingView` | `/teacher/grading` | **100% Real** | Banner inline | Fetch bài tập tuần tự theo từng buổi (`for loop`) khi đổi lớp. Input điểm `@input` chuẩn hóa `normalizeScore` ngay khi gõ -> cẩn thận khi gõ số thập phân / xóa rỗng trong Playwright. |
| `TeacherSubmissionDetailView` | `/teacher/grading/submission/:assignmentId/:studentId` | **100% Real** | Banner inline | **Bắt buộc nhập `teacherFeedback`** (`required: true`, max 1000) mới cho lưu điểm. Checkbox "Đạt câu này" lúc mới mở chỉ ước lượng theo tổng điểm chứ không đọc từng câu trả lời của HS. |
| `TeacherAttendanceReportView` | `/teacher/reports/attendance` | **100% MOCK** | **Native `alert`** | Toàn bộ KPI (`94.8%`, `12 Buổi`...), 3 lớp dropdown, biểu đồ và danh sách HS đều **hardcode tĩnh**; đổi bộ lọc không đổi số liệu. |
| `TeacherNoticesView` | `/teacher/communication/notices` | **HYBRID** | **Native `alert`** (ở form soạn mock) | **Tab `Thông báo của tôi` (`?tab=inbox`) gọi API thật** (`NotificationInboxPanel`); **2 tab `Lịch sử đã gửi` (`sent`), `Thông báo trung tâm` (`center`) & modal `Soạn tin nhắn mới` hoàn toàn MOCK cục bộ** (kể cả chọn tab `Thông báo trung tâm` vẫn render cùng mảng `sentList`). |
| `TeacherChatView` | `/teacher/communication/chat` | **100% Real + SignalR** | Banner inline + Modal xóa tin nhắn | Kết nối `/chathub`, upload tệp tối đa `20MB`, phân trang `30` tin/trang. Trên màn hình `< 1024px`, chọn hội thoại sẽ ẩn danh sách bên trái. |
| `TeacherPayslipView` | `/teacher/payslip` | **100% Real** | **`ElMessage.error`** (Element Plus) | Dùng **Element Plus** (`el-date-picker` tháng, `el-table`). Đòi hỏi `authService.getProfileId()` khác null (không có fallback lookup). |
| `TeacherProfileView` | `/teacher/account/profile` | **100% Real** | Banner inline (`role="status"` / `role="alert"`) | Quy trình Sửa -> Nhập -> Lưu tách riêng theo từng card; trường SĐT bị disabled. Lưu Email gọi `teacherService.update` (không gọi `profileService.update`). |
| `TeacherPasswordView` | `/teacher/account/password` | **100% Real** | Banner inline (`role="status"` / `role="alert"`) | **Bug**: `changePassword()` set `passwordMessage` xong gọi ngay `resetForm()` ở dòng kế tiếp làm xóa sạch `passwordMessage` -> **banner báo thành công không bao giờ hiển thị**. |

---

## 2. Chi tiết luồng nghiệp vụ trọng tâm phía Teacher

### 2.1 Điểm danh & Chuyên cần (`TeacherAttendanceView`)
1. Chọn lớp & buổi học (hoặc mở trực tiếp qua `/teacher/attendance?classId=...&lessonId=...`).
2. Hỗ trợ các bộ lọc nhanh buổi học (`ALL`, `IN_PROGRESS`, `ENDED`, `COMPLETED`, `UNMARKED`) và lọc học sinh (`searchQuery`, `statusFilter`, `HAS_LEAVE`).
3. Điểm danh từng học sinh (`PUT /api/attendance/lesson/{lessonId}/student/{studentId}`) hoặc lưu hàng loạt (`POST /api/attendance/lesson/{lessonId}` — hiện popup `window.confirm` nếu vẫn còn học sinh chưa chọn trạng thái).
4. Cho phép xem lịch sử điểm danh của học sinh (`showHistoryModal`) và chi tiết đơn xin nghỉ (`showLeaveModal` với nút duyệt nghỉ có phép `EXCUSED`).

### 2.2 Ngân hàng câu hỏi & AI Import (`TeacherQuestionsView`, `ImportQuestionsModal`)
1. Danh sách ngân hàng câu hỏi hỗ trợ tìm kiếm (`300ms` debounce) + lọc Khóa học + phân trang server (`10`/trang).
2. Chọn ngân hàng mở panel câu hỏi bên phải: lọc Độ khó (`EASY`, `MEDIUM`, `HARD`), Loại (`MULTIPLE_CHOICE`, `TRUE_FALSE`, `FILL_IN`, `ESSAY`, `TIMED_SEQUENCE`), tìm kiếm debounce `300ms`, phân trang server (`20`/trang), chọn nhiều để xóa hàng loạt (`bulkDeleteQuestions`).
3. Modal AI Import (3 bước):
   - **Bước 1**: Chọn/tạo ngân hàng đích + chọn tệp `.docx`/`.pdf` (tối đa `50MB`), kiểm tra dung lượng/đuôi tệp và đẩy lên FastAPI worker (`/api/parser/process-file`).
   - **Bước 2**: Polling `/api/parser/job/{jobId}` mỗi `2.5s` theo dõi 4 bước tiến trình; có nút hủy polling (`cancelPolling`).
   - **Bước 3**: Xem trước danh sách câu hỏi worker tách được, chỉnh loại/độ khó, bỏ bớt câu và lưu qua `bulkCreateQuestions` (nếu batch lỗi sẽ fallback tự động gọi `createQuestion` từng câu một).

### 2.3 Giao bài tập & Chấm điểm (`TeacherAssignmentsView`, `TeacherGradingView`, `TeacherSubmissionDetailView`)
1. Chọn Lớp -> chọn Buổi học -> Tạo mới / Sửa / Nhân bản (`clone` sang buổi khác) / Xem câu hỏi / Quản lý học sinh được giao / Xóa bài tập.
2. Ở màn hình Chấm điểm (`TeacherGradingView`): chọn Lớp -> chọn Bài tập -> lọc trạng thái (`ALL`, `SUBMITTED`, `GRADED`, `ASSIGNED`), nhập điểm (`0..100`) và nhận xét trực tiếp rồi lưu từng em (`gradeStudent`) hoặc "Lưu tất cả kết quả" (`batchGrade`), hoặc mở chi tiết bài làm `/teacher/grading/submission/:assignmentId/:studentId` để tick từng câu đạt và chọn nhanh 6 mẫu nhận xét có sẵn.
