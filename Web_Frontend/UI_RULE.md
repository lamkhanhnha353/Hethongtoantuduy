# GLOBAL UI/UX DESIGN RULES — HỆ THỐNG TOÁN TƯ DUY (WEB_FRONTEND)

Bạn là Senior Product Designer & Vue 3 Frontend Architect. Mọi chỉnh sửa giao diện trên toàn bộ dự án phải tuân thủ nghiêm ngặt bộ quy chuẩn dưới đây nhằm tạo ra một hệ thống **vững chãi, sắc nét, "thô" (tactile & sturdy — đúng chất phần mềm quản trị/giáo dục chuyên dụng)** và **hiển thị thông số dễ đọc tức thì**, đồng thời **giữ nguyên 100% logic `<script setup>` và nhận diện màu Cam thương hiệu**.

---

## 1. NGUYÊN TẮC BẤT DI BẤT DỊCH
1. **Bảo toàn nghiệp vụ:** Tuyệt đối không làm thay đổi props, emits, biến reactive, API calls hay luồng xử lý trong `<script setup>`.
2. **Giữ bản sắc Cam – Ấm:** Duy trì tông chủ đạo Cam (`orange-600`) trên nền kem/đá ấm (`stone-50` / `orange-50/40`), loại bỏ hoàn toàn màu xanh dương mặc định của Element Plus.
3. **Thoát khỏi vẻ "mềm mại kiểu template AI":** Giảm độ bo tròn quá mức (`rounded-2xl`, `rounded-3xl`), bỏ các dải gradient sặc sỡ trên header modal/banner, thay bằng đường nét vuông vức, viền rõ ràng và phân cấp tương phản cao.

---

## 2. HÌNH KHỐI & ĐƯỜNG VIỀN (STURDY & CRISP GEOMETRY)
- **Card & Container chính (Bảng dữ liệu, Khối thống kê, Modal):**
  - Bo góc sắc nét: Chỉ dùng `rounded-lg` (8px). Không dùng `rounded-2xl` hay `rounded-3xl`.
  - Viền rõ ràng, chắc chắn: `bg-white border border-stone-300 shadow-[0_1px_2px_rgba(0,0,0,0.04)]`.
- **Phân tách khu vực (Dividers):**
  - Dùng đường kẻ rõ `border-stone-200` hoặc `border-stone-300` giữa Header trang, Thanh công cụ (Toolbar), Bảng dữ liệu và Chân trang (Pagination).

---

## 3. GIAO DIỆN NHẬP LIỆU "THÔ" & RÕ RÀNG (TACTILE INPUTS & FORMS)
Mọi ô nhập liệu (`<input>`, `<select>`, `<textarea>`, `el-input`, `el-select`, `el-date-picker`) phải có cảm giác "đầm tay", dễ nhận diện vùng nhập:
- **Hình dáng:** Bo góc vuông vức `rounded-md` (6px), chiều cao chuẩn `h-9` (36px) cho thanh công cụ và `h-10` (40px) cho form nhập liệu chính.
- **Màu nền & Viền:**
  - Trạng thái nghỉ: Nền ngà nhẹ `bg-stone-50/70` kèm viền rõ `border border-stone-300` (tránh viền nhạt `border-gray-200` gây chìm ô nhập).
  - Trạng thái Focus: Nền chuyển trắng tinh `focus:bg-white`, viền cam đậm `focus:border-orange-600` và vòng sáng gọn `focus:ring-2 focus:ring-orange-500/20 outline-none`.
- **Nhãn (Label):** Luôn đặt phía trên ô nhập, chữ đậm vừa dễ đọc: `block text-xs font-semibold uppercase tracking-wide text-stone-700 mb-1.5`.

---

## 4. HỆ THỐNG NÚT BẤM THỐNG NHẤT (UNIFIED BUTTONS)
Chuẩn hóa toàn bộ nút bấm về bo góc `rounded-md` (6px), chữ `text-sm font-semibold`, chiều cao gọn `h-9 px-3.5`, khoảng cách icon-chữ `gap-1.5`:
1. **Primary CTA (Lưu, Tạo mới, Gửi hàng loạt):**
   - `bg-orange-600 hover:bg-orange-700 active:bg-orange-800 text-white border border-orange-700 shadow-xs`
2. **Secondary / Action trong bảng (Chi tiết, Lọc, Xuất file, Hủy):**
   - Dạng khối viền rõ (không dùng chữ trơn mờ nhạt): `bg-white text-stone-800 border border-stone-300 hover:bg-stone-100 hover:border-stone-400 active:bg-stone-200`
3. **Danger (Xóa, Hủy ca, Khóa tài khoản):**
   - Nút kích hoạt: `bg-white text-rose-700 border border-rose-300 hover:bg-rose-50 hover:border-rose-400`
   - Nút xác nhận trong Modal: `bg-rose-600 hover:bg-rose-700 text-white border border-rose-700`

---

## 5. HIỂN THỊ THÔNG SỐ & BẢNG DỮ LIỆU DỄ ĐỌC (HIGH-LEGIBILITY DATA)
Mọi con số, tiền tệ, thống kê, bảng danh sách phải tối ưu cho việc quét mắt nhanh (scannability):
- **Căn chỉnh số học:**
  - Bắt buộc gắn `tabular-nums` cho mọi cột số, tiền tệ, ngày giờ, tỷ lệ `%`.
  - Căn lề trái (`text-left`) cho văn bản/tên; căn giữa (`text-center`) cho mã/trạng thái/số buổi; căn lề phải (`text-right`) cho tiền tệ và số lượng.
- **Quy tắc tương phản giá trị (Chống nhiễu thị giác):**
  - **Giá trị rỗng hoặc bằng `0` (ví dụ: `0`, `0 đ`, `+0 đ`, `-0 đ`, `Chưa có`):** Làm mờ bằng `text-stone-400 font-normal` để không gây nhiễu mắt.
  - **Giá trị dương / phát sinh thực tế (`> 0`):** Tô đậm rõ ràng (`text-stone-900 font-semibold`).
  - **Thông số tổng hợp quan trọng nhất (Thực nhận, Tổng doanh thu, Điểm tổng kết):** Nhấn mạnh bằng chữ đậm cỡ lớn hơn (`text-[15px] font-bold text-orange-950`) và có thể lót nền nhẹ `bg-orange-50/40` cho cả cột.
- **Cấu trúc Bảng (Data Table):**
  - Header (`<th>`): Nền `bg-stone-100`, chữ `text-xs font-bold uppercase tracking-wider text-stone-700`, viền dưới dày `border-b-2 border-stone-300`.
  - Hàng (`<tr>`): Phân cách bằng `border-b border-stone-200`, hover rõ nét `hover:bg-orange-50/50`.
- **Thẻ KPI (Stat Cards):**
  - Bỏ nền gradient. Dùng nền trắng, viền `border-stone-300`, bo góc `rounded-lg`. Nhãn phía trên (`text-xs font-semibold uppercase text-stone-500`), con số chính cực rõ (`text-2xl font-bold text-stone-900 tabular-nums`).
- **Badge Trạng thái:**
  - Dùng tag vuông vức `rounded` (4px) kèm viền mảnh thay vì pill tròn mềm:
    - Thành công / Hoạt động: `bg-emerald-50 text-emerald-800 border border-emerald-300 font-semibold`
    - Cảnh báo / Chờ xử lý: `bg-amber-50 text-amber-800 border border-amber-300 font-semibold`
    - Lỗi / Nghỉ / Phạt: `bg-rose-50 text-rose-800 border border-rose-300 font-semibold`
    - Trung tính / Chưa gửi (`NotSent`, `Draft`): `bg-stone-100 text-stone-700 border border-stone-300 font-medium`

---

## 6. ĐỒNG BỘ ELEMENT PLUS (ÁP DỤNG TẠI `main.css`)
Đảm bảo các component Element Plus (`el-button`, `el-input`, `el-select`, `el-date-picker`, `el-table`, `el-pagination`) tự động kế thừa độ "thô" và màu Cam của hệ thống.