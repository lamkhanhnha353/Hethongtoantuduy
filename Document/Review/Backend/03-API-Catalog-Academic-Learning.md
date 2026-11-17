# 03. Danh mục API — Khóa học, Lớp học, Lịch học, Điểm danh, Nghỉ phép & Bài tập

---

## 1. Khóa học (`CoursesController` — `/api/courses`)

Nguồn: [`Controllers/CoursesController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/CoursesController.cs), [`Services/CourseService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/CourseService.cs)

Class có `[Authorize]`.

| Method | Endpoint | Roles | Input | Response & Rules |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/courses` | Any auth | — | `200 OK`: `List<CourseResponse>` (kèm `courseLevels`) |
| `GET` | `/api/courses/{id}` | Any auth | `id` | `200 OK` / `404` |
| `POST` | `/api/courses/media` | `Admin` | `multipart/form-data`: `file`, `type` (`"image"` \| `"video"`)<br>Limit: `105MB` (image max 10MB JPG/PNG/WEBP, video max 100MB MP4/MOV/WEBM/M4V) | `200 OK`: `{ url, type, fileName }` (Cloudinary)<br>`400` / `502` / `503` |
| `POST` | `/api/courses` | `Admin` | `CourseRequest` (`code, name, description?, minAge, maxAge, imageUrl?, status, videoUrl?, benefits?, facilityImages?`) | `201 Created` / `400 { message }` (trùng `code`) |
| `PUT` | `/api/courses/{id}` | `Admin` | `CourseRequest` | `200 OK` / `404` |
| `DELETE` | `/api/courses/{id}` | `Admin` | `id` | `200 OK` / `404` / `400 { message }` (nếu còn `CourseLevels`) |

---

## 2. Cấp độ khóa học (`CourseLevelsController` — `/api/courselevels`)

Nguồn: [`Controllers/CourseLevelsController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/CourseLevelsController.cs), [`Services/CourseLevelService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/CourseLevelService.cs)

Class có `[Authorize]`.

| Method | Endpoint | Roles | Input | Response & Rules |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/courselevels/course/{courseId}` | Any auth | `courseId` | `200 OK`: `List<CourseLevelResponse>` (gồm cả metadata từ `Course` cha + JSON `syllabus`, `faqs`) |
| `GET` | `/api/courselevels/{id}` | Any auth | `id` | `200 OK` / `404` |
| `POST` | `/api/courselevels` | `Admin` | `CourseLevelRequest` (`courseId, code, name, fee, totalSessions, orderIndex, status, syllabus?, faqs?`) | `201 Created` / `400 { message }` (nếu `courseId` không tồn tại) |
| `PUT` | `/api/courselevels/{id}` | `Admin` | `CourseLevelRequest` | `200 OK` / `404` |
| `DELETE` | `/api/courselevels/{id}` | `Admin` | `id` | `200 OK` / `404` / `400 { message }` (nếu còn `Classes`) |

---

## 3. Lớp học (`ClassesController` — `/api/classes`)

Nguồn: [`Controllers/ClassesController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/ClassesController.cs), [`Services/ClassService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/ClassService.cs)

> [!WARNING]
> `ClassesController` **không có `[Authorize]`**, toàn bộ 11 endpoints hiện đang cho phép truy cập ẩn danh!

| Method | Endpoint | Query / Body | Response & Side Effects |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/classes` | — | `200 OK`: `List<ClassResponse>` |
| `GET` | `/api/classes/paged` | `page=1, pageSize=12, keyword?, courseId?, courseLevelId?, teacherId?, status?, capacity? ("available"\|"full"\|"low"), sort? ("name_desc"\|"students_desc"\|"date_asc"\|"newest")` | `200 OK`: `PagedResult<ClassResponse>` (lưu ý `capacity="full"` lọc `>= 90%` sĩ số) |
| `GET` | `/api/classes/course-level/{courseLevelId}` | `courseLevelId` | `200 OK`: `List<ClassResponse>` |
| `GET` | `/api/classes/{id}` | `id` | `200 OK` / `404` |
| `POST` | `/api/classes` | `ClassRequest` (`courseLevelId, teacherId?, name, maxCapacity, startDate, endDate?, status`) | `201 Created` / `400 { message }`. Nếu có `teacherId`, tạo notification + email phân bổ lớp cho giáo viên |
| `PUT` | `/api/classes/{id}` | `ClassRequest` | `200 OK` / `404`. Nếu đổi `teacherId` sang giáo viên mới, gửi notification + email |
| `DELETE` | `/api/classes/{id}` | `id` | `200 OK` / `404` / `400 { message }` (chặn nếu lớp đã có `ClassStudents`) |
| `GET` | `/api/classes/{id}/students` | `id` | `200 OK`: `List<ClassStudentResponse>` |
| `GET` | `/api/classes/{id}/students/paged` | `page=1, pageSize=20, keyword?` | `200 OK`: `PagedResult<ClassStudentResponse>` |
| `POST` | `/api/classes/{id}/students` | `AddClassStudentRequest` (`studentId`) | `200 OK`: `ClassStudentResponse` / `400 { message }` (trùng `ACTIVE`/`PENDING`, đầy sĩ số `MaxCapacity`, không thấy lớp/học sinh). Gửi notification + email cho phụ huynh & giáo viên |
| `DELETE` | `/api/classes/{id}/students/{studentId}` | `id, studentId` | `200 OK` / `404` |

---

## 4. Lịch học cố định & Sinh buổi học (`ClassSchedulesController` — `/api/classschedules`)

Nguồn: [`Controllers/ClassSchedulesController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/ClassSchedulesController.cs), [`Services/ClassScheduleService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/ClassScheduleService.cs)

| Method | Endpoint | Auth / Policy | Input | Response & Quy tắc nghiệp vụ |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/classschedules/class/{classId}` | Public | `classId` | `200 OK`: `List<ClassScheduleResponse>` |
| `GET` | `/api/classschedules/{id}` | Public | `id` | `200 OK` / `404` |
| `POST` | `/api/classschedules` | `AdminOrDevelopmentTesting` | `ClassScheduleRequest`: `classId`, `dayOfWeek` (`0..6`), `startTime` (`TimeSpan`), `endTime` (`TimeSpan`), `room?` | `201 Created` / `400` / `404` / `409` (trùng ca cùng lớp, trùng phòng, hoặc trùng lịch giáo viên) |
| `PUT` | `/api/classschedules/{id}` | `AdminOrDevelopmentTesting` | `ClassScheduleRequest` | `200 OK` / `404` / `409` |
| `DELETE` | `/api/classschedules/{id}` | `AdminOrDevelopmentTesting` | `id` | `200 OK` / `404` / `409` |

### Thuật toán tự động sinh `Lesson` & tính `Classes.EndDate`
Khi tạo/sửa/xóa `ClassSchedule` (trong transaction):
1. Đếm `fixedLessons`: các buổi trong quá khứ (`LessonDate.Date < UtcNow.Date`) hoặc đã khác trạng thái `SCHEDULED`/`CANCELED`/`CANCELLED`.
2. Tính `remainingSessions = Max(0, CourseLevel.TotalSessions - fixedLessons.Count)`.
3. Từ `Max(Class.StartDate.Date, UtcNow.Date)`, rải tuần tự từng ngày khớp `dayOfWeek` của các `ClassSchedule` cho đủ `remainingSessions`, lấy `title` từ JSON `CourseLevel.Syllabus[sessionIndex].title` (fallback `"Buổi học {n}"`).
4. Cập nhật lại các buổi `SCHEDULED` tương lai hiện có; nếu số buổi giảm làm buổi thừa chuyển sang `"CANCELED"`, hoặc đổi ngày/giờ/title của buổi **đã có `LeaveRequest` (chưa `CANCELLED`)** → ném **`409 Conflict`** và rollback!
5. Cập nhật `Classes.EndDate = Max(activeLessons.LessonDate)`.

---

## 5. Buổi học & Lộ trình học tập (`LessonsController` — `/api/lessons`)

Nguồn: [`Controllers/LessonsController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/LessonsController.cs), [`Services/LessonService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/LessonService.cs)

| Method | Endpoint | Auth | Query / Body | Response & Rules |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/lessons` | **Public (thiếu `[Authorize]`)** | — | `200 OK`: `List<LessonResponse>` |
| `GET` | `/api/lessons/paged` | **Public** | `page=1, pageSize=20, classId?, status?, keyword?` | `200 OK`: `PagedResult<LessonResponse>` |
| `GET` | `/api/lessons/class/{classId}` | **Public** | `classId` | `200 OK`: `List<LessonResponse>` (sắp xếp tăng dần theo ngày/giờ) |
| `GET` | `/api/lessons/{id}` | **Public** | `id` | `200 OK` / `404` |
| `POST` | `/api/lessons` | **Public** | `LessonRequest` (`classId, lessonDate, startTime?, endTime?, status="SCHEDULED", isImportan=false`) | `201 Created` / `400 { message }`. Nếu `isImportan == true`, phát notification `SPECIAL` + email cho GV & PH lớp |
| `PUT` | `/api/lessons/{id}` | **Public** | `LessonRequest` | `200 OK` / `404` / `400 { message }`. Nếu chuyển `isImportan` từ `false` → `true`, phát notification `SPECIAL` |
| `DELETE` | `/api/lessons/{id}` | **Public** | `id` | `200 OK` / `404` / `400 { message }` (chặn nếu có `Attendances` hoặc `LeaveRequests`) |
| `GET` | `/api/lessons/student/{studentId}` | `Parent` | `studentId` (kiểm tra ownership) | `200 OK`: `List<StudentLessonResponse>` (các lớp `ACTIVE`) / `404` |
| `GET` | `/api/lessons/student/{studentId}/roadmap` | `Parent` | `studentId` (kiểm tra ownership) | `200 OK`: `List<StudentRoadmapResponse>` (buổi `SCHEDULED` đầu tiên là `IN_PROGRESS`, các buổi sau là `LOCKED`) / `404` |

---

## 6. Điểm danh (`AttendancesController` — `/api/attendances` hoặc `/api/attendance`)

Nguồn: [`Controllers/AttendancesController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/AttendancesController.cs), [`Services/AttendanceService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/AttendanceService.cs)

Class có `[Authorize]` (chưa giới hạn Role `Teacher,Admin`). Hỗ trợ 2 route prefix song song: `api/attendances` và `api/attendance`.

| Method | Endpoint | Input | Response & Side Effects |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/attendances/lesson/{lessonId}` | `lessonId` | `200 OK`: `LessonAttendanceDetailResponse` (danh sách học viên `ACTIVE` của lớp, trạng thái điểm danh hoặc `"UNMARKED"`, kèm thông tin đơn xin nghỉ)<br>`404` |
| `POST` | `/api/attendances/lesson/{lessonId}` | Body: `List<AttendanceItemDto>` (`studentId`, `status` `PRESENT\|ABSENT\|LATE\|EXCUSED`, `teacherNotes?`) | `200 OK`: `{ message, data: LessonAttendanceDetailResponse }`<br>`400` / `404` / `500` |
| `POST` | `/api/attendances/batch` | `BatchAttendanceRequest` (`lessonId`, `attendances[]`) | Như trên |
| `PUT` | `/api/attendances/lesson/{lessonId}/student/{studentId}` | `AttendanceItemDto` (`status`, `teacherNotes?`) | `200 OK`: `{ message, data: StudentAttendanceItemResponse }` / `400` / `404` |
| `GET` | `/api/attendances/student/{studentId}` | Query: `classId?` | `200 OK`: `List<StudentAttendanceHistoryResponse>` |

### Side effects khi lưu điểm danh
1. Học sinh bị đánh dấu `ABSENT` hoặc `LATE` → Enqueue notification `ATTENDANCE` gửi phụ huynh (`RefId = LESSON:{lessonId}:STUDENT:{studentId}:{status}`).
2. Nếu tất cả học sinh `ACTIVE` trong lớp đã được điểm danh (khác `UNMARKED`) → Publish notification `ATTENDANCE` cho Giáo viên phụ trách + AllAdmins (`RefId = LESSON:{lessonId}:ATTENDANCE_COMPLETED`).

---

## 7. Đơn xin nghỉ phép (`LeaveRequestsController` — `/api/leaverequests`)

Nguồn: [`Controllers/LeaveRequestsController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/LeaveRequestsController.cs), [`Services/LeaveRequestService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/LeaveRequestService.cs)

| Method | Endpoint | Roles | Input | Response & Business Rules |
| :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/leaverequests` | `Parent` | `LeaveRequestCreateRequest` (`studentId, lessonId, reason?`) | `200 OK`: `LeaveRequestResponse` (status `PENDING`).<br>Kiểm tra: (1) Student thuộc Parent (`404`); (2) Student `ACTIVE` trong lớp (`400`); (3) Lesson `SCHEDULED` (`400`); (4) Lesson có `StartTime` và còn cách giờ học **>= 2 tiếng** theo giờ VN `UtcNow + 7h` (`400`); (5) Chưa có đơn chưa hủy cho buổi đó (`409`). Gửi notification `LEAVE_REQUEST` cho GV lớp |
| `DELETE` | `/api/leaverequests/{id}` | `Parent` | `id` | `204 No Content` (chuyển `CANCELLED`, gửi notification cho GV)<br>`404` / `409` (nếu không phải `PENDING`) |
| `GET` | `/api/leaverequests/pending` | `Teacher` | — | `200 OK`: `List<LeaveRequestResponse>` (chỉ các đơn `PENDING` thuộc lớp GV phụ trách) |
| `GET` | `/api/leaverequests/pending/paged` | `Teacher` | `page=1, pageSize=20` | `200 OK`: `PagedResult<LeaveRequestResponse>` |
| `PUT` | `/api/leaverequests/{id}/process?isApproved=true` | `Teacher` | Query `isApproved` (`bool`) | `200 OK`: `{ message }` / `404` (nếu không tìm thấy, không thuộc lớp GV, hoặc không còn `PENDING`). Nếu `isApproved=true`, **tự động tạo/cập nhật `Attendance` thành `EXCUSED`**! Gửi notification `ATTENDANCE` cho phụ huynh |
| `GET` | `/api/leaverequests/student/{studentId}` | `Parent` | `studentId` | `200 OK`: `List<LeaveRequestResponse>` / `404` |

---

## 8. Ngân hàng câu hỏi (`QuestionBanksController` — `/api/question-banks`)

Nguồn: [`Controllers/QuestionBanksController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/QuestionBanksController.cs), [`Services/QuestionBankService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/QuestionBankService.cs)

Class có `[Authorize]`; các thao tác ghi yêu cầu `Roles = "Teacher,Admin"`.

| Method | Endpoint | Roles | Input | Response & Rules |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/question-banks` | Any auth | Query `courseId?, keyword?` | `200 OK`: `List<QuestionBankResponse>` |
| `GET` | `/api/question-banks/paged` | Any auth | `page=1, pageSize=20, courseId?, keyword?` | `200 OK`: `PagedResult<QuestionBankResponse>` |
| `GET` | `/api/question-banks/{id}` | Any auth | `id` | `200 OK`: `QuestionBankDetailResponse` / `404` |
| `POST` | `/api/question-banks` | `Teacher,Admin` | `QuestionBankRequest` (`courseId, name, description?`) | `201 Created` / `404` (Course không tồn tại) |
| `PUT` | `/api/question-banks/{id}` | `Teacher,Admin` | `QuestionBankRequest` | `200 OK` / `404` |
| `DELETE` | `/api/question-banks/{id}` | `Teacher,Admin` | `id` | `200 OK` / `404` (gỡ `QuestionAssignments` và xóa hết `Questions` bên trong) |
| `GET` | `/api/question-banks/{bankId}/questions` | Any auth | Query `difficulty?, type?` | `200 OK`: `List<QuestionResponse>` / `404` (Bank không tồn tại) |
| `GET` | `/api/question-banks/{bankId}/questions/paged` | Any auth | `page=1, pageSize=20, difficulty?, type?, keyword?` | `200 OK`: `PagedResult<QuestionResponse>` / `404` |
| `GET` | `/api/question-banks/questions/{id}` | Any auth | `id` | `200 OK` / `404` |
| `POST` | `/api/question-banks/questions` | `Teacher,Admin` | `QuestionRequest` (`bankId, type, content, imageUrl?, difficulty, answer`) | `201 Created` / `404` |
| `POST` | `/api/question-banks/questions/bulk` | `Teacher,Admin` | `BulkQuestionCreateRequest` (`bankId, questions[]`) | `200 OK`: `List<QuestionResponse>` / `404` |
| `PUT` | `/api/question-banks/questions/{id}` | `Teacher,Admin` | `QuestionRequest` | `200 OK` / `404` |
| `DELETE` | `/api/question-banks/questions/{id}` | `Teacher,Admin` | `id` | `200 OK` / `404` |
| `POST` | `/api/question-banks/questions/bulk-delete` | `Teacher,Admin` | `BulkQuestionDeleteRequest` (`ids[]`) | `200 OK`: `BulkQuestionDeleteResponse` / `400` / `404` |

---

## 9. Bài tập & Luyện tập (`AssignmentsController` — `/api/assignments`)

Nguồn: [`Controllers/AssignmentsController.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Controllers/AssignmentsController.cs), [`Services/AssignmentService.cs`](file:///d:/ASP.NET/Hethongtoantuduy/Backend/ToanTuDuy/Services/AssignmentService.cs)

Class có `[Authorize]`.

| Method | Endpoint | Roles | Input | Response & Rules |
| :--- | :--- | :--- | :--- | :--- |
| `GET` | `/api/assignments/lesson/{lessonId}` | Any auth | `lessonId` | `200 OK`: `List<AssignmentResponse>` / `404` |
| `GET` | `/api/assignments/{id}` | Any auth | `id` | `200 OK`: `AssignmentDetailResponse` (chứa đáp án `Answer`!) / `404` |
| `POST` | `/api/assignments` | `Teacher,Admin` | `CreateAssignmentRequest` (`lessonId, title, startTime, deadline, timeLimitMinutes, maxAttempt=1, status="OPEN", questions[], assignAllStudentsInClass, specificStudentIds?`) | `201 Created`: `AssignmentDetailResponse`. Validate: phải có >= 1 câu hỏi, `maxAttempt == 1`, `deadline > startTime`, question type chỉ nhận `MULTIPLE_CHOICE \| TRUE_FALSE \| FILL_IN \| TIMED_SEQUENCE` |
| `POST` | `/api/assignments/clone` | `Teacher,Admin` | `CloneAssignmentRequest` (`sourceAssignmentId, targetLessonId, newTitle?, startTime?, deadline?, assignAllStudentsInClass, specificStudentIds?`) | `201 Created`: `AssignmentDetailResponse` / `404` / `400` |
| `PUT` | `/api/assignments/{id}` | `Teacher,Admin` | `UpdateAssignmentRequest` | `200 OK` / `404` / `400` |
| `DELETE` | `/api/assignments/{id}` | `Teacher,Admin` | `id` | `200 OK` / `404` |
| `POST` | `/api/assignments/{id}/students` | `Teacher,Admin` | `AssignStudentsRequest` (`studentIds[]`) | `200 OK`: `{ message, data }` / `404` / `400` (HS không thuộc lớp) |
| `DELETE` | `/api/assignments/{id}/students/{studentId}` | `Teacher,Admin` | `id, studentId` | `200 OK` / `404` / `400` (nếu đã `SUBMITTED`/`GRADED`) |
| `GET` | `/api/assignments/student/{studentId}` | `Teacher,Admin,Parent` | Query `lessonId?` | `200 OK`: `List<AssignmentResponse>` |
| `GET` | `/api/assignments/student/{studentId}/practice` | `Parent` | `studentId` (check ownership) | `200 OK`: `List<StudentPracticeAssignmentDto>` (`isAvailable` true khi `OPEN`, trong thời gian, chưa nộp) |
| `GET` | `/api/assignments/{id}/student/{studentId}/practice` | `Parent` | `id, studentId` (check ownership) | `200 OK`: `StudentPracticeAssignmentDetailDto` (không lộ đáp án; tách `options` A–D hoặc `sequenceTerms`) / `400` / `404` |
| `POST` | `/api/assignments/{id}/student/{studentId}/practice/submit` | `Parent` | `SubmitStudentPracticeRequest` (`answers[]`: `{ questionAssignmentId, answer }`) | `200 OK`: `StudentPracticeSubmissionResponseDto` (tự chấm điểm, atomic update chống nộp trùng, gửi notification cho GV) / `400` / `404` |
| `PUT` | `/api/assignments/{id}/grade/{studentId}` | `Teacher,Admin` | `GradeStudentRequest` (`score?, teacherFeedback?, status="GRADED"`) | `200 OK`: `{ message, data }` / `404` (gửi notification `ACADEMIC` cho PH) |
| `PUT` | `/api/assignments/{id}/batch-grade` | `Teacher,Admin` | `BatchGradeRequest` (`grades[]`) | `200 OK` / `400` |
| `GET` | `/api/assignments/{id}/submission/{studentId}` | `Teacher,Admin,Parent` | `id, studentId` | `200 OK`: `StudentSubmissionDetailResponse` / `404` |

### Quy tắc parse & tự chấm điểm (`AssignmentService`)
- `MULTIPLE_CHOICE`: nội dung `Question.Content` phải parse được đủ 4 phương án `A.`, `B.`, `C.`, `D.` (regex `(?<key>[A-D])[\.:\)]\s*`). Chấm theo chữ cái đầu của `Question.Answer` (`^[A-D]`).
- `TRUE_FALSE`: so sánh không phân biệt hoa thường với chữ đầu `Đúng` hoặc `Sai` trong `Question.Answer`.
- `FILL_IN` & `TIMED_SEQUENCE`: lấy dòng đầu tiên của `Question.Answer`, chuẩn hóa Unicode FormKC + gộp khoảng trắng, so sánh không phân biệt hoa thường. Riêng `TIMED_SEQUENCE` bắt buộc `Content` theo mẫu `"Tính nhẩm theo nhịp: <số>, +<số>, -<số>"` (>= 2 số hạng).
- Bắt buộc khi submit `answers` phải có **đúng 1 câu trả lời cho mỗi câu hỏi** trong bài (`answersByQuestionId.Count == questions.Count`).
