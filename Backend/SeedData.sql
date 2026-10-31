-- =============================================================================
-- SCRIPT SEED DATA: HỆ THỐNG QUẢN LÝ TRUNG TÂM TOÁN TƯ DUY (MATH THINKING)
-- Ngữ cảnh: Toán tư duy 4-14 tuổi (Soroban, Finger Math, Mathnasium, Kumon, I-MATH, CPA)
-- Hệ quản trị CSDL: Microsoft SQL Server
-- =============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRY
    BEGIN TRANSACTION;

    PRINT N'>>> Đang xóa dữ liệu cũ theo thứ tự ràng buộc khóa ngoại (FK)...';
    DELETE FROM [dbo].[QUESTION_ASSIGNMENT];
    DELETE FROM [dbo].[STUDENT_ASSIGNMENT];
    DELETE FROM [dbo].[LEAVE_REQUEST];
    DELETE FROM [dbo].[ATTENDANCE];
    DELETE FROM [dbo].[CLASS_STUDENT];
    DELETE FROM [dbo].[EMAIL_VERIFICATION_TOKEN];
    DELETE FROM [dbo].[INVOICE];
    DELETE FROM [dbo].[ASSIGNMENT];
    DELETE FROM [dbo].[LESSON];
    DELETE FROM [dbo].[CLASS_SCHEDULE];
    DELETE FROM [dbo].[StudentBadge];
    DELETE FROM [dbo].[QUESTION];
    DELETE FROM [dbo].[NOTIFICATION_SETTING];
    DELETE FROM [dbo].[NOTIFICATION];
    DELETE FROM [dbo].[CLASSES];
    DELETE FROM [dbo].[STUDENT_PROFILE];
    DELETE FROM [dbo].[QUESTION_BANK];
    DELETE FROM [dbo].[COURSE_LEVEL];
    DELETE FROM [dbo].[PARENT_PROFILE];
    DELETE FROM [dbo].[TEACHER_PROFILE];
    DELETE FROM [dbo].[COURSE];
    DELETE FROM [dbo].[BADGE];
    DELETE FROM [dbo].[AspNetUserRoles];
    DELETE FROM [dbo].[AspNetUserClaims];
    DELETE FROM [dbo].[AspNetUserLogins];
    DELETE FROM [dbo].[AspNetUserTokens];
    DELETE FROM [dbo].[ACCOUNT];

-- =============================================================================
-- 1. BẢNG ACCOUNT (41 dòng: 1 Admin, 8 Giáo viên, 32 Phụ huynh)
-- =============================================================================
-- Tài khoản kiểm thử local: Admin 0901000000 / Admin@Test123!;
-- Teacher 0902000001 / Teacher@Test123!. Không dùng các mật khẩu này ở production.
SET IDENTITY_INSERT [dbo].[ACCOUNT] ON;
INSERT INTO [dbo].[ACCOUNT] (id, phone, password_hash, role, refresh_token, is_active, created_at, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount) VALUES
(1, '0901000000', 'AQAAAAEAAYagAAAAEPHX7xuOKVB3dhLrVO1Yi+UtUT0LEYIAAvWHfGxn48HxdfEjYAWDlcc12ZIbsLTBAw==', 'Admin', NULL, 1, '2026-08-01 08:00:00', '0901000000', '0901000000', 'admin@toantuduy.edu.vn', 'ADMIN@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(2, '0902000001', 'AQAAAAEAAYagAAAAEBdl1Hiyk6Y2riACzj6kgiX3w2CiJEwgXhCPQopl/Ej0NQTw+EzKEP7S6b/UgUdgoA==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000001', '0902000001', 'gv.huynhnhathao@toantuduy.edu.vn', 'GV.HUYNHNHATHAO@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(3, '0902000002', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000002', '0902000002', 'gv.nguyenthimai@toantuduy.edu.vn', 'GV.NGUYENTHIMAI@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(4, '0902000003', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000003', '0902000003', 'gv.tranminhtuan@toantuduy.edu.vn', 'GV.TRANMINHTUAN@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(5, '0902000004', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000004', '0902000004', 'gv.levanphuoc@toantuduy.edu.vn', 'GV.LEVANPHUOC@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(6, '0902000005', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000005', '0902000005', 'gv.phamthithanh@toantuduy.edu.vn', 'GV.PHAMTHITHANH@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(7, '0902000006', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000006', '0902000006', 'gv.hoangquocbao@toantuduy.edu.vn', 'GV.HOANGQUOCBAO@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(8, '0902000007', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000007', '0902000007', 'gv.dothikieuoanh@toantuduy.edu.vn', 'GV.DOTHIKIEUOANH@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(9, '0902000008', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000008', '0902000008', 'gv.vuminhduc@toantuduy.edu.vn', 'GV.VUMINHDUC@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(10, '0903000001', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000001', '0903000001', 'parent01@gmail.com', 'PARENT01@GMAIL.COM', 1, 1, 0, 0, 0),
(11, '0903000002', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000002', '0903000002', 'parent02@gmail.com', 'PARENT02@GMAIL.COM', 1, 1, 0, 0, 0),
(12, '0903000003', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000003', '0903000003', 'parent03@gmail.com', 'PARENT03@GMAIL.COM', 1, 1, 0, 0, 0),
(13, '0903000004', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000004', '0903000004', 'parent04@gmail.com', 'PARENT04@GMAIL.COM', 1, 1, 0, 0, 0),
(14, '0903000005', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000005', '0903000005', 'parent05@gmail.com', 'PARENT05@GMAIL.COM', 1, 1, 0, 0, 0),
(15, '0903000006', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000006', '0903000006', 'parent06@gmail.com', 'PARENT06@GMAIL.COM', 1, 1, 0, 0, 0),
(16, '0903000007', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000007', '0903000007', 'parent07@gmail.com', 'PARENT07@GMAIL.COM', 1, 1, 0, 0, 0),
(17, '0903000008', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000008', '0903000008', 'parent08@gmail.com', 'PARENT08@GMAIL.COM', 1, 1, 0, 0, 0),
(18, '0903000009', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000009', '0903000009', 'parent09@gmail.com', 'PARENT09@GMAIL.COM', 1, 1, 0, 0, 0),
(19, '0903000010', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000010', '0903000010', 'parent10@gmail.com', 'PARENT10@GMAIL.COM', 1, 1, 0, 0, 0),
(20, '0903000011', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000011', '0903000011', 'parent11@gmail.com', 'PARENT11@GMAIL.COM', 1, 1, 0, 0, 0),
(21, '0903000012', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000012', '0903000012', 'parent12@gmail.com', 'PARENT12@GMAIL.COM', 1, 1, 0, 0, 0),
(22, '0903000013', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000013', '0903000013', 'parent13@gmail.com', 'PARENT13@GMAIL.COM', 1, 1, 0, 0, 0),
(23, '0903000014', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000014', '0903000014', 'parent14@gmail.com', 'PARENT14@GMAIL.COM', 1, 1, 0, 0, 0),
(24, '0903000015', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000015', '0903000015', 'parent15@gmail.com', 'PARENT15@GMAIL.COM', 1, 1, 0, 0, 0),
(25, '0903000016', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000016', '0903000016', 'parent16@gmail.com', 'PARENT16@GMAIL.COM', 1, 1, 0, 0, 0),
(26, '0903000017', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000017', '0903000017', 'parent17@gmail.com', 'PARENT17@GMAIL.COM', 1, 1, 0, 0, 0),
(27, '0903000018', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000018', '0903000018', 'parent18@gmail.com', 'PARENT18@GMAIL.COM', 1, 1, 0, 0, 0),
(28, '0903000019', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000019', '0903000019', 'parent19@gmail.com', 'PARENT19@GMAIL.COM', 1, 1, 0, 0, 0),
(29, '0903000020', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000020', '0903000020', 'parent20@gmail.com', 'PARENT20@GMAIL.COM', 1, 1, 0, 0, 0),
(30, '0903000021', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000021', '0903000021', 'parent21@gmail.com', 'PARENT21@GMAIL.COM', 1, 1, 0, 0, 0),
(31, '0903000022', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000022', '0903000022', 'parent22@gmail.com', 'PARENT22@GMAIL.COM', 1, 1, 0, 0, 0),
(32, '0903000023', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000023', '0903000023', 'parent23@gmail.com', 'PARENT23@GMAIL.COM', 1, 1, 0, 0, 0),
(33, '0903000024', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000024', '0903000024', 'parent24@gmail.com', 'PARENT24@GMAIL.COM', 1, 1, 0, 0, 0),
(34, '0903000025', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000025', '0903000025', 'parent25@gmail.com', 'PARENT25@GMAIL.COM', 1, 1, 0, 0, 0),
(35, '0903000026', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000026', '0903000026', 'parent26@gmail.com', 'PARENT26@GMAIL.COM', 1, 1, 0, 0, 0),
(36, '0903000027', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000027', '0903000027', 'parent27@gmail.com', 'PARENT27@GMAIL.COM', 1, 1, 0, 0, 0),
(37, '0903000028', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000028', '0903000028', 'parent28@gmail.com', 'PARENT28@GMAIL.COM', 1, 1, 0, 0, 0),
(38, '0903000029', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000029', '0903000029', 'parent29@gmail.com', 'PARENT29@GMAIL.COM', 1, 1, 0, 0, 0),
(39, '0903000030', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000030', '0903000030', 'parent30@gmail.com', 'PARENT30@GMAIL.COM', 1, 1, 0, 0, 0),
(40, '0903000031', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000031', '0903000031', 'parent31@gmail.com', 'PARENT31@GMAIL.COM', 1, 1, 0, 0, 0),
(41, '0903000032', 'AQAAAAEAACcQAAAAEHashFake1234567890abcdefghijklmnopqrstuvwxyz==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000032', '0903000032', 'parent32@gmail.com', 'PARENT32@GMAIL.COM', 1, 1, 0, 0, 0);
SET IDENTITY_INSERT [dbo].[ACCOUNT] OFF;

-- =============================================================================
-- 2. BẢNG BADGE (20 dòng huy hiệu Toán Tư Duy)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[BADGE] ON;
INSERT INTO [dbo].[BADGE] (id, name, description, icon_url, condition_type, condition_value) VALUES
(1, N'Bàn tính Thần tốc', N'Hoàn thành 50 phép tính Soroban chính xác liên tiếp', 'https://cdn-icons-png.flaticon.com/512/2583/2583344.png', 'SOROBAN_STREAK', 50),
(2, N'Vua Ngón tay', N'Tính nhẩm Finger Math vượt mốc 99 trong vòng 30 giây', 'https://cdn-icons-png.flaticon.com/512/2583/2583319.png', 'FINGER_SPEED', 99),
(3, N'Thần đồng Suy luận', N'Giải đúng 20 câu đố tư duy logic khó liên tiếp', 'https://cdn-icons-png.flaticon.com/512/2583/2583434.png', 'LOGIC_SOLVE', 20),
(4, N'Ngôi sao Kiên trì', N'Duy trì chuỗi học tập 7 ngày liên tục không nghỉ', 'https://cdn-icons-png.flaticon.com/512/2583/2583277.png', 'STREAK_DAYS', 7),
(5, N'Thợ săn Điểm số', N'Tích lũy đạt mốc 1000 điểm tư duy', 'https://cdn-icons-png.flaticon.com/512/2583/2583386.png', 'TOTAL_POINTS', 1000),
(6, N'Đại kiện tướng CPA', N'Hoàn thành xuất sắc tiến trình Chuyển đổi Trừu tượng CPA', 'https://cdn-icons-png.flaticon.com/512/2583/2583448.png', 'CPA_MASTER', 30),
(7, N'Bậc thầy Hình học', N'Nhận diện và ghép hình học không gian chính xác 100%', 'https://cdn-icons-png.flaticon.com/512/2583/2583325.png', 'GEOMETRY_PERFECT', 15),
(8, N'Nhà khám phá Mathnasium', N'Hoàn thiện toàn bộ phiếu bài tập A* của Mathnasium', 'https://cdn-icons-png.flaticon.com/512/2583/2583307.png', 'WORKSHEET_COMPLETED', 25),
(9, N'Kỷ lục gia Kumon', N'Tự giác hoàn thành bài tập Kumon 14 ngày liên tiếp', 'https://cdn-icons-png.flaticon.com/512/2583/2583350.png', 'KUMON_SELF_STUDY', 14),
(10, N'Nhà hùng biện Socrates', N'Giải thích thành công cách giải bài toán theo phương pháp I-MATH', 'https://cdn-icons-png.flaticon.com/512/2583/2583375.png', 'IMATH_EXPLAIN', 10),
(11, N'Chiến binh Bất bại', N'Đạt điểm 10 tuyệt đối trong 5 bài kiểm tra định kỳ', 'https://cdn-icons-png.flaticon.com/512/2583/2583362.png', 'PERFECT_SCORE', 5),
(12, N'Vua Phản xạ 3 giây', N'Đưa ra đáp án tính nhẩm chính xác trong vòng dưới 3 giây', 'https://cdn-icons-png.flaticon.com/512/2583/2583390.png', 'FAST_RESPONSE', 30),
(13, N'Tân binh Xuất sắc', N'Hoàn thành khóa học đầu tiên tại trung tâm với thành tích Tốt', 'https://cdn-icons-png.flaticon.com/512/2583/2583314.png', 'FIRST_COURSE_PASS', 1),
(14, N'Chuyên cần Vàng', N'Đi học chuyên cần đầy đủ 100% các buổi học trong tháng', 'https://cdn-icons-png.flaticon.com/512/2583/2583349.png', 'ATTENDANCE_FULL', 12),
(15, N'Bậc thầy Nhân chia', N'Thành thạo bảng cửu chương và phép nhân chia 3 chữ số trên bàn tính', 'https://cdn-icons-png.flaticon.com/512/2583/2583370.png', 'MULTIPLICATION_MASTER', 50),
(16, N'Thần tốc Tính nhẩm', N'Cộng trừ liên tiếp 10 số có 2 chữ số trong 5 giây', 'https://cdn-icons-png.flaticon.com/512/2583/2583329.png', 'MENTAL_MATH_SPEED', 10),
(17, N'Đôi mắt Tinh tường', N'Tìm đúng quy luật dãy số phức tạp trong vòng 1 phút', 'https://cdn-icons-png.flaticon.com/512/2583/2583357.png', 'PATTERN_RECOGNITION', 10),
(18, N'Tinh thần Đồng đội', N'Hỗ trợ bạn cùng bàn giải toán tư duy nhóm hiệu quả', 'https://cdn-icons-png.flaticon.com/512/2583/2583380.png', 'TEAMWORK', 5),
(19, N'Học sinh Tiêu biểu Tháng', N'Đạt danh hiệu Học sinh tiêu biểu xuất sắc của tháng', 'https://cdn-icons-png.flaticon.com/512/2583/2583420.png', 'STUDENT_OF_MONTH', 1),
(20, N'Chinh phục 100 Sao', N'Tích lũy 100 ngôi sao thưởng từ các bài tập về nhà', 'https://cdn-icons-png.flaticon.com/512/2583/2583439.png', 'STARS_COLLECTED', 100);
SET IDENTITY_INSERT [dbo].[BADGE] OFF;

-- =============================================================================
-- 3. BẢNG COURSE (6 khóa học Toán Tư Duy chủ đạo)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[COURSE] ON;
INSERT INTO [dbo].[COURSE] (id, code, name, description, min_age, max_age, image_url, video_url, benefits, facility_images, rating, reviews, status, created_at, updated_at) VALUES
(1, 'SRB', N'Soroban / UCMAS - Bàn tính Số học Nhật Bản', N'Phương pháp rèn luyện trí não thông qua bàn tính cổ truyền Soroban, giúp trẻ phản xạ tính nhẩm siêu tốc, gia tăng sự tập trung và phát triển não bộ đồng đều cả 2 bán cầu.', 6, 12, 'https://images.unsplash.com/photo-1596495578065-6e0763fa1178', NULL, NULL, NULL, 5.0, 0, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(2, 'FGM', N'Finger Math - Toán ngón tay Hàn Quốc', N'Phương pháp tính nhẩm toán học bằng đôi bàn tay linh hoạt, giúp trẻ từ 4-8 tuổi làm quen với các phép cộng trừ trong phạm vi 99 một cách trực quan, vui nhộn.', 4, 8, 'https://images.unsplash.com/photo-1509228468518-180dd4864904', NULL, NULL, NULL, 5.0, 0, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(3, 'MTN', N'Mathnasium - Toán tư duy Hoa Kỳ', N'Chương trình toán tư duy theo lộ trình cá nhân hóa Mathnasium Method từ Mỹ, xây dựng nền tảng từ bản chất toán học, phát triển tư duy định lượng và suy luận logic sâu sắc.', 6, 14, 'https://images.unsplash.com/photo-1580582932707-520aed937b7b', NULL, NULL, NULL, 5.0, 0, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(4, 'KMN', N'Kumon - Toán tự học Nhật Bản', N'Hệ thống tự học toán Kumon chuẩn Nhật Bản giúp học sinh hình thành thói quen kỷ luật, độc lập suy nghĩ và nắm vững kỹ năng thao tác tính toán tốc độ cao vượt cấp lớp học.', 5, 12, 'https://images.unsplash.com/photo-1427504494785-3a9ca7044f45', NULL, NULL, NULL, 5.0, 0, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(5, 'IMT', N'I-MATH / Maieutic - Toán tư duy gợi mở Socrates', N'Khóa học khơi gợi tư duy phản biện qua phương pháp vấn đáp Maieutic, khuyến khích học sinh tự đặt câu hỏi tại sao, tranh luận và tự khám phá giải pháp cho bài toán.', 7, 14, 'https://images.unsplash.com/photo-1503676260728-1c00da094a0b', NULL, NULL, NULL, 5.0, 0, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(6, 'CPA', N'Tiến trình CPA - Concrete Pictorial Abstract', N'Phương pháp học toán qua 3 bước: Cụ thể (Vật thể thực) - Minh họa (Hình ảnh biểu diễn) - Trừu tượng (Ký hiệu toán học), giúp trẻ hiểu sâu bản chất, không học vẹt.', 4, 10, 'https://images.unsplash.com/photo-1577896851231-70ef18881754', NULL, NULL, NULL, 5.0, 0, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00');
SET IDENTITY_INSERT [dbo].[COURSE] OFF;

-- =============================================================================
-- 4. BẢNG TEACHER_PROFILE (8 giáo viên chuyên môn cao)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[TEACHER_PROFILE] ON;
INSERT INTO [dbo].[TEACHER_PROFILE] (id, account_id, full_name, email, specialize, created_at) VALUES
(1, 2, N'Huỳnh Nhật Hào', 'gv.huynhnhathao@toantuduy.edu.vn', N'Toán bàn tính Soroban & UCMAS Master', '2026-08-01 08:00:00'),
(2, 3, N'Nguyễn Thị Mai', 'gv.nguyenthimai@toantuduy.edu.vn', N'Finger Math Mầm non & Tiểu học', '2026-08-01 08:00:00'),
(3, 4, N'Trần Minh Tuấn', 'gv.tranminhtuan@toantuduy.edu.vn', N'Toán tư duy Mathnasium Hoa Kỳ', '2026-08-01 08:00:00'),
(4, 5, N'Lê Văn Phước', 'gv.levanphuoc@toantuduy.edu.vn', N'Phương pháp tự học Kumon Nhật Bản', '2026-08-01 08:00:00'),
(5, 6, N'Phạm Thị Thanh', 'gv.phamthithanh@toantuduy.edu.vn', N'I-MATH & Gợi mở Maieutic Socrates', '2026-08-01 08:00:00'),
(6, 7, N'Hoàng Quốc Bảo', 'gv.hoangquocbao@toantuduy.edu.vn', N'Tiến trình CPA & Singapore Math', '2026-08-01 08:00:00'),
(7, 8, N'Đỗ Thị Kiều Oanh', 'gv.dothikieuoanh@toantuduy.edu.vn', N'Tư duy Hình học & Không gian', '2026-08-01 08:00:00'),
(8, 9, N'Vũ Minh Đức', 'gv.vuminhduc@toantuduy.edu.vn', N'Số học & Kỹ năng tính nhanh', '2026-08-01 08:00:00');
SET IDENTITY_INSERT [dbo].[TEACHER_PROFILE] OFF;

-- =============================================================================
-- 5. BẢNG PARENT_PROFILE (32 phụ huynh)
-- =============================================================================
-- PIN để NULL vì dịch vụ yêu cầu giá trị hash từ ASP.NET PasswordHasher, không phải PIN thô.
SET IDENTITY_INSERT [dbo].[PARENT_PROFILE] ON;
INSERT INTO [dbo].[PARENT_PROFILE] (id, account_id, full_name, pin, avatar, created_at) VALUES
(1, 10, N'Trần Quang Huy', NULL, 'https://randomuser.me/api/portraits/men/1.jpg', '2026-08-01 08:00:00'),
(2, 11, N'Nguyễn Thu Trang', NULL, 'https://randomuser.me/api/portraits/women/2.jpg', '2026-08-01 08:00:00'),
(3, 12, N'Lê Hoàng Nam', NULL, 'https://randomuser.me/api/portraits/men/3.jpg', '2026-08-01 08:00:00'),
(4, 13, N'Phạm Mỹ Duyên', NULL, 'https://randomuser.me/api/portraits/women/4.jpg', '2026-08-01 08:00:00'),
(5, 14, N'Vũ Đình Trọng', NULL, 'https://randomuser.me/api/portraits/men/5.jpg', '2026-08-01 08:00:00'),
(6, 15, N'Hoàng Lan Anh', NULL, 'https://randomuser.me/api/portraits/women/6.jpg', '2026-08-01 08:00:00'),
(7, 16, N'Đỗ Quốc Hùng', NULL, 'https://randomuser.me/api/portraits/men/7.jpg', '2026-08-01 08:00:00'),
(8, 17, N'Bùi Phương Thảo', NULL, 'https://randomuser.me/api/portraits/women/8.jpg', '2026-08-01 08:00:00'),
(9, 18, N'Ngô Tiến Đạt', NULL, 'https://randomuser.me/api/portraits/men/9.jpg', '2026-08-01 08:00:00'),
(10, 19, N'Đặng Kim Ngân', NULL, 'https://randomuser.me/api/portraits/women/10.jpg', '2026-08-01 08:00:00'),
(11, 20, N'Dương Gia Bảo', NULL, 'https://randomuser.me/api/portraits/men/11.jpg', '2026-08-01 08:00:00'),
(12, 21, N'Lý Khánh Linh', NULL, 'https://randomuser.me/api/portraits/women/12.jpg', '2026-08-01 08:00:00'),
(13, 22, N'Trịnh Anh Tuấn', NULL, 'https://randomuser.me/api/portraits/men/13.jpg', '2026-08-01 08:00:00'),
(14, 23, N'Phan Ngọc Ánh', NULL, 'https://randomuser.me/api/portraits/women/14.jpg', '2026-08-01 08:00:00'),
(15, 24, N'Mai Hữu Nghĩa', NULL, 'https://randomuser.me/api/portraits/men/15.jpg', '2026-08-01 08:00:00'),
(16, 25, N'Võ Bích Hạnh', NULL, 'https://randomuser.me/api/portraits/women/16.jpg', '2026-08-01 08:00:00'),
(17, 26, N'Lâm Khải Hoàn', NULL, 'https://randomuser.me/api/portraits/men/17.jpg', '2026-08-01 08:00:00'),
(18, 27, N'Châu Thùy Dung', NULL, 'https://randomuser.me/api/portraits/women/18.jpg', '2026-08-01 08:00:00'),
(19, 28, N'Đoàn Minh Triết', NULL, 'https://randomuser.me/api/portraits/men/19.jpg', '2026-08-01 08:00:00'),
(20, 29, N'Hồ Thu Hà', NULL, 'https://randomuser.me/api/portraits/women/20.jpg', '2026-08-01 08:00:00'),
(21, 30, N'Tạ Thanh Bình', NULL, 'https://randomuser.me/api/portraits/men/21.jpg', '2026-08-01 08:00:00'),
(22, 31, N'Đinh Hồng Nhung', NULL, 'https://randomuser.me/api/portraits/women/22.jpg', '2026-08-01 08:00:00'),
(23, 32, N'Lương Tuấn Khang', NULL, 'https://randomuser.me/api/portraits/men/23.jpg', '2026-08-01 08:00:00'),
(24, 33, N'Tô Yến Nhi', NULL, 'https://randomuser.me/api/portraits/women/24.jpg', '2026-08-01 08:00:00'),
(25, 34, N'Quách Văn Thái', NULL, 'https://randomuser.me/api/portraits/men/25.jpg', '2026-08-01 08:00:00'),
(26, 35, N'Lê Thị Hương', NULL, 'https://randomuser.me/api/portraits/women/26.jpg', '2026-08-01 08:00:00'),
(27, 36, N'Nguyễn Đức Long', NULL, 'https://randomuser.me/api/portraits/men/27.jpg', '2026-08-01 08:00:00'),
(28, 37, N'Trần Thị Cẩm Tú', NULL, 'https://randomuser.me/api/portraits/women/28.jpg', '2026-08-01 08:00:00'),
(29, 38, N'Phạm Quốc Việt', NULL, 'https://randomuser.me/api/portraits/men/29.jpg', '2026-08-01 08:00:00'),
(30, 39, N'Vũ Thị Hồng', NULL, 'https://randomuser.me/api/portraits/women/30.jpg', '2026-08-01 08:00:00'),
(31, 40, N'Hoàng Trọng Nghĩa', NULL, 'https://randomuser.me/api/portraits/men/31.jpg', '2026-08-01 08:00:00'),
(32, 41, N'Đặng Thanh Mai', NULL, 'https://randomuser.me/api/portraits/women/32.jpg', '2026-08-01 08:00:00');
SET IDENTITY_INSERT [dbo].[PARENT_PROFILE] OFF;

-- =============================================================================
-- 6. BẢNG COURSE_LEVEL (18 cấp độ: 3 cấp độ / khóa)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[COURSE_LEVEL] ON;
INSERT INTO [dbo].[COURSE_LEVEL] (id, course_id, code, name, fee, total_sessions, order_index, status, created_at, updated_at) VALUES
(1, 1, 'SRB-L1', N'Soroban Cơ bản (Làm quen bàn tính & Cộng trừ không nhớ)', 2400000, 24, 1, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(2, 1, 'SRB-L2', N'Soroban Trung cấp (Phép nhân & Chia 2-3 chữ số)', 2800000, 24, 2, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(3, 1, 'SRB-L3', N'Soroban Nâng cao (Ảo tính Anzan & Tính nhẩm siêu tốc)', 3200000, 24, 3, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(4, 2, 'FGM-L1', N'Finger Math Nhập môn (Nhận diện bàn tay 0-9)', 1800000, 16, 1, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(5, 2, 'FGM-L2', N'Finger Math Cơ bản (Cộng trừ không nhớ phạm vi 99)', 2200000, 20, 2, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(6, 2, 'FGM-L3', N'Finger Math Nâng cao (Cộng trừ có nhớ & Tốc độ cao)', 2600000, 20, 3, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(7, 3, 'MTN-L1', N'Mathnasium Level 1 - Tư duy Số lượng & Đo lường', 3000000, 24, 1, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(8, 3, 'MTN-L2', N'Mathnasium Level 2 - Phân tích & Hình học trực quan', 3400000, 24, 2, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(9, 3, 'MTN-L3', N'Mathnasium Level 3 - Giải quyết vấn đề & Logic đại số', 3800000, 24, 3, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(10, 4, 'KMN-L1', N'Kumon Starter - Nhận diện quy luật dãy số', 2000000, 20, 1, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(11, 4, 'KMN-L2', N'Kumon Elementary - Thành thạo 4 phép tính cơ bản', 2400000, 24, 2, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(12, 4, 'KMN-L3', N'Kumon Advanced - Tính toán phân số & Toán đố', 2800000, 24, 3, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(13, 5, 'IMT-L1', N'I-MATH Foundation - Khởi động Tư duy phản biện', 2600000, 20, 1, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(14, 5, 'IMT-L2', N'I-MATH Explorer - Phương pháp gợi mở Socrates', 3100000, 24, 2, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(15, 5, 'IMT-L3', N'I-MATH Master - Tự lập luận & Tranh luận toán học', 3600000, 24, 3, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(16, 6, 'CPA-L1', N'CPA Concrete - Khám phá qua Học cụ thực hành', 2200000, 18, 1, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(17, 6, 'CPA-L2', N'CPA Pictorial - Mô hình hóa sơ đồ & Thanh Bar Model', 2600000, 20, 2, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00'),
(18, 6, 'CPA-L3', N'CPA Abstract - Chinh phục Ký hiệu & Công thức trừu tượng', 3000000, 24, 3, 'ACTIVE', '2026-08-01 08:00:00', '2026-08-01 08:00:00');
SET IDENTITY_INSERT [dbo].[COURSE_LEVEL] OFF;

-- =============================================================================
-- 7. BẢNG QUESTION_BANK (20 ngân hàng câu hỏi phân theo khóa học)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[QUESTION_BANK] ON;
INSERT INTO [dbo].[QUESTION_BANK] (id, course_id, name, description) VALUES
(1, 1, N'Ngân hàng Soroban - Thao tác hạt đơn vị', N'Gồm các bài tập gảy hạt đơn giản và cộng trừ không nhớ'),
(2, 1, N'Ngân hàng Soroban - Công thức Anh bạn nhỏ', N'Bài tập kết hợp hạt 5 (cộng trừ 1, 2, 3, 4 có sự trợ giúp của hạt 5)'),
(3, 1, N'Ngân hàng Soroban - Công thức Anh bạn lớn', N'Bài tập qua cầu 10 (kết hợp các cặp số có tổng bằng 10)'),
(4, 1, N'Ngân hàng Soroban - Ảo tính Anzan nhẩm', N'Luyện tập hình dung bàn tính trong tâm trí và tính nhẩm tốc độ'),
(5, 2, N'Ngân hàng Finger Math - Bàn tay phải 0-9', N'Nhận diện và gập xòe các ngón tay hàng đơn vị'),
(6, 2, N'Ngân hàng Finger Math - Hai bàn tay 0-99', N'Cộng trừ không nhớ và có nhớ trên cả hai bàn tay'),
(7, 2, N'Ngân hàng Finger Math - Thử thách tốc độ', N'Các bài tập phản xạ ngón tay tính toán nhanh'),
(8, 3, N'Ngân hàng Mathnasium - Cấu trúc số & Tròn chục', N'Tư duy về số lượng, giá trị vị trí và ước lượng định lượng'),
(9, 3, N'Ngân hàng Mathnasium - Hình học không gian trực quan', N'Nhận biết khối hình, trục đối xứng và góc xoay'),
(10, 3, N'Ngân hàng Mathnasium - Bài toán thực tế có lời văn', N'Phân tích ngữ cảnh, xác định dữ kiện và xây dựng biểu thức'),
(11, 4, N'Ngân hàng Kumon - Dãy số & Quy luật tiến lùi', N'Điền số còn thiếu vào quy luật dãy số tăng giảm đều'),
(12, 4, N'Ngân hàng Kumon - Bảng tính 4 phép tính cơ bản', N'Phiếu bài tập rèn phản xạ tính toán chuẩn xác và tốc độ'),
(13, 4, N'Ngân hàng Kumon - Phân số & Tỉ lệ thức', N'Rút gọn, quy đồng mẫu số và toán ứng dụng phân số'),
(14, 5, N'Ngân hàng I-MATH - Suy luận logic & Ngụy biện', N'Phát hiện lỗi suy luận sai trong các lập luận toán học'),
(15, 5, N'Ngân hàng I-MATH - Câu hỏi mở & Thảo luận Socrates', N'Bài toán mở khuyến khích trẻ tìm nhiều cách giải khác nhau'),
(16, 5, N'Ngân hàng I-MATH - Nghịch lý & Thử thách tư duy', N'Những bài toán đố hóc búa đòi hỏi tư duy đa chiều'),
(17, 6, N'Ngân hàng CPA - Khám phá vật thể thực Concrete', N'Đếm khối lập phương, que tính và phân nhóm đồ vật'),
(18, 6, N'Ngân hàng CPA - Mô hình hóa Pictorial Bar Model', N'Vẽ sơ đồ thanh và mô hình khối giải bài toán đố'),
(19, 6, N'Ngân hàng CPA - Đại số trừu tượng Abstract', N'Chuyển đổi hình vẽ thành phương trình và ký hiệu toán học'),
(20, 3, N'Ngân hàng Đánh giá Năng lực Toàn diện', N'Bộ đề kiểm tra đánh giá chỉ số thông minh toán học (MQ)');
SET IDENTITY_INSERT [dbo].[QUESTION_BANK] OFF;

-- =============================================================================
-- 8. BẢNG STUDENT_PROFILE (40 học sinh)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[STUDENT_PROFILE] ON;
INSERT INTO [dbo].[STUDENT_PROFILE] (id, parent_id, full_name, avatar, total_points, gender, stars, streak_days, is_active, created_at) VALUES
(1, 1, N'Trần Minh Khang', 'https://api.dicebear.com/7.x/bottts/svg?seed=student1', 650, N'Nam', 32, 7, 1, '2026-08-15 08:00:00'),
(2, 1, N'Trần Bảo Ngọc', 'https://api.dicebear.com/7.x/bottts/svg?seed=student2', 480, N'Nữ', 24, 5, 1, '2026-08-15 08:00:00'),
(3, 2, N'Nguyễn Hoàng Nam', 'https://api.dicebear.com/7.x/bottts/svg?seed=student3', 820, N'Nam', 45, 12, 1, '2026-08-15 08:00:00'),
(4, 3, N'Lê Gia Hưng', 'https://api.dicebear.com/7.x/bottts/svg?seed=student4', 390, N'Nam', 18, 3, 1, '2026-08-15 08:00:00'),
(5, 4, N'Phạm Hải Đăng', 'https://api.dicebear.com/7.x/bottts/svg?seed=student5', 950, N'Nam', 55, 15, 1, '2026-08-15 08:00:00'),
(6, 5, N'Vũ Quỳnh Anh', 'https://api.dicebear.com/7.x/bottts/svg?seed=student6', 520, N'Nữ', 28, 6, 1, '2026-08-15 08:00:00'),
(7, 6, N'Hoàng Tuấn Kiệt', 'https://api.dicebear.com/7.x/bottts/svg?seed=student7', 710, N'Nam', 38, 9, 1, '2026-08-15 08:00:00'),
(8, 7, N'Đỗ Minh Trí', 'https://api.dicebear.com/7.x/bottts/svg?seed=student8', 1100, N'Nam', 62, 20, 1, '2026-08-15 08:00:00'),
(9, 8, N'Bùi Phương Linh', 'https://api.dicebear.com/7.x/bottts/svg?seed=student9', 600, N'Nữ', 30, 8, 1, '2026-08-15 08:00:00'),
(10, 8, N'Bùi Quang Dũng', 'https://api.dicebear.com/7.x/bottts/svg?seed=student10', 430, N'Nam', 22, 4, 1, '2026-08-15 08:00:00'),
(11, 9, N'Ngô Thảo Nguyên', 'https://api.dicebear.com/7.x/bottts/svg?seed=student11', 880, N'Nữ', 48, 14, 1, '2026-08-15 08:00:00'),
(12, 10, N'Đặng Bảo Trâm', 'https://api.dicebear.com/7.x/bottts/svg?seed=student12', 340, N'Nữ', 16, 2, 1, '2026-08-15 08:00:00'),
(13, 11, N'Dương Trọng Nhân', 'https://api.dicebear.com/7.x/bottts/svg?seed=student13', 760, N'Nam', 40, 11, 1, '2026-08-15 08:00:00'),
(14, 12, N'Lý Thục Anh', 'https://api.dicebear.com/7.x/bottts/svg?seed=student14', 920, N'Nữ', 50, 16, 1, '2026-08-15 08:00:00'),
(15, 13, N'Trịnh Phúc Lâm', 'https://api.dicebear.com/7.x/bottts/svg?seed=student15', 540, N'Nam', 26, 6, 1, '2026-08-15 08:00:00'),
(16, 14, N'Phan Ngọc Diệp', 'https://api.dicebear.com/7.x/bottts/svg?seed=student16', 670, N'Nữ', 35, 10, 1, '2026-08-15 08:00:00'),
(17, 15, N'Mai Hữu Phước', 'https://api.dicebear.com/7.x/bottts/svg?seed=student17', 410, N'Nam', 20, 4, 1, '2026-08-15 08:00:00'),
(18, 16, N'Võ Khánh An', 'https://api.dicebear.com/7.x/bottts/svg?seed=student18', 1250, N'Nữ', 70, 22, 1, '2026-08-15 08:00:00'),
(19, 17, N'Lâm Thiên Kim', 'https://api.dicebear.com/7.x/bottts/svg?seed=student19', 580, N'Nữ', 29, 7, 1, '2026-08-15 08:00:00'),
(20, 18, N'Châu Anh Quân', 'https://api.dicebear.com/7.x/bottts/svg?seed=student20', 830, N'Nam', 44, 13, 1, '2026-08-15 08:00:00'),
(21, 19, N'Đoàn Bảo Uyên', 'https://api.dicebear.com/7.x/bottts/svg?seed=student21', 490, N'Nữ', 25, 5, 1, '2026-08-15 08:00:00'),
(22, 20, N'Hồ Đăng Khoa', 'https://api.dicebear.com/7.x/bottts/svg?seed=student22', 1020, N'Nam', 58, 18, 1, '2026-08-15 08:00:00'),
(23, 21, N'Tạ Minh Quân', 'https://api.dicebear.com/7.x/bottts/svg?seed=student23', 370, N'Nam', 17, 3, 1, '2026-08-15 08:00:00'),
(24, 22, N'Đinh Thùy Chi', 'https://api.dicebear.com/7.x/bottts/svg?seed=student24', 640, N'Nữ', 33, 8, 1, '2026-08-15 08:00:00'),
(25, 23, N'Lương Tuấn Khải', 'https://api.dicebear.com/7.x/bottts/svg?seed=student25', 780, N'Nam', 42, 12, 1, '2026-08-15 08:00:00'),
(26, 24, N'Tô Ngọc Hân', 'https://api.dicebear.com/7.x/bottts/svg?seed=student26', 890, N'Nữ', 47, 15, 1, '2026-08-15 08:00:00'),
(27, 25, N'Quách Nhật Minh', 'https://api.dicebear.com/7.x/bottts/svg?seed=student27', 460, N'Nam', 23, 5, 1, '2026-08-15 08:00:00'),
(28, 26, N'Lê Minh Đức', 'https://api.dicebear.com/7.x/bottts/svg?seed=student28', 720, N'Nam', 37, 9, 1, '2026-08-15 08:00:00'),
(29, 27, N'Nguyễn Khánh Vy', 'https://api.dicebear.com/7.x/bottts/svg?seed=student29', 1150, N'Nữ', 65, 21, 1, '2026-08-15 08:00:00'),
(30, 28, N'Trần Tuấn Phong', 'https://api.dicebear.com/7.x/bottts/svg?seed=student30', 510, N'Nam', 27, 6, 1, '2026-08-15 08:00:00'),
(31, 29, N'Phạm Thảo Vy', 'https://api.dicebear.com/7.x/bottts/svg?seed=student31', 800, N'Nữ', 43, 12, 1, '2026-08-15 08:00:00'),
(32, 30, N'Vũ Hữu Đạt', 'https://api.dicebear.com/7.x/bottts/svg?seed=student32', 360, N'Nam', 19, 3, 1, '2026-08-15 08:00:00'),
(33, 31, N'Hoàng Bảo Long', 'https://api.dicebear.com/7.x/bottts/svg?seed=student33', 980, N'Nam', 53, 17, 1, '2026-08-15 08:00:00'),
(34, 32, N'Đặng Thanh Thảo', 'https://api.dicebear.com/7.x/bottts/svg?seed=student34', 630, N'Nữ', 31, 8, 1, '2026-08-15 08:00:00'),
(35, 2, N'Nguyễn Mai Anh', 'https://api.dicebear.com/7.x/bottts/svg?seed=student35', 420, N'Nữ', 21, 4, 1, '2026-08-15 08:00:00'),
(36, 3, N'Lê Tuấn Tú', 'https://api.dicebear.com/7.x/bottts/svg?seed=student36', 850, N'Nam', 46, 14, 1, '2026-08-15 08:00:00'),
(37, 5, N'Vũ Hoàng Yến', 'https://api.dicebear.com/7.x/bottts/svg?seed=student37', 740, N'Nữ', 39, 10, 1, '2026-08-15 08:00:00'),
(38, 7, N'Đỗ Minh Châu', 'https://api.dicebear.com/7.x/bottts/svg?seed=student38', 910, N'Nữ', 49, 16, 1, '2026-08-15 08:00:00'),
(39, 11, N'Dương Gia Hân', 'https://api.dicebear.com/7.x/bottts/svg?seed=student39', 570, N'Nữ', 28, 7, 1, '2026-08-15 08:00:00'),
(40, 15, N'Mai Quỳnh Chi', 'https://api.dicebear.com/7.x/bottts/svg?seed=student40', 680, N'Nữ', 36, 9, 1, '2026-08-15 08:00:00');
SET IDENTITY_INSERT [dbo].[STUDENT_PROFILE] OFF;

-- =============================================================================
-- 9. BẢNG CLASSES (20 lớp học Toán Tư Duy)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[CLASSES] ON;
INSERT INTO [dbo].[CLASSES] (id, course_level_id, teacher_id, name, max_capacity, start_date, end_date, status, created_at) VALUES
(1, 1, 1, N'Lớp Soroban Cơ bản SRB-01', 15, '2026-09-01 08:00:00', '2026-11-30 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(2, 1, 1, N'Lớp Soroban Cơ bản SRB-02', 15, '2026-09-05 08:00:00', '2026-12-05 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(3, 2, 1, N'Lớp Soroban Trung cấp SRB-03', 12, '2026-09-10 08:00:00', '2026-12-10 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(4, 3, 1, N'Lớp Soroban Nâng cao SRB-04', 10, '2026-09-15 08:00:00', '2026-12-15 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(5, 4, 2, N'Lớp Finger Math Mầm non FGM-01', 12, '2026-09-01 08:00:00', '2026-11-15 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(6, 5, 2, N'Lớp Finger Math Cơ bản FGM-02', 15, '2026-09-02 08:00:00', '2026-11-20 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(7, 6, 2, N'Lớp Finger Math Nâng cao FGM-03', 12, '2026-09-03 08:00:00', '2026-11-25 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(8, 7, 3, N'Lớp Mathnasium Level 1 MTN-01', 10, '2026-09-01 08:00:00', '2026-12-01 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(9, 8, 3, N'Lớp Mathnasium Level 2 MTN-02', 10, '2026-09-05 08:00:00', '2026-12-05 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(10, 9, 3, N'Lớp Mathnasium Level 3 MTN-03', 10, '2026-09-10 08:00:00', '2026-12-10 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(11, 10, 4, N'Lớp Kumon Starter KMN-01', 15, '2026-09-01 08:00:00', '2026-11-30 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(12, 11, 4, N'Lớp Kumon Elementary KMN-02', 15, '2026-09-05 08:00:00', '2026-12-05 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(13, 12, 4, N'Lớp Kumon Advanced KMN-03', 12, '2026-09-10 08:00:00', '2026-12-10 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(14, 13, 5, N'Lớp I-MATH Foundation IMT-01', 12, '2026-09-01 08:00:00', '2026-11-20 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(15, 14, 5, N'Lớp I-MATH Explorer IMT-02', 12, '2026-09-05 08:00:00', '2026-11-25 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(16, 15, 5, N'Lớp I-MATH Master IMT-03', 10, '2026-09-10 08:00:00', '2026-12-01 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(17, 16, 6, N'Lớp CPA Concrete CPA-01', 12, '2026-09-01 08:00:00', '2026-11-15 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(18, 17, 6, N'Lớp CPA Pictorial CPA-02', 12, '2026-09-05 08:00:00', '2026-11-20 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(19, 18, 6, N'Lớp CPA Abstract CPA-03', 12, '2026-09-10 08:00:00', '2026-12-01 17:00:00', 'ACTIVE', '2026-08-01 08:00:00'),
(20, 8, 7, N'Lớp Tư duy Không gian MTN-SP01', 10, '2026-09-15 08:00:00', '2026-12-15 17:00:00', 'ACTIVE', '2026-08-01 08:00:00');
SET IDENTITY_INSERT [dbo].[CLASSES] OFF;

-- =============================================================================
-- 10. BẢNG NOTIFICATION (40 thông báo cho phụ huynh)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[NOTIFICATION] ON;
INSERT INTO [dbo].[NOTIFICATION] (id, parent_id, title, body, type, ref_id, is_read, created_at) VALUES
(1, 1, N'Chào mừng đến với Trung tâm Toán Tư Duy', N'Kính chào phụ huynh Trần Quang Huy, bé Khang đã được ghi danh lớp Soroban SRB-01.', 'SYSTEM', 'REF-SYS-01', 1, '2026-08-20 09:00:00'),
(2, 1, N'Nhắc nhở học phí khóa học', N'Hóa đơn học phí tháng 9 của bé Khang đã được khởi tạo. Xin vui lòng kiểm tra.', 'TUITION', 'REF-INV-01', 1, '2026-08-25 10:00:00'),
(3, 2, N'Báo cáo tiến độ tuần 1', N'Bé Nguyễn Hoàng Nam đã hoàn thành xuất sắc 100% bài tập tuần đầu tiên.', 'ACADEMIC', 'REF-REP-01', 0, '2026-09-08 17:30:00'),
(4, 3, N'Điểm danh bé Gia Hưng', N'Bé Lê Gia Hưng đã có mặt tại lớp đúng giờ hôm nay.', 'ATTENDANCE', 'REF-ATT-01', 1, '2026-09-02 17:35:00'),
(5, 4, N'Chúc mừng bé nhận huy hiệu', N'Bé Phạm Hải Đăng vừa nhận được huy hiệu Thần đồng Suy luận!', 'ACADEMIC', 'REF-BDG-01', 0, '2026-09-10 19:00:00'),
(6, 5, N'Thông báo lịch nghỉ bù ngày lễ', N'Trung tâm xin thông báo lịch nghỉ Quốc khánh và lịch học bù của bé Quỳnh Anh.', 'SYSTEM', 'REF-SYS-02', 1, '2026-08-30 08:00:00'),
(7, 6, N'Kết quả kiểm tra giữa kỳ', N'Bé Hoàng Tuấn Kiệt đạt 9.5 điểm trong bài kiểm tra Finger Math nâng cao.', 'ACADEMIC', 'REF-ASM-01', 0, '2026-09-20 18:00:00'),
(8, 7, N'Nhắc nhở chuẩn bị học cụ Soroban', N'Kính nhờ phụ huynh chuẩn bị bàn tính Soroban tiêu chuẩn 17 cột cho buổi học tới.', 'ACADEMIC', 'REF-ACA-01', 1, '2026-09-01 14:00:00'),
(9, 8, N'Biên lai thanh toán thành công', N'Học phí lớp Mathnasium của bé Phương Linh đã thanh toán thành công qua VNPay.', 'TUITION', 'REF-INV-02', 1, '2026-08-28 11:15:00'),
(10, 9, N'Bé đạt chuỗi học tập 14 ngày', N'Bé Ngô Thảo Nguyên duy trì kỷ lục làm bài tập Kumon 14 ngày liên tiếp!', 'ACADEMIC', 'REF-STK-01', 0, '2026-09-18 20:00:00'),
(11, 10, N'Đơn xin nghỉ học đã được duyệt', N'Đơn xin nghỉ buổi học ngày 15/09 của bé Bảo Trâm đã được giáo viên phê duyệt.', 'ATTENDANCE', 'REF-LRQ-01', 1, '2026-09-14 16:00:00'),
(12, 11, N'Thông báo họp phụ huynh định kỳ', N'Trân trọng kính mời phụ huynh tham dự buổi đánh giá năng lực tư duy cuối khóa.', 'SYSTEM', 'REF-SYS-03', 0, '2026-09-25 09:00:00'),
(13, 12, N'Bài tập về nhà mới được giao', N'Giáo viên vừa giao bài tập: Thử thách Bar Model bài toán Tổng - Hiệu.', 'ACADEMIC', 'REF-ASM-02', 1, '2026-09-12 19:30:00'),
(14, 13, N'Điểm thưởng tư duy tháng 9', N'Bé Trịnh Phúc Lâm được cộng thêm 50 điểm tư duy nhờ tích cực phát biểu.', 'ACADEMIC', 'REF-PTS-01', 0, '2026-09-22 17:45:00'),
(15, 14, N'Xác nhận đổi ca học thành công', N'Ca học của bé Phan Ngọc Diệp đã được chuyển sang Thứ 7 lúc 08:30.', 'SYSTEM', 'REF-SCH-01', 1, '2026-09-03 10:30:00'),
(16, 15, N'Nhắc nhở nộp bài tập đúng hạn', N'Bài tập tuần 3 của bé Hữu Phước sẽ hết hạn nộp vào 23:59 tối nay.', 'ACADEMIC', 'REF-ASM-03', 0, '2026-09-19 16:00:00'),
(17, 16, N'Bé đạt danh hiệu Học sinh Tiêu biểu', N'Chúc mừng bé Võ Khánh An đạt giải Nhất bảng đấu Mathnasium tuần 2!', 'ACADEMIC', 'REF-BDG-02', 0, '2026-09-15 18:30:00'),
(18, 17, N'Nhắc nhở bảo lưu ca học', N'Thủ tục bảo lưu học tập của bé Lâm Thiên Kim đã hoàn tất ghi nhận.', 'SYSTEM', 'REF-SYS-04', 1, '2026-09-11 14:00:00'),
(19, 18, N'Phản hồi bài tập từ giáo viên', N'Cô Mai đã chấm và để lại lời nhận xét chi tiết cho bài làm của bé Anh Quân.', 'ACADEMIC', 'REF-FDB-01', 0, '2026-09-16 20:30:00'),
(20, 19, N'Thông báo kiểm tra định kỳ lần 1', N'Lớp CPA-01 sẽ có bài kiểm tra năng lực mô hình vào Thứ 4 tuần sau.', 'ACADEMIC', 'REF-TST-01', 1, '2026-09-09 08:30:00'),
(21, 21, N'Thông báo học tập số 21', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 21 cho phụ huynh.', 'TUITION', 'REF-NOTIF-21', 1, '2026-09-16 10:00:00'),
(22, 22, N'Thông báo học tập số 22', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 22 cho phụ huynh.', 'ATTENDANCE', 'REF-NOTIF-22', 0, '2026-09-17 10:00:00'),
(23, 23, N'Thông báo học tập số 23', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 23 cho phụ huynh.', 'SYSTEM', 'REF-NOTIF-23', 1, '2026-09-18 10:00:00'),
(24, 24, N'Thông báo học tập số 24', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 24 cho phụ huynh.', 'ACADEMIC', 'REF-NOTIF-24', 0, '2026-09-19 10:00:00'),
(25, 25, N'Thông báo học tập số 25', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 25 cho phụ huynh.', 'TUITION', 'REF-NOTIF-25', 1, '2026-09-20 10:00:00'),
(26, 26, N'Thông báo học tập số 26', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 26 cho phụ huynh.', 'ATTENDANCE', 'REF-NOTIF-26', 0, '2026-09-21 10:00:00'),
(27, 27, N'Thông báo học tập số 27', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 27 cho phụ huynh.', 'SYSTEM', 'REF-NOTIF-27', 1, '2026-09-22 10:00:00'),
(28, 28, N'Thông báo học tập số 28', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 28 cho phụ huynh.', 'ACADEMIC', 'REF-NOTIF-28', 0, '2026-09-23 10:00:00'),
(29, 29, N'Thông báo học tập số 29', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 29 cho phụ huynh.', 'TUITION', 'REF-NOTIF-29', 1, '2026-09-24 10:00:00'),
(30, 30, N'Thông báo học tập số 30', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 30 cho phụ huynh.', 'ATTENDANCE', 'REF-NOTIF-30', 0, '2026-09-10 10:00:00'),
(31, 31, N'Thông báo học tập số 31', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 31 cho phụ huynh.', 'SYSTEM', 'REF-NOTIF-31', 1, '2026-09-11 10:00:00'),
(32, 32, N'Thông báo học tập số 32', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 32 cho phụ huynh.', 'ACADEMIC', 'REF-NOTIF-32', 0, '2026-09-12 10:00:00'),
(33, 1, N'Thông báo học tập số 33', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 33 cho phụ huynh.', 'TUITION', 'REF-NOTIF-33', 1, '2026-09-13 10:00:00'),
(34, 2, N'Thông báo học tập số 34', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 34 cho phụ huynh.', 'ATTENDANCE', 'REF-NOTIF-34', 0, '2026-09-14 10:00:00'),
(35, 3, N'Thông báo học tập số 35', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 35 cho phụ huynh.', 'SYSTEM', 'REF-NOTIF-35', 1, '2026-09-15 10:00:00'),
(36, 4, N'Thông báo học tập số 36', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 36 cho phụ huynh.', 'ACADEMIC', 'REF-NOTIF-36', 0, '2026-09-16 10:00:00'),
(37, 5, N'Thông báo học tập số 37', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 37 cho phụ huynh.', 'TUITION', 'REF-NOTIF-37', 1, '2026-09-17 10:00:00'),
(38, 6, N'Thông báo học tập số 38', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 38 cho phụ huynh.', 'ATTENDANCE', 'REF-NOTIF-38', 0, '2026-09-18 10:00:00'),
(39, 7, N'Thông báo học tập số 39', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 39 cho phụ huynh.', 'SYSTEM', 'REF-NOTIF-39', 1, '2026-09-19 10:00:00'),
(40, 8, N'Thông báo học tập số 40', N'Cập nhật tình hình học tập và rèn luyện môn Toán Tư Duy số 40 cho phụ huynh.', 'ACADEMIC', 'REF-NOTIF-40', 0, '2026-09-20 10:00:00');
SET IDENTITY_INSERT [dbo].[NOTIFICATION] OFF;

-- =============================================================================
-- 11. BẢNG NOTIFICATION_SETTING (40 cài đặt thông báo)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[NOTIFICATION_SETTING] ON;
INSERT INTO [dbo].[NOTIFICATION_SETTING] (id, parent_id, type, enabled) VALUES
(1, 1, 'EMAIL_NOTIFICATION', 1),
(2, 2, 'PUSH_NOTIFICATION', 1),
(3, 3, 'SMS_ALERT', 1),
(4, 4, 'REPORT_WEEKLY', 1),
(5, 5, 'EMAIL_NOTIFICATION', 0),
(6, 6, 'PUSH_NOTIFICATION', 1),
(7, 7, 'SMS_ALERT', 1),
(8, 8, 'REPORT_WEEKLY', 1),
(9, 9, 'EMAIL_NOTIFICATION', 1),
(10, 10, 'PUSH_NOTIFICATION', 0),
(11, 11, 'SMS_ALERT', 1),
(12, 12, 'REPORT_WEEKLY', 1),
(13, 13, 'EMAIL_NOTIFICATION', 1),
(14, 14, 'PUSH_NOTIFICATION', 1),
(15, 15, 'SMS_ALERT', 0),
(16, 16, 'REPORT_WEEKLY', 1),
(17, 17, 'EMAIL_NOTIFICATION', 1),
(18, 18, 'PUSH_NOTIFICATION', 1),
(19, 19, 'SMS_ALERT', 1),
(20, 20, 'REPORT_WEEKLY', 0),
(21, 21, 'EMAIL_NOTIFICATION', 1),
(22, 22, 'PUSH_NOTIFICATION', 1),
(23, 23, 'SMS_ALERT', 1),
(24, 24, 'REPORT_WEEKLY', 1),
(25, 25, 'EMAIL_NOTIFICATION', 0),
(26, 26, 'PUSH_NOTIFICATION', 1),
(27, 27, 'SMS_ALERT', 1),
(28, 28, 'REPORT_WEEKLY', 1),
(29, 29, 'EMAIL_NOTIFICATION', 1),
(30, 30, 'PUSH_NOTIFICATION', 0),
(31, 31, 'SMS_ALERT', 1),
(32, 32, 'REPORT_WEEKLY', 1),
(33, 1, 'EMAIL_NOTIFICATION', 1),
(34, 2, 'PUSH_NOTIFICATION', 1),
(35, 3, 'SMS_ALERT', 0),
(36, 4, 'REPORT_WEEKLY', 1),
(37, 5, 'EMAIL_NOTIFICATION', 1),
(38, 6, 'PUSH_NOTIFICATION', 1),
(39, 7, 'SMS_ALERT', 1),
(40, 8, 'REPORT_WEEKLY', 0);
SET IDENTITY_INSERT [dbo].[NOTIFICATION_SETTING] OFF;

-- =============================================================================
-- 12. BẢNG QUESTION (40 câu hỏi Toán Tư Duy thực tế)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[QUESTION] ON;
INSERT INTO [dbo].[QUESTION] (id, bank_id, type, content, image_url, difficulty, answer) VALUES
(1, 1, 'MULTIPLE_CHOICE', N'Khi gảy bàn tính Soroban, hạt ở hàng trên có giá trị bằng bao nhiêu? A. 1; B. 5; C. 10; D. 50', NULL, 'EASY', N'B'),
(2, 1, 'FILL_IN', N'Tính nhẩm nhanh trên bàn tính: 3 + 1 + 5 - 2 = ?', NULL, 'EASY', N'7'),
(3, 2, 'MULTIPLE_CHOICE', N'Trong công thức Anh bạn nhỏ của Soroban, bạn nhỏ của 4 là số nào? A. 1; B. 2; C. 3; D. 5', NULL, 'EASY', N'A'),
(4, 2, 'TRUE_FALSE', N'Để thực hiện phép tính +4 khi cột không đủ 4 hạt dưới, ta gảy thêm hạt 5 và bớt 1 hạt dưới.', NULL, 'MEDIUM', N'TRUE'),
(5, 3, 'FILL_IN', N'Trong công thức Anh bạn lớn của Soroban, bạn lớn của 7 là mấy để tạo thành 10?', NULL, 'MEDIUM', N'3'),
(6, 3, 'MULTIPLE_CHOICE', N'Thực hiện phép tính qua cầu 10: 8 + 5 = ? (Cộng 10 bớt bạn lớn của 5) A. 12; B. 13; C. 14; D. 15', NULL, 'MEDIUM', N'B'),
(7, 4, 'FILL_IN', N'Ảo tính Anzan: 12 + 15 - 7 + 20 = ?', NULL, 'HARD', N'40'),
(8, 4, 'MULTIPLE_CHOICE', N'Tốc độ ảo tính Anzan giúp kích thích bán cầu não nào phát triển mạnh nhất? A. Bán cầu não trái; B. Bán cầu não phải; C. Cả hai bán cầu não; D. Không xác định', NULL, 'MEDIUM', N'C'),
(9, 5, 'MULTIPLE_CHOICE', N'Trong quy ước Finger Math bàn tay phải, ngón cái xòe ra biểu thị số mấy? A. 1; B. 5; C. 9; D. 10', NULL, 'EASY', N'B'),
(10, 5, 'FILL_IN', N'Xòe ngón cái và 3 ngón tay trỏ, giữa, áp út của bàn tay phải biểu thị số mấy?', NULL, 'EASY', N'8'),
(11, 6, 'FILL_IN', N'Tính bằng Finger Math hai bàn tay: 23 + 15 = ?', NULL, 'MEDIUM', N'38'),
(12, 6, 'TRUE_FALSE', N'Trên phương pháp Finger Math, số 99 được biểu thị bằng cách nắm cả 2 bàn tay lại.', NULL, 'EASY', N'FALSE'),
(13, 7, 'MULTIPLE_CHOICE', N'Tính nhẩm phản xạ ngón tay: 47 + 22 - 18 = ? A. 51; B. 52; C. 50; D. 49', NULL, 'HARD', N'A'),
(14, 7, 'FILL_IN', N'Một người gập 1 ngón tay cái bên trái và xòe 4 ngón tay phải. Số đó là số bao nhiêu?', NULL, 'MEDIUM', N'44'),
(15, 8, 'MULTIPLE_CHOICE', N'Số liền trước của số tròn chục nhỏ nhất có 3 chữ số là số nào? A. 90; B. 99; C. 101; D. 109', NULL, 'EASY', N'B'),
(16, 8, 'FILL_IN', N'Có bao nhiêu số chẵn có 2 chữ số lớn hơn 20 và nhỏ hơn 30?', NULL, 'EASY', N'4'),
(17, 9, 'MULTIPLE_CHOICE', N'Một hình lập phương có bao nhiêu mặt, bao nhiêu đỉnh và bao nhiêu cạnh? A. 6 mặt, 8 đỉnh, 12 cạnh; B. 6 mặt, 12 đỉnh, 8 cạnh; C. 8 mặt, 6 đỉnh, 12 cạnh; D. 4 mặt, 4 đỉnh, 6 cạnh', NULL, 'MEDIUM', N'A'),
(18, 9, 'TRUE_FALSE', N'Hình thoi có 4 góc vuông được gọi là hình vuông.', NULL, 'EASY', N'TRUE'),
(19, 10, 'FILL_IN', N'Một trang trại có gà và thỏ. Đếm được 10 cái đầu và 28 cái chân. Hỏi có bao nhiêu con thỏ?', NULL, 'HARD', N'4'),
(20, 10, 'MULTIPLE_CHOICE', N'Mẹ mua 3 hộp sữa và 2 cái bánh hết 55 nghìn. Giá 1 hộp sữa hơn 1 cái bánh 5 nghìn. Giá 1 cái bánh là? A. 8 nghìn; B. 10 nghìn; C. 12 nghìn; D. 15 nghìn', NULL, 'HARD', N'A'),
(21, 11, 'FILL_IN', N'Tìm số tiếp theo trong dãy số: 2, 4, 8, 16, 32, ... ?', NULL, 'EASY', N'64'),
(22, 11, 'MULTIPLE_CHOICE', N'Tìm số còn thiếu: 1, 4, 9, 16, [ ? ], 36, 49 A. 20; B. 24; C. 25; D. 30', NULL, 'MEDIUM', N'C'),
(23, 12, 'FILL_IN', N'Tính nhanh giá trị biểu thức: 125 x 8 x 25 x 4 = ?', NULL, 'MEDIUM', N'100000'),
(24, 12, 'TRUE_FALSE', N'Tích của một số lẻ với một số chẵn bất kỳ luôn luôn là một số chẵn.', NULL, 'EASY', N'TRUE'),
(25, 13, 'MULTIPLE_CHOICE', N'Rút gọn phân số 48/64 về dạng phân số tối giản ta được: A. 6/8; B. 3/4; C. 12/16; D. 24/32', NULL, 'MEDIUM', N'B'),
(26, 13, 'FILL_IN', N'Tính giá trị của phép tính: (3/4 + 1/2) : 5/8 = ?', NULL, 'HARD', N'2'),
(27, 14, 'MULTIPLE_CHOICE', N'Nam nói: ''Số chia hết cho 2 thì chắc chắn chia hết cho 4''. Lập luận này đúng hay sai? A. Luôn đúng; B. Sai, ví dụ số 6; C. Chỉ đúng với số có 2 chữ số; D. Không thể kết luận', NULL, 'EASY', N'B'),
(28, 14, 'TRUE_FALSE', N'Nếu A lớn hơn B và B lớn hơn C thì chắc chắn A lớn hơn C (tính chất bắc cầu).', NULL, 'EASY', N'TRUE'),
(29, 15, 'MULTIPLE_CHOICE', N'Làm thế nào chia đều 7 quả táo cho 8 bạn nhỏ mà không bổ bất kỳ quả nào quá 4 phần? A. Cắt 4 quả làm đôi, 3 quả làm 4; B. Cắt mỗi quả làm 8 phần; C. Ép thành nước táo; D. Cả A và C đều hợp lý', NULL, 'HARD', N'D'),
(30, 15, 'FILL_IN', N'Nếu ngày hôm qua là ngày mai của thứ Hai, thì ngày mai là ngày hôm qua của thứ mấy?', NULL, 'HARD', N'Thứ Năm'),
(31, 16, 'MULTIPLE_CHOICE', N'Bác tài xế xe buýt chở 10 người, trạm 1 xuống 3 lên 2, trạm 2 xuống 4 lên 5. Hỏi bác tài xế mắt màu gì? A. Mắt đen; B. Màu mắt của người đang giải đố; C. Mắt nâu; D. Không biết', NULL, 'EASY', N'B'),
(32, 17, 'FILL_IN', N'Có 3 đĩa cam, mỗi đĩa có 4 quả. Viết phép tính nhân tương ứng: 3 x 4 = ?', NULL, 'EASY', N'12'),
(33, 17, 'TRUE_FALSE', N'Mô hình cụ thể (Concrete) trong CPA là để trẻ trực tiếp cầm nắm que tính, đồ vật.', NULL, 'EASY', N'TRUE'),
(34, 18, 'MULTIPLE_CHOICE', N'Mô hình thanh Bar Model: Thanh A dài hơn thanh B 12 đơn vị. Tổng 2 thanh là 40. Thanh B dài: A. 14; B. 26; C. 16; D. 28', NULL, 'MEDIUM', N'A'),
(35, 18, 'FILL_IN', N'Mô hình đoạn thẳng: Tuổi bố gấp 4 lần tuổi con, bố hơn con 30 tuổi. Hỏi con bao nhiêu tuổi?', NULL, 'MEDIUM', N'10'),
(36, 19, 'MULTIPLE_CHOICE', N'Chuyển mô hình ''Tìm hai số biết tổng là S và tỉ số là a/b'' sang công thức đại số tìm số bé: A. S : (a + b) x a; B. S : (a - b) x a; C. S x a + b; D. S : a x b', NULL, 'HARD', N'A'),
(37, 19, 'FILL_IN', N'Cho biểu thức 2x + 15 = 45. Giá trị của x là bao nhiêu?', NULL, 'EASY', N'15'),
(38, 20, 'MULTIPLE_CHOICE', N'Trong một phòng họp có 8 người. Mỗi người bắt tay với tất cả những người còn lại đúng 1 lần. Có tất cả bao nhiêu cái bắt tay? A. 28; B. 32; C. 56; D. 64', NULL, 'HARD', N'A'),
(39, 20, 'FILL_IN', N'Tìm số tự nhiên nhỏ nhất có 2 chữ số mà khi chia cho 2, 3, 5 đều dư 1:', NULL, 'MEDIUM', N'31'),
(40, 20, 'TRUE_FALSE', N'Tổng của 5 số tự nhiên liên tiếp bất kỳ luôn luôn chia hết cho 5.', NULL, 'MEDIUM', N'TRUE');
SET IDENTITY_INSERT [dbo].[QUESTION] OFF;

-- =============================================================================
-- 13. BẢNG StudentBadge (40 huy hiệu học sinh đạt được)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[StudentBadge] ON;
INSERT INTO [dbo].[StudentBadge] (id, student_id, badge_id, earned_at) VALUES
(1, 1, 4, '2026-09-02 16:30:00'),
(2, 2, 7, '2026-09-03 16:30:00'),
(3, 3, 10, '2026-09-04 16:30:00'),
(4, 4, 13, '2026-09-05 16:30:00'),
(5, 5, 16, '2026-09-06 16:30:00'),
(6, 6, 19, '2026-09-07 16:30:00'),
(7, 7, 2, '2026-09-08 16:30:00'),
(8, 8, 5, '2026-09-09 16:30:00'),
(9, 9, 8, '2026-09-10 16:30:00'),
(10, 10, 11, '2026-09-11 16:30:00'),
(11, 11, 14, '2026-09-12 16:30:00'),
(12, 12, 17, '2026-09-13 16:30:00'),
(13, 13, 20, '2026-09-14 16:30:00'),
(14, 14, 3, '2026-09-15 16:30:00'),
(15, 15, 6, '2026-09-16 16:30:00'),
(16, 16, 9, '2026-09-17 16:30:00'),
(17, 17, 12, '2026-09-18 16:30:00'),
(18, 18, 15, '2026-09-19 16:30:00'),
(19, 19, 18, '2026-09-20 16:30:00'),
(20, 20, 1, '2026-09-21 16:30:00'),
(21, 21, 4, '2026-09-22 16:30:00'),
(22, 22, 7, '2026-09-23 16:30:00'),
(23, 23, 10, '2026-09-24 16:30:00'),
(24, 24, 13, '2026-09-25 16:30:00'),
(25, 25, 16, '2026-09-01 16:30:00'),
(26, 26, 19, '2026-09-02 16:30:00'),
(27, 27, 2, '2026-09-03 16:30:00'),
(28, 28, 5, '2026-09-04 16:30:00'),
(29, 29, 8, '2026-09-05 16:30:00'),
(30, 30, 11, '2026-09-06 16:30:00'),
(31, 31, 14, '2026-09-07 16:30:00'),
(32, 32, 17, '2026-09-08 16:30:00'),
(33, 33, 20, '2026-09-09 16:30:00'),
(34, 34, 3, '2026-09-10 16:30:00'),
(35, 35, 6, '2026-09-11 16:30:00'),
(36, 36, 9, '2026-09-12 16:30:00'),
(37, 37, 12, '2026-09-13 16:30:00'),
(38, 38, 15, '2026-09-14 16:30:00'),
(39, 39, 18, '2026-09-15 16:30:00'),
(40, 40, 1, '2026-09-16 16:30:00');
SET IDENTITY_INSERT [dbo].[StudentBadge] OFF;

-- =============================================================================
-- 14. BẢNG CLASS_SCHEDULE (40 lịch học: 2 buổi / tuần / lớp)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[CLASS_SCHEDULE] ON;
INSERT INTO [dbo].[CLASS_SCHEDULE] (id, class_id, day_of_week, start_time, end_time, created_at) VALUES
(1, 1, 1, '17:30:00', '19:00:00', '2026-08-01 08:00:00'),
(2, 1, 3, '17:30:00', '19:00:00', '2026-08-01 08:00:00'),
(3, 2, 2, '18:00:00', '19:30:00', '2026-08-01 08:00:00'),
(4, 2, 4, '18:00:00', '19:30:00', '2026-08-01 08:00:00'),
(5, 3, 3, '19:00:00', '20:30:00', '2026-08-01 08:00:00'),
(6, 3, 5, '19:00:00', '20:30:00', '2026-08-01 08:00:00'),
(7, 4, 6, '08:30:00', '10:00:00', '2026-08-01 08:00:00'),
(8, 4, 0, '08:30:00', '10:00:00', '2026-08-01 08:00:00'),
(9, 5, 1, '10:15:00', '11:45:00', '2026-08-01 08:00:00'),
(10, 5, 3, '10:15:00', '11:45:00', '2026-08-01 08:00:00'),
(11, 6, 2, '14:30:00', '16:00:00', '2026-08-01 08:00:00'),
(12, 6, 4, '14:30:00', '16:00:00', '2026-08-01 08:00:00'),
(13, 7, 3, '17:30:00', '19:00:00', '2026-08-01 08:00:00'),
(14, 7, 5, '17:30:00', '19:00:00', '2026-08-01 08:00:00'),
(15, 8, 6, '18:00:00', '19:30:00', '2026-08-01 08:00:00'),
(16, 8, 0, '18:00:00', '19:30:00', '2026-08-01 08:00:00'),
(17, 9, 1, '19:00:00', '20:30:00', '2026-08-01 08:00:00'),
(18, 9, 3, '19:00:00', '20:30:00', '2026-08-01 08:00:00'),
(19, 10, 2, '08:30:00', '10:00:00', '2026-08-01 08:00:00'),
(20, 10, 4, '08:30:00', '10:00:00', '2026-08-01 08:00:00'),
(21, 11, 3, '10:15:00', '11:45:00', '2026-08-01 08:00:00'),
(22, 11, 5, '10:15:00', '11:45:00', '2026-08-01 08:00:00'),
(23, 12, 6, '14:30:00', '16:00:00', '2026-08-01 08:00:00'),
(24, 12, 0, '14:30:00', '16:00:00', '2026-08-01 08:00:00'),
(25, 13, 1, '17:30:00', '19:00:00', '2026-08-01 08:00:00'),
(26, 13, 3, '17:30:00', '19:00:00', '2026-08-01 08:00:00'),
(27, 14, 2, '18:00:00', '19:30:00', '2026-08-01 08:00:00'),
(28, 14, 4, '18:00:00', '19:30:00', '2026-08-01 08:00:00'),
(29, 15, 3, '19:00:00', '20:30:00', '2026-08-01 08:00:00'),
(30, 15, 5, '19:00:00', '20:30:00', '2026-08-01 08:00:00'),
(31, 16, 6, '08:30:00', '10:00:00', '2026-08-01 08:00:00'),
(32, 16, 0, '08:30:00', '10:00:00', '2026-08-01 08:00:00'),
(33, 17, 1, '10:15:00', '11:45:00', '2026-08-01 08:00:00'),
(34, 17, 3, '10:15:00', '11:45:00', '2026-08-01 08:00:00'),
(35, 18, 2, '14:30:00', '16:00:00', '2026-08-01 08:00:00'),
(36, 18, 4, '14:30:00', '16:00:00', '2026-08-01 08:00:00'),
(37, 19, 3, '17:30:00', '19:00:00', '2026-08-01 08:00:00'),
(38, 19, 5, '17:30:00', '19:00:00', '2026-08-01 08:00:00'),
(39, 20, 6, '18:00:00', '19:30:00', '2026-08-01 08:00:00'),
(40, 20, 0, '18:00:00', '19:30:00', '2026-08-01 08:00:00');
SET IDENTITY_INSERT [dbo].[CLASS_SCHEDULE] OFF;

-- =============================================================================
-- 15. BẢNG CLASS_STUDENT (40 lượt xếp lớp: 2 học sinh / lớp)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[CLASS_STUDENT] ON;
INSERT INTO [dbo].[CLASS_STUDENT] (id, class_id, student_id, joined_at, status) VALUES
(1, 1, 1, '2026-08-28 09:00:00', 'ACTIVE'),
(2, 1, 2, '2026-08-28 09:00:00', 'ACTIVE'),
(3, 2, 3, '2026-08-28 09:00:00', 'ACTIVE'),
(4, 2, 4, '2026-08-28 09:00:00', 'ACTIVE'),
(5, 3, 5, '2026-08-28 09:00:00', 'ACTIVE'),
(6, 3, 6, '2026-08-28 09:00:00', 'ACTIVE'),
(7, 4, 7, '2026-08-28 09:00:00', 'ACTIVE'),
(8, 4, 8, '2026-08-28 09:00:00', 'ACTIVE'),
(9, 5, 9, '2026-08-28 09:00:00', 'ACTIVE'),
(10, 5, 10, '2026-08-28 09:00:00', 'ACTIVE'),
(11, 6, 11, '2026-08-28 09:00:00', 'ACTIVE'),
(12, 6, 12, '2026-08-28 09:00:00', 'ACTIVE'),
(13, 7, 13, '2026-08-28 09:00:00', 'ACTIVE'),
(14, 7, 14, '2026-08-28 09:00:00', 'ACTIVE'),
(15, 8, 15, '2026-08-28 09:00:00', 'ACTIVE'),
(16, 8, 16, '2026-08-28 09:00:00', 'ACTIVE'),
(17, 9, 17, '2026-08-28 09:00:00', 'ACTIVE'),
(18, 9, 18, '2026-08-28 09:00:00', 'ACTIVE'),
(19, 10, 19, '2026-08-28 09:00:00', 'ACTIVE'),
(20, 10, 20, '2026-08-28 09:00:00', 'ACTIVE'),
(21, 11, 21, '2026-08-28 09:00:00', 'ACTIVE'),
(22, 11, 22, '2026-08-28 09:00:00', 'ACTIVE'),
(23, 12, 23, '2026-08-28 09:00:00', 'ACTIVE'),
(24, 12, 24, '2026-08-28 09:00:00', 'ACTIVE'),
(25, 13, 25, '2026-08-28 09:00:00', 'ACTIVE'),
(26, 13, 26, '2026-08-28 09:00:00', 'ACTIVE'),
(27, 14, 27, '2026-08-28 09:00:00', 'ACTIVE'),
(28, 14, 28, '2026-08-28 09:00:00', 'ACTIVE'),
(29, 15, 29, '2026-08-28 09:00:00', 'ACTIVE'),
(30, 15, 30, '2026-08-28 09:00:00', 'ACTIVE'),
(31, 16, 31, '2026-08-28 09:00:00', 'ACTIVE'),
(32, 16, 32, '2026-08-28 09:00:00', 'ACTIVE'),
(33, 17, 33, '2026-08-28 09:00:00', 'ACTIVE'),
(34, 17, 34, '2026-08-28 09:00:00', 'ACTIVE'),
(35, 18, 35, '2026-08-28 09:00:00', 'ACTIVE'),
(36, 18, 36, '2026-08-28 09:00:00', 'ACTIVE'),
(37, 19, 37, '2026-08-28 09:00:00', 'ACTIVE'),
(38, 19, 38, '2026-08-28 09:00:00', 'ACTIVE'),
(39, 20, 39, '2026-08-28 09:00:00', 'ACTIVE'),
(40, 20, 40, '2026-08-28 09:00:00', 'ACTIVE');
SET IDENTITY_INSERT [dbo].[CLASS_STUDENT] OFF;

-- =============================================================================
-- 16. BẢNG LESSON (40 buổi học)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[LESSON] ON;
INSERT INTO [dbo].[LESSON] (id, class_id, lesson_date, start_time, end_time, status, is_importan) VALUES
(1, 1, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(2, 1, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(3, 2, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(4, 2, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(5, 3, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(6, 3, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(7, 4, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(8, 4, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(9, 5, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(10, 5, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(11, 6, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(12, 6, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(13, 7, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(14, 7, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(15, 8, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(16, 8, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(17, 9, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(18, 9, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(19, 10, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(20, 10, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(21, 11, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(22, 11, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(23, 12, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(24, 12, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(25, 13, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(26, 13, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(27, 14, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(28, 14, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(29, 15, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(30, 15, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(31, 16, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(32, 16, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(33, 17, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(34, 17, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(35, 18, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(36, 18, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(37, 19, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(38, 19, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(39, 20, '2026-09-07 00:00:00', '2026-09-07 17:30:00', '2026-09-07 19:00:00', 'COMPLETED', 1),
(40, 20, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0);
SET IDENTITY_INSERT [dbo].[LESSON] OFF;

-- =============================================================================
-- 17. BẢNG ASSIGNMENT (25 bài tập về nhà và kiểm tra)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[ASSIGNMENT] ON;
INSERT INTO [dbo].[ASSIGNMENT] (id, class_id, stage_id, title, start_time, deadline, time_limit_minutes, max_attempt, status) VALUES
(1, 1, 'STAGE-SRB-01', N'BTVN Tuần 1: Luyện gảy hạt 1 đến 9 trên bàn tính Soroban', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 30, 2, 'CLOSED'),
(2, 1, 'STAGE-SRB-02', N'BTVN Tuần 2: Công thức Anh bạn nhỏ +4 và +3', '2026-09-15 08:00:00', '2026-09-20 23:59:00', 30, 2, 'CLOSED'),
(3, 2, 'STAGE-SRB-01', N'BTVN Tuần 1: Nhận diện cấu tạo bàn tính Soroban', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 25, 2, 'CLOSED'),
(4, 3, 'STAGE-SRB-03', N'Thử thách Anzan: Tính nhẩm 5 dãy số liên tiếp', '2026-09-12 08:00:00', '2026-09-18 23:59:00', 20, 1, 'CLOSED'),
(5, 4, 'STAGE-SRB-04', N'Kiểm tra định kỳ: Nhân 2 chữ số với 1 chữ số', '2026-09-20 08:00:00', '2026-09-25 23:59:00', 45, 1, 'OPEN'),
(6, 5, 'STAGE-FGM-01', N'BTVN Finger Math: Quy ước ngón cái và ngón trỏ', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 20, 3, 'CLOSED'),
(7, 6, 'STAGE-FGM-02', N'BTVN Finger Math: Phép cộng không nhớ trong phạm vi 50', '2026-09-15 08:00:00', '2026-09-20 23:59:00', 25, 2, 'CLOSED'),
(8, 7, 'STAGE-FGM-03', N'Thử thách ngón tay: Phản xạ nhanh 20 câu tính nhẩm', '2026-09-18 08:00:00', '2026-09-23 23:59:00', 15, 1, 'CLOSED'),
(9, 8, 'STAGE-MTN-01', N'Phiếu bài tập Mathnasium A1: Cấu trúc số chẵn lẻ', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 30, 2, 'CLOSED'),
(10, 9, 'STAGE-MTN-02', N'Phiếu bài tập Mathnasium B2: Nhận biết hình khối không gian', '2026-09-15 08:00:00', '2026-09-20 23:59:00', 35, 2, 'CLOSED'),
(11, 10, 'STAGE-MTN-03', N'Bài toán có lời văn: Giải bài toán gà và thỏ', '2026-09-18 08:00:00', '2026-09-24 23:59:00', 40, 1, 'OPEN'),
(12, 11, 'STAGE-KMN-01', N'Phiếu tự học Kumon 01: Quy luật dãy số tăng đều', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 20, 2, 'CLOSED'),
(13, 12, 'STAGE-KMN-02', N'Phiếu tự học Kumon 02: Tốc độ cộng trừ có nhớ', '2026-09-15 08:00:00', '2026-09-20 23:59:00', 25, 2, 'CLOSED'),
(14, 13, 'STAGE-KMN-03', N'Phiếu tự học Kumon 03: Rút gọn phân số tối giản', '2026-09-20 08:00:00', '2026-09-26 23:59:00', 30, 1, 'OPEN'),
(15, 14, 'STAGE-IMT-01', N'Tư duy phản biện: Nhận diện lập luận đúng và ngụy biện', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 30, 1, 'CLOSED'),
(16, 15, 'STAGE-IMT-02', N'Phương pháp Socrates: Tìm 3 cách giải cho 1 bài toán', '2026-09-16 08:00:00', '2026-09-21 23:59:00', 40, 1, 'CLOSED'),
(17, 16, 'STAGE-IMT-03', N'Câu đố logic: Nghịch lý thời gian và ngày trong tuần', '2026-09-22 08:00:00', '2026-09-28 23:59:00', 30, 1, 'OPEN'),
(18, 17, 'STAGE-CPA-01', N'CPA Giai đoạn 1: Đếm và nhóm vật thể thực hành', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 25, 2, 'CLOSED'),
(19, 18, 'STAGE-CPA-02', N'CPA Giai đoạn 2: Vẽ sơ đồ thanh Bar Model bài toán Tổng - Tỉ', '2026-09-16 08:00:00', '2026-09-21 23:59:00', 35, 2, 'CLOSED'),
(20, 19, 'STAGE-CPA-03', N'CPA Giai đoạn 3: Lập phương trình đại số từ mô hình thanh', '2026-09-22 08:00:00', '2026-09-28 23:59:00', 40, 1, 'OPEN'),
(21, 20, 'STAGE-MTN-04', N'Hình học không gian: Thử thách cắt ghép và đối xứng', '2026-09-18 08:00:00', '2026-09-24 23:59:00', 30, 2, 'CLOSED'),
(22, 1, 'STAGE-SRB-05', N'Ôn tập giữa kỳ: Tổng hợp các công thức Anh bạn nhỏ', '2026-09-25 08:00:00', '2026-09-30 23:59:00', 45, 1, 'OPEN'),
(23, 6, 'STAGE-FGM-04', N'Kiểm tra định kỳ: Tính nhẩm 50 phép tính liên tục', '2026-09-25 08:00:00', '2026-09-30 23:59:00', 30, 1, 'OPEN'),
(24, 8, 'STAGE-MTN-05', N'Đố vui toán học: Logic bắt tay và số học vui', '2026-09-26 08:00:00', '2026-10-02 23:59:00', 25, 2, 'OPEN'),
(25, 18, 'STAGE-CPA-04', N'Bar Model nâng cao: Bài toán Hiệu - Tỉ kết hợp chuyển đổi', '2026-09-26 08:00:00', '2026-10-02 23:59:00', 40, 1, 'OPEN');
SET IDENTITY_INSERT [dbo].[ASSIGNMENT] OFF;

-- =============================================================================
-- 18. BẢNG INVOICE (40 hóa đơn học phí tương ứng 40 học sinh)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[INVOICE] ON;
INSERT INTO [dbo].[INVOICE] (id, parent_id, student_id, class_id, amount, status, payment_method, transaction_id, created_at, txn_ref) VALUES
(1, 1, 1, 1, 2400000, 'PAID', 'VNPAY', 'VNPAY202609010001', '2026-08-28 10:00:00', 'SEED-INV-0001'),
(2, 1, 2, 1, 2400000, 'PAID', 'VNPAY', 'VNPAY202609010002', '2026-08-28 10:00:00', 'SEED-INV-0002'),
(3, 2, 3, 2, 2400000, 'PAID', 'VNPAY', 'VNPAY202609010003', '2026-08-28 10:00:00', 'SEED-INV-0003'),
(4, 3, 4, 2, 2400000, 'PAID', 'VNPAY', 'VNPAY202609010004', '2026-08-28 10:00:00', 'SEED-INV-0004'),
(5, 4, 5, 3, 2800000, 'PAID', 'VNPAY', 'VNPAY202609010005', '2026-08-28 10:00:00', 'SEED-INV-0005'),
(6, 5, 6, 3, 2800000, 'PAID', 'VNPAY', 'VNPAY202609010006', '2026-08-28 10:00:00', 'SEED-INV-0006'),
(7, 6, 7, 4, 3200000, 'PAID', 'VNPAY', 'VNPAY202609010007', '2026-08-28 10:00:00', 'SEED-INV-0007'),
(8, 7, 8, 4, 3200000, 'PAID', 'VNPAY', 'VNPAY202609010008', '2026-08-28 10:00:00', 'SEED-INV-0008'),
(9, 8, 9, 5, 1800000, 'PAID', 'VNPAY', 'VNPAY202609010009', '2026-08-28 10:00:00', 'SEED-INV-0009'),
(10, 8, 10, 5, 1800000, 'PAID', 'VNPAY', 'VNPAY202609010010', '2026-08-28 10:00:00', 'SEED-INV-0010'),
(11, 9, 11, 6, 2200000, 'PAID', 'VNPAY', 'VNPAY202609010011', '2026-08-28 10:00:00', 'SEED-INV-0011'),
(12, 10, 12, 6, 2200000, 'PAID', 'VNPAY', 'VNPAY202609010012', '2026-08-28 10:00:00', 'SEED-INV-0012'),
(13, 11, 13, 7, 2600000, 'PAID', 'VNPAY', 'VNPAY202609010013', '2026-08-28 10:00:00', 'SEED-INV-0013'),
(14, 12, 14, 7, 2600000, 'PAID', 'VNPAY', 'VNPAY202609010014', '2026-08-28 10:00:00', 'SEED-INV-0014'),
(15, 13, 15, 8, 3000000, 'PAID', 'VNPAY', 'VNPAY202609010015', '2026-08-28 10:00:00', 'SEED-INV-0015'),
(16, 14, 16, 8, 3000000, 'PAID', 'VNPAY', 'VNPAY202609010016', '2026-08-28 10:00:00', 'SEED-INV-0016'),
(17, 15, 17, 9, 3400000, 'PAID', 'VNPAY', 'VNPAY202609010017', '2026-08-28 10:00:00', 'SEED-INV-0017'),
(18, 16, 18, 9, 3400000, 'PAID', 'VNPAY', 'VNPAY202609010018', '2026-08-28 10:00:00', 'SEED-INV-0018'),
(19, 17, 19, 10, 3800000, 'PAID', 'VNPAY', 'VNPAY202609010019', '2026-08-28 10:00:00', 'SEED-INV-0019'),
(20, 18, 20, 10, 3800000, 'PAID', 'VNPAY', 'VNPAY202609010020', '2026-08-28 10:00:00', 'SEED-INV-0020'),
(21, 19, 21, 11, 2000000, 'PAID', 'VNPAY', 'VNPAY202609010021', '2026-08-28 10:00:00', 'SEED-INV-0021'),
(22, 20, 22, 11, 2000000, 'PAID', 'VNPAY', 'VNPAY202609010022', '2026-08-28 10:00:00', 'SEED-INV-0022'),
(23, 21, 23, 12, 2400000, 'PAID', 'VNPAY', 'VNPAY202609010023', '2026-08-28 10:00:00', 'SEED-INV-0023'),
(24, 22, 24, 12, 2400000, 'PAID', 'VNPAY', 'VNPAY202609010024', '2026-08-28 10:00:00', 'SEED-INV-0024'),
(25, 23, 25, 13, 2800000, 'PAID', 'VNPAY', 'VNPAY202609010025', '2026-08-28 10:00:00', 'SEED-INV-0025'),
(26, 24, 26, 13, 2800000, 'PAID', 'VNPAY', 'VNPAY202609010026', '2026-08-28 10:00:00', 'SEED-INV-0026'),
(27, 25, 27, 14, 2600000, 'PAID', 'VNPAY', 'VNPAY202609010027', '2026-08-28 10:00:00', 'SEED-INV-0027'),
(28, 26, 28, 14, 2600000, 'PAID', 'VNPAY', 'VNPAY202609010028', '2026-08-28 10:00:00', 'SEED-INV-0028'),
(29, 27, 29, 15, 3100000, 'PAID', 'VNPAY', 'VNPAY202609010029', '2026-08-28 10:00:00', 'SEED-INV-0029'),
(30, 28, 30, 15, 3100000, 'PAID', 'VNPAY', 'VNPAY202609010030', '2026-08-28 10:00:00', 'SEED-INV-0030'),
(31, 29, 31, 16, 3600000, 'PAID', 'VNPAY', 'VNPAY202609010031', '2026-08-28 10:00:00', 'SEED-INV-0031'),
(32, 30, 32, 16, 3600000, 'PAID', 'VNPAY', 'VNPAY202609010032', '2026-08-28 10:00:00', 'SEED-INV-0032'),
(33, 31, 33, 17, 2200000, 'PENDING', 'CASH', NULL, '2026-08-28 10:00:00', 'SEED-INV-0033'),
(34, 32, 34, 17, 2200000, 'PENDING', 'CASH', NULL, '2026-08-28 10:00:00', 'SEED-INV-0034'),
(35, 2, 35, 18, 2600000, 'PENDING', 'CASH', NULL, '2026-08-28 10:00:00', 'SEED-INV-0035'),
(36, 3, 36, 18, 2600000, 'PENDING', 'CASH', NULL, '2026-08-28 10:00:00', 'SEED-INV-0036'),
(37, 5, 37, 19, 3000000, 'PENDING', 'CASH', NULL, '2026-08-28 10:00:00', 'SEED-INV-0037'),
(38, 7, 38, 19, 3000000, 'CANCELLED', NULL, NULL, '2026-08-28 10:00:00', 'SEED-INV-0038'),
(39, 11, 39, 20, 3400000, 'CANCELLED', NULL, NULL, '2026-08-28 10:00:00', 'SEED-INV-0039'),
(40, 15, 40, 20, 3400000, 'CANCELLED', NULL, NULL, '2026-08-28 10:00:00', 'SEED-INV-0040');
SET IDENTITY_INSERT [dbo].[INVOICE] OFF;

-- =============================================================================
-- 19. BẢNG ATTENDANCE (40 lượt điểm danh buổi học)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[ATTENDANCE] ON;
INSERT INTO [dbo].[ATTENDANCE] (id, lesson_id, student_id, status, teacher_notes) VALUES
(1, 1, 1, 'PRESENT', N'Bé tập trung tốt, tính nhẩm nhanh và chủ động phát biểu'),
(2, 1, 2, 'PRESENT', N'Thao tác gảy hạt Soroban chính xác, phản xạ tốt'),
(3, 3, 3, 'PRESENT', N'Bé đến muộn 10 phút do trời mưa, sau đó hòa nhập tốt với lớp'),
(4, 3, 4, 'PRESENT', N'Bé nắm vững quy tắc Finger Math ngón tay cái'),
(5, 5, 5, 'ABSENT', N'Vắng mặt có phép (gia đình có đơn xin nghỉ)'),
(6, 5, 6, 'PRESENT', N'Bé giải được câu đố tư duy khó đầu tiên trong lớp'),
(7, 7, 7, 'PRESENT', N'Tiếp thu bài nhanh, giúp đỡ bạn cùng bàn'),
(8, 7, 8, 'LATE', N'Đến muộn 15 phút do kẹt xe giờ cao điểm'),
(9, 9, 9, 'PRESENT', N'Tích cực giơ tay xây dựng bài học'),
(10, 9, 10, 'PRESENT', N'Làm đúng toàn bộ bài tập luyện nhanh tại lớp'),
(11, 11, 11, 'PRESENT', N'Bé tập trung tốt, tính nhẩm nhanh và chủ động phát biểu'),
(12, 11, 12, 'PRESENT', N'Thao tác gảy hạt Soroban chính xác, phản xạ tốt'),
(13, 13, 13, 'PRESENT', N'Bé đến muộn 10 phút do trời mưa, sau đó hòa nhập tốt với lớp'),
(14, 13, 14, 'PRESENT', N'Bé nắm vững quy tắc Finger Math ngón tay cái'),
(15, 15, 15, 'PRESENT', N'Cần rèn luyện thêm thao tác gảy hạt 5'),
(16, 15, 16, 'PRESENT', N'Bé giải được câu đố tư duy khó đầu tiên trong lớp'),
(17, 17, 17, 'PRESENT', N'Tiếp thu bài nhanh, giúp đỡ bạn cùng bàn'),
(18, 17, 18, 'PRESENT', N'Vắng mặt có phép do bị ốm'),
(19, 19, 19, 'PRESENT', N'Tích cực giơ tay xây dựng bài học'),
(20, 19, 20, 'ABSENT', N'Vắng mặt có phép (gia đình có đơn xin nghỉ)'),
(21, 21, 21, 'PRESENT', N'Bé tập trung tốt, tính nhẩm nhanh và chủ động phát biểu'),
(22, 21, 22, 'PRESENT', N'Thao tác gảy hạt Soroban chính xác, phản xạ tốt'),
(23, 23, 23, 'PRESENT', N'Bé đến muộn 10 phút do trời mưa, sau đó hòa nhập tốt với lớp'),
(24, 23, 24, 'PRESENT', N'Bé nắm vững quy tắc Finger Math ngón tay cái'),
(25, 25, 25, 'PRESENT', N'Cần rèn luyện thêm thao tác gảy hạt 5'),
(26, 25, 26, 'PRESENT', N'Bé giải được câu đố tư duy khó đầu tiên trong lớp'),
(27, 27, 27, 'PRESENT', N'Tiếp thu bài nhanh, giúp đỡ bạn cùng bàn'),
(28, 27, 28, 'LATE', N'Đến muộn 15 phút do kẹt xe giờ cao điểm'),
(29, 29, 29, 'PRESENT', N'Tích cực giơ tay xây dựng bài học'),
(30, 29, 30, 'PRESENT', N'Làm đúng toàn bộ bài tập luyện nhanh tại lớp'),
(31, 31, 31, 'PRESENT', N'Bé tập trung tốt, tính nhẩm nhanh và chủ động phát biểu'),
(32, 31, 32, 'PRESENT', N'Thao tác gảy hạt Soroban chính xác, phản xạ tốt'),
(33, 33, 33, 'PRESENT', N'Bé đến muộn 10 phút do trời mưa, sau đó hòa nhập tốt với lớp'),
(34, 33, 34, 'PRESENT', N'Bé nắm vững quy tắc Finger Math ngón tay cái'),
(35, 35, 35, 'PRESENT', N'Cần rèn luyện thêm thao tác gảy hạt 5'),
(36, 35, 36, 'PRESENT', N'Bé giải được câu đố tư duy khó đầu tiên trong lớp'),
(37, 37, 37, 'PRESENT', N'Tiếp thu bài nhanh, giúp đỡ bạn cùng bàn'),
(38, 37, 38, 'PRESENT', N'Vắng mặt có phép do bị ốm'),
(39, 39, 39, 'PRESENT', N'Tích cực giơ tay xây dựng bài học'),
(40, 39, 40, 'PRESENT', N'Làm đúng toàn bộ bài tập luyện nhanh tại lớp');
SET IDENTITY_INSERT [dbo].[ATTENDANCE] OFF;

-- =============================================================================
-- 20. BẢNG LEAVE_REQUEST (20 đơn xin nghỉ học)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[LEAVE_REQUEST] ON;
INSERT INTO [dbo].[LEAVE_REQUEST] (id, lesson_id, student_id, reason, status, created_at) VALUES
(1, 2, 2, N'Bé bị sốt virus, bác sĩ chỉ định nghỉ ngơi tĩnh dưỡng', 'APPROVED', '2026-09-12 14:00:00'),
(2, 4, 4, N'Gia đình có việc hiếu hỷ ở quê cần về gấp', 'APPROVED', '2026-09-12 14:00:00'),
(3, 6, 6, N'Bé bị cảm cúm thời tiết, ho nhiều nên xin nghỉ để tránh lây các bạn', 'PENDING', '2026-09-12 14:00:00'),
(4, 8, 8, N'Trùng lịch kiểm tra học kỳ tại trường tiểu học công lập', 'APPROVED', '2026-09-12 14:00:00'),
(5, 10, 10, N'Gia đình có chuyến công tác dài ngày đưa bé đi cùng', 'REJECTED', '2026-09-12 14:00:00'),
(6, 12, 12, N'Bé tham gia cuộc thi bơi lội cấp quận', 'PENDING', '2026-09-12 14:00:00'),
(7, 14, 14, N'Bé bị đau bụng cần theo dõi tại bệnh viện', 'APPROVED', '2026-09-12 14:00:00'),
(8, 16, 16, N'Gia đình có lịch hẹn khám nha khoa định kỳ', 'APPROVED', '2026-09-12 14:00:00'),
(9, 18, 18, N'Bé bị sốt xuất huyết cần điều trị 1 tuần', 'PENDING', '2026-09-12 14:00:00'),
(10, 20, 20, N'Gia đình có việc gia đình đột xuất', 'REJECTED', '2026-09-12 14:00:00'),
(11, 22, 22, N'Bé bị sốt virus, bác sĩ chỉ định nghỉ ngơi tĩnh dưỡng', 'APPROVED', '2026-09-12 14:00:00'),
(12, 24, 24, N'Gia đình có việc hiếu hỷ ở quê cần về gấp', 'PENDING', '2026-09-12 14:00:00'),
(13, 26, 26, N'Bé bị cảm cúm thời tiết, ho nhiều nên xin nghỉ để tránh lây các bạn', 'APPROVED', '2026-09-12 14:00:00'),
(14, 28, 28, N'Trùng lịch kiểm tra học kỳ tại trường tiểu học công lập', 'APPROVED', '2026-09-12 14:00:00'),
(15, 30, 30, N'Gia đình có chuyến công tác dài ngày đưa bé đi cùng', 'REJECTED', '2026-09-12 14:00:00'),
(16, 32, 32, N'Bé tham gia cuộc thi bơi lội cấp quận', 'APPROVED', '2026-09-12 14:00:00'),
(17, 34, 34, N'Bé bị đau bụng cần theo dõi tại bệnh viện', 'APPROVED', '2026-09-12 14:00:00'),
(18, 36, 36, N'Gia đình có lịch hẹn khám nha khoa định kỳ', 'PENDING', '2026-09-12 14:00:00'),
(19, 38, 38, N'Bé bị sốt xuất huyết cần điều trị 1 tuần', 'APPROVED', '2026-09-12 14:00:00'),
(20, 40, 40, N'Gia đình có việc gia đình đột xuất', 'REJECTED', '2026-09-12 14:00:00');
SET IDENTITY_INSERT [dbo].[LEAVE_REQUEST] OFF;

-- =============================================================================
-- 21. BẢNG STUDENT_ASSIGNMENT (40 bài nộp của học sinh)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[STUDENT_ASSIGNMENT] ON;
INSERT INTO [dbo].[STUDENT_ASSIGNMENT] (id, assignment_id, student_id, score, status, teacher_feedback, completed_at) VALUES
(1, 1, 1, 8.2, 'GRADED', N'Bài làm xuất sắc, phản xạ tính nhẩm nhanh và chính xác tuyệt đối!', '2026-09-12 20:15:00'),
(2, 1, 2, 8.9, 'GRADED', N'Làm bài cẩn thận, chữ số rõ ràng, nắm vững quy tắc bàn tính.', '2026-09-12 20:15:00'),
(3, 2, 3, 9.6, 'GRADED', N'Cần chú ý kỹ câu hỏi đố tư duy logic, không vội vàng.', '2026-09-12 20:15:00'),
(4, 2, 4, 7.7, 'GRADED', N'Bé vẽ sơ đồ Bar Model rất đẹp và logic.', '2026-09-12 20:15:00'),
(5, 3, 5, 8.4, 'GRADED', N'Tốt, phát huy phản xạ ngón tay Finger Math nhanh hơn nữa nhé.', '2026-09-12 20:15:00'),
(6, 3, 6, 9.1, 'GRADED', N'Đúng hầu hết các câu, nhầm lẫn một chút ở câu tính có nhớ.', '2026-09-12 20:15:00'),
(7, 4, 7, 9.8, 'GRADED', N'Tư duy nhạy bén, tìm ra cách giải ngắn gọn rất thông minh.', '2026-09-12 20:15:00'),
(8, 4, 8, 7.9, 'GRADED', N'Bé hoàn thành bài tập trước thời hạn với độ chính xác cao.', '2026-09-12 20:15:00'),
(9, 5, 9, 8.6, 'GRADED', N'Bài làm xuất sắc, phản xạ tính nhẩm nhanh và chính xác tuyệt đối!', '2026-09-12 20:15:00'),
(10, 5, 10, 9.3, 'GRADED', N'Làm bài cẩn thận, chữ số rõ ràng, nắm vững quy tắc bàn tính.', '2026-09-12 20:15:00'),
(11, 6, 11, 10.0, 'GRADED', N'Cần chú ý kỹ câu hỏi đố tư duy logic, không vội vàng.', '2026-09-12 20:15:00'),
(12, 6, 12, 8.1, 'GRADED', N'Bé vẽ sơ đồ Bar Model rất đẹp và logic.', '2026-09-12 20:15:00'),
(13, 7, 13, 8.8, 'GRADED', N'Tốt, phát huy phản xạ ngón tay Finger Math nhanh hơn nữa nhé.', '2026-09-12 20:15:00'),
(14, 7, 14, 9.5, 'GRADED', N'Đúng hầu hết các câu, nhầm lẫn một chút ở câu tính có nhớ.', '2026-09-12 20:15:00'),
(15, 8, 15, 7.6, 'GRADED', N'Tư duy nhạy bén, tìm ra cách giải ngắn gọn rất thông minh.', '2026-09-12 20:15:00'),
(16, 8, 16, 8.3, 'GRADED', N'Bé hoàn thành bài tập trước thời hạn với độ chính xác cao.', '2026-09-12 20:15:00'),
(17, 9, 17, 9.0, 'GRADED', N'Bài làm xuất sắc, phản xạ tính nhẩm nhanh và chính xác tuyệt đối!', '2026-09-12 20:15:00'),
(18, 9, 18, 9.7, 'GRADED', N'Làm bài cẩn thận, chữ số rõ ràng, nắm vững quy tắc bàn tính.', '2026-09-12 20:15:00'),
(19, 10, 19, 7.8, 'GRADED', N'Cần chú ý kỹ câu hỏi đố tư duy logic, không vội vàng.', '2026-09-12 20:15:00'),
(20, 10, 20, 8.5, 'GRADED', N'Bé vẽ sơ đồ Bar Model rất đẹp và logic.', '2026-09-12 20:15:00'),
(21, 11, 21, 9.2, 'GRADED', N'Tốt, phát huy phản xạ ngón tay Finger Math nhanh hơn nữa nhé.', '2026-09-12 20:15:00'),
(22, 11, 22, 9.9, 'GRADED', N'Đúng hầu hết các câu, nhầm lẫn một chút ở câu tính có nhớ.', '2026-09-12 20:15:00'),
(23, 12, 23, 8.0, 'GRADED', N'Tư duy nhạy bén, tìm ra cách giải ngắn gọn rất thông minh.', '2026-09-12 20:15:00'),
(24, 12, 24, 8.7, 'GRADED', N'Bé hoàn thành bài tập trước thời hạn với độ chính xác cao.', '2026-09-12 20:15:00'),
(25, 13, 25, 9.4, 'GRADED', N'Bài làm xuất sắc, phản xạ tính nhẩm nhanh và chính xác tuyệt đối!', '2026-09-12 20:15:00'),
(26, 13, 26, 7.5, 'GRADED', N'Làm bài cẩn thận, chữ số rõ ràng, nắm vững quy tắc bàn tính.', '2026-09-12 20:15:00'),
(27, 14, 27, 8.2, 'GRADED', N'Cần chú ý kỹ câu hỏi đố tư duy logic, không vội vàng.', '2026-09-12 20:15:00'),
(28, 14, 28, 8.9, 'GRADED', N'Bé vẽ sơ đồ Bar Model rất đẹp và logic.', '2026-09-12 20:15:00'),
(29, 15, 29, 9.6, 'GRADED', N'Tốt, phát huy phản xạ ngón tay Finger Math nhanh hơn nữa nhé.', '2026-09-12 20:15:00'),
(30, 15, 30, 7.7, 'GRADED', N'Đúng hầu hết các câu, nhầm lẫn một chút ở câu tính có nhớ.', '2026-09-12 20:15:00'),
(31, 16, 31, 8.4, 'GRADED', N'Tư duy nhạy bén, tìm ra cách giải ngắn gọn rất thông minh.', '2026-09-12 20:15:00'),
(32, 16, 32, 9.1, 'GRADED', N'Bé hoàn thành bài tập trước thời hạn với độ chính xác cao.', '2026-09-12 20:15:00'),
(33, 17, 33, 9.8, 'GRADED', N'Bài làm xuất sắc, phản xạ tính nhẩm nhanh và chính xác tuyệt đối!', '2026-09-12 20:15:00'),
(34, 17, 34, 7.9, 'GRADED', N'Làm bài cẩn thận, chữ số rõ ràng, nắm vững quy tắc bàn tính.', '2026-09-12 20:15:00'),
(35, 18, 35, 8.6, 'GRADED', N'Cần chú ý kỹ câu hỏi đố tư duy logic, không vội vàng.', '2026-09-12 20:15:00'),
(36, 18, 36, 9.3, 'GRADED', N'Bé vẽ sơ đồ Bar Model rất đẹp và logic.', '2026-09-12 20:15:00'),
(37, 19, 37, 10.0, 'SUBMITTED', N'Tốt, phát huy phản xạ ngón tay Finger Math nhanh hơn nữa nhé.', '2026-09-12 20:15:00'),
(38, 19, 38, 8.1, 'SUBMITTED', N'Đúng hầu hết các câu, nhầm lẫn một chút ở câu tính có nhớ.', '2026-09-12 20:15:00'),
(39, 20, 39, 8.8, 'SUBMITTED', N'Tư duy nhạy bén, tìm ra cách giải ngắn gọn rất thông minh.', '2026-09-12 20:15:00'),
(40, 20, 40, 9.5, 'SUBMITTED', N'Bé hoàn thành bài tập trước thời hạn với độ chính xác cao.', '2026-09-12 20:15:00');
SET IDENTITY_INSERT [dbo].[STUDENT_ASSIGNMENT] OFF;

-- =============================================================================
-- 22. BẢNG QUESTION_ASSIGNMENT (40 liên kết câu hỏi vào bài tập)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[QUESTION_ASSIGNMENT] ON;
INSERT INTO [dbo].[QUESTION_ASSIGNMENT] (id, assignment_id, question_id, order_index, point_value) VALUES
(1, 1, 1, 1, 5.0),
(2, 1, 2, 2, 5.0),
(3, 2, 3, 1, 5.0),
(4, 2, 4, 2, 5.0),
(5, 3, 5, 1, 5.0),
(6, 3, 6, 2, 5.0),
(7, 4, 7, 1, 5.0),
(8, 4, 8, 2, 5.0),
(9, 5, 9, 1, 5.0),
(10, 5, 10, 2, 5.0),
(11, 6, 11, 1, 5.0),
(12, 6, 12, 2, 5.0),
(13, 7, 13, 1, 5.0),
(14, 7, 14, 2, 5.0),
(15, 8, 15, 1, 5.0),
(16, 8, 16, 2, 5.0),
(17, 9, 17, 1, 5.0),
(18, 9, 18, 2, 5.0),
(19, 10, 19, 1, 5.0),
(20, 10, 20, 2, 5.0),
(21, 11, 21, 1, 5.0),
(22, 11, 22, 2, 5.0),
(23, 12, 23, 1, 5.0),
(24, 12, 24, 2, 5.0),
(25, 13, 25, 1, 5.0),
(26, 13, 26, 2, 5.0),
(27, 14, 27, 1, 5.0),
(28, 14, 28, 2, 5.0),
(29, 15, 29, 1, 5.0),
(30, 15, 30, 2, 5.0),
(31, 16, 31, 1, 5.0),
(32, 16, 32, 2, 5.0),
(33, 17, 33, 1, 5.0),
(34, 17, 34, 2, 5.0),
(35, 18, 35, 1, 5.0),
(36, 18, 36, 2, 5.0),
(37, 19, 37, 1, 5.0),
(38, 19, 38, 2, 5.0),
(39, 20, 39, 1, 5.0),
(40, 20, 40, 2, 5.0);
SET IDENTITY_INSERT [dbo].[QUESTION_ASSIGNMENT] OFF;

    COMMIT TRANSACTION;
    PRINT N'=============================================================================';
    PRINT N'>>> SEED DATA HOÀN TẤT THÀNH CÔNG! Toàn bộ 22 bảng đã được nạp dữ liệu chuẩn.';
    PRINT N'=============================================================================';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    PRINT N'>>> LỖI XẢY RA KHI SEED DỮ LIỆU: ' + @ErrorMessage;
    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;