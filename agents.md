   # Quy Định Phối Hợp Và Làm Việc Của Agent (AGENTS.md)

   Tài liệu này quy định vai trò, phạm vi thao tác, quy trình Git và các nguyên tắc nghiêm ngặt dành cho AI Agents và lập trình viên trong dự án **Hệ thống Toán Tư Duy**.

   ## 1. Nguyên Tắc Cốt Lõi (Core Guardrails)

   * **Đúng Phạm Vi (Strict Scope Limitation):**

   * Phạm vi làm việc hiện tại **CHỈ NẰM TRONG**: `Backend/` (API, Database, Server logic) và `Mobile_App/` (Expo/React Native).

   * **TUYỆT ĐỐI KHÔNG** tự ý sửa đổi, thêm, xóa hoặc refactor bất kỳ file/thư mục nào thuộc `Web_Frontend/` hoặc các phần ngoài phạm vi được phân công.

   * **Contract-First API Design:** Mọi kết nối giữa Mobile và Backend phải tuân theo API Specification (Swagger/OpenAPI hoặc file Type định nghĩa chung). Không đoán field name hay tự chế endpoint giả trong mã nguồn chính.

   * **Không Hard-code:** Tất cả URL Backend, Secret Keys, API Credentials phải nằm trong file cấu hình `.env`.

   * **Kiểm Tra Chấtvi Lượng Bắt Buộc:** Tất cả mã nguồn trước khi commit/push phải chạy thành công các lệnh kiểm tra static type và linter.

   ## 2. Phân Chia Thư Mục & Quyền Hạn (Directory Access Rules)

   | Thư Mục / Phạm Vi | Trạng Thái Thao Tác | Ghi Chú | 
   | ----- | ----- | ----- | 
   | `Backend/` | **ĐƯỢC PHÉP** | Xây dựng API, kết nối DB, Server logic, API Specs. | 
   | `Mobile_App/` | **ĐƯỢC PHÉP** | Phát triển ứng dụng Expo/React Native, tích hợp API Backend. | 
   | `Web_Frontend/` | **CẤM CAN THIỆP** | Do bộ phận/Agent khác đảm nhiệm. Tuyệt đối không chỉnh sửa. | 
   | Khác (`.github/`, root `package.json`, ...) | **XIN PHÉP TRƯỚC** | Chỉ chỉnh sửa khi có yêu cầu rõ ràng từ Lead/User. | 

   ## 3. Quy Định Sử Dụng Git (Git Workflow & Standards)

   ### 3.1 Quy Tắc Đặt Tên Nhánh (Branch Naming)

   Mọi công việc phải được thực hiện trên nhánh phụ. Không push trực tiếp lên `main` hoặc `master`.

   Cú pháp: `<phạm-vi>/<loại-thay-đổi>/<tên-ngắn-gọn-mô-tả>`

   * **Tên phạm vi (`<phạm-vi>`):** `backend` hoặc `mobile`

   * **Loại thay đổi (`<loại-thay-đổi>`):**

   * `feat`: Tính năng mới (Feature)

   * `fix`: Sửa lỗi (Bug fix)

   * `refactor`: Tối ưu / cấu trúc lại code mà không thay đổi logic

   * `docs`: Cập nhật tài liệu

   *Ví dụ:*

   * `backend/feat/auth-login-jwt`

   * `mobile/fix/login-screen-validation`

   * `backend/refactor/db-connection-pool`

   ### 3.2 Quy Chuẩn Commit Message (Conventional Commits)

   Thông điệp commit phải ngắn gọn, rõ ràng và tuân theo cấu trúc:

   `<type>(<scope>): <mô tả ngắn bằng tiếng Việt hoặc tiếng Anh>`

   * **`<type>`:** `feat`, `fix`, `refactor`, `docs`, `style`, `test`, `chore`.

   * **`<scope>`:** `backend` hoặc `mobile` (hoặc tên module chi tiết hơn như `mobile/auth`, `backend/user`).

   *Ví dụ:*

   * `feat(backend): thêm API đăng ký người dùng mới`

   * `fix(mobile): sửa lỗi crash khi chưa cấp quyền vị trí`

   * `refactor(backend): tối ưu middleware xác thực JWT`

   * `docs(mobile): cập nhật hướng dẫn cài đặt môi trường dev`

   ### 3.3 Quy Trình Pull Request (PR) & Review

   1. **Trước khi tạo PR / Commit:**
      Chạy các lệnh kiểm tra tương ứng tại máy local:

      * **Đối với Mobile App:**

      ```
      cd Mobile_App
      npm run lint
      npx tsc --noEmit
      
      ```

      * **Đối với Backend:**
      Chạy linter/test tương ứng của module Backend (ví dụ: `npm run lint` hoặc `npm test`).

   2. **Yêu Cầu Tạo PR:**

      * Tiêu đề PR phải ghi rõ phạm vi: `[Backend] ...` hoặc `[Mobile] ...`.

      * Nội dung PR phải liệt kê: các thay đổi chính, API mới (nếu có), và hình ảnh/video demo (nếu là UI Mobile).

      * Kiểm tra danh sách file đã thay đổi (Changed Files): **Đảm bảo không có bất kỳ file nào thuộc `Web_Frontend/` bị ảnh hưởng.**

   3. **Gộp nhánh (Merge):**

      * Phải vượt qua CI/CD check (nếu có).

      * Chỉ merge sau khi thành viên/Lead review và chấp thuận.

   ## 4. Checklist Cho Agent Trước Khi Hoàn Thành Task

   * \[ \] Task này nằm hoàn toàn trong `Backend/` hoặc `Mobile_App/`?

   * \[ \] Không có file nào ngoài phạm vi trên bị chỉnh sửa/xóa?

   * \[ \] Nhánh Git và Commit Message đã đúng định dạng chuẩn chưa?

   * \[ \] Đã test local và vượt qua `npm run lint` & `npx tsc --noEmit` chưa?

   * \[ \] Mọi cấu hình nhạy cảm/URL đã được chuyển vào file `.env` chưa?