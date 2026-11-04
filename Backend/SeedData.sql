-- =============================================================================
-- SCRIPT SEED DATA: HỆ THỐNG QUẢN LÝ TRUNG TÂM TOÁN TƯ DUY (MATH THINKING)
-- Ngữ cảnh: Toán tư duy 4-14 tuổi (Soroban, Finger Math, Mathnasium, Kumon, I-MATH, CPA)
-- Hệ quản trị CSDL: Microsoft SQL Server
-- sqlcmd -S "(localdb)\mssqllocaldb" -d ToanTuDuyDb -i SeedData.sql -f 65001
-- Cần chạy migration AddPayrollManagement trước khi chạy script này.
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
    DELETE FROM [dbo].[PAYROLL_SUMMARY];
    DELETE FROM [dbo].[SALARY_ADJUSTMENT];
    DELETE FROM [dbo].[TEACHER_SALARY_CONFIG];
    DELETE FROM [dbo].[STUDENT_PROFILE];
    DELETE FROM [dbo].[QUESTION_BANK];
    DELETE FROM [dbo].[COURSE_LEVEL];
    DELETE FROM [dbo].[ADMIN_PROFILE];
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
-- 1. BẢNG ACCOUNT (43 dòng: 2 Admin, 9 Giáo viên, 32 Phụ huynh)
-- =============================================================================
-- Tài khoản kiểm thử hệ thống:
--   Admin chính:      User = Admin123   / Password = Demo123@   (Email: admin123@toantuduy.edu.vn)
--   Admin dự phòng:   User = 0901000000 / Password = Admin@Test123!
--   Teacher chính:    User = Teacher123 / Password = Demo123@   (Email: teacher123@toantuduy.edu.vn)
--   Teacher dự phòng: User = 0902000001 / Password = Teacher@Test123!
--   Parent:           User = 0903000001 .. 0903000032 / Password = Parent@Test123!
SET IDENTITY_INSERT [dbo].[ACCOUNT] ON;
INSERT INTO [dbo].[ACCOUNT] (id, phone, password_hash, role, refresh_token, is_active, created_at, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount) VALUES
(1, '0901000000', 'AQAAAAEAAYagAAAAEPHX7xuOKVB3dhLrVO1Yi+UtUT0LEYIAAvWHfGxn48HxdfEjYAWDlcc12ZIbsLTBAw==', 'Admin', NULL, 1, '2026-08-01 08:00:00', '0901000000', '0901000000', 'admin@toantuduy.edu.vn', 'ADMIN@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(2, '0902000001', 'AQAAAAEAAYagAAAAEBdl1Hiyk6Y2riACzj6kgiX3w2CiJEwgXhCPQopl/Ej0NQTw+EzKEP7S6b/UgUdgoA==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000001', '0902000001', 'gv.huynhnhathao@toantuduy.edu.vn', 'GV.HUYNHNHATHAO@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(3, '0902000002', 'AQAAAAEAAYagAAAAEBdl1Hiyk6Y2riACzj6kgiX3w2CiJEwgXhCPQopl/Ej0NQTw+EzKEP7S6b/UgUdgoA==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000002', '0902000002', 'gv.nguyenthimai@toantuduy.edu.vn', 'GV.NGUYENTHIMAI@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(4, '0902000003', 'AQAAAAEAAYagAAAAEBdl1Hiyk6Y2riACzj6kgiX3w2CiJEwgXhCPQopl/Ej0NQTw+EzKEP7S6b/UgUdgoA==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000003', '0902000003', 'gv.tranminhtuan@toantuduy.edu.vn', 'GV.TRANMINHTUAN@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(5, '0902000004', 'AQAAAAEAAYagAAAAEBdl1Hiyk6Y2riACzj6kgiX3w2CiJEwgXhCPQopl/Ej0NQTw+EzKEP7S6b/UgUdgoA==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000004', '0902000004', 'gv.levanphuoc@toantuduy.edu.vn', 'GV.LEVANPHUOC@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(6, '0902000005', 'AQAAAAEAAYagAAAAEBdl1Hiyk6Y2riACzj6kgiX3w2CiJEwgXhCPQopl/Ej0NQTw+EzKEP7S6b/UgUdgoA==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000005', '0902000005', 'gv.phamthithanh@toantuduy.edu.vn', 'GV.PHAMTHITHANH@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(7, '0902000006', 'AQAAAAEAAYagAAAAEBdl1Hiyk6Y2riACzj6kgiX3w2CiJEwgXhCPQopl/Ej0NQTw+EzKEP7S6b/UgUdgoA==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000006', '0902000006', 'gv.hoangquocbao@toantuduy.edu.vn', 'GV.HOANGQUOCBAO@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(8, '0902000007', 'AQAAAAEAAYagAAAAEBdl1Hiyk6Y2riACzj6kgiX3w2CiJEwgXhCPQopl/Ej0NQTw+EzKEP7S6b/UgUdgoA==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000007', '0902000007', 'gv.dothikieuoanh@toantuduy.edu.vn', 'GV.DOTHIKIEUOANH@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(9, '0902000008', 'AQAAAAEAAYagAAAAEBdl1Hiyk6Y2riACzj6kgiX3w2CiJEwgXhCPQopl/Ej0NQTw+EzKEP7S6b/UgUdgoA==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', '0902000008', '0902000008', 'gv.vuminhduc@toantuduy.edu.vn', 'GV.VUMINHDUC@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(10, '0903000001', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000001', '0903000001', 'parent01@gmail.com', 'PARENT01@GMAIL.COM', 1, 1, 0, 0, 0),
(11, '0903000002', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000002', '0903000002', 'parent02@gmail.com', 'PARENT02@GMAIL.COM', 1, 1, 0, 0, 0),
(12, '0903000003', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000003', '0903000003', 'parent03@gmail.com', 'PARENT03@GMAIL.COM', 1, 1, 0, 0, 0),
(13, '0903000004', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000004', '0903000004', 'parent04@gmail.com', 'PARENT04@GMAIL.COM', 1, 1, 0, 0, 0),
(14, '0903000005', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000005', '0903000005', 'parent05@gmail.com', 'PARENT05@GMAIL.COM', 1, 1, 0, 0, 0),
(15, '0903000006', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000006', '0903000006', 'parent06@gmail.com', 'PARENT06@GMAIL.COM', 1, 1, 0, 0, 0),
(16, '0903000007', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000007', '0903000007', 'parent07@gmail.com', 'PARENT07@GMAIL.COM', 1, 1, 0, 0, 0),
(17, '0903000008', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000008', '0903000008', 'parent08@gmail.com', 'PARENT08@GMAIL.COM', 1, 1, 0, 0, 0),
(18, '0903000009', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000009', '0903000009', 'parent09@gmail.com', 'PARENT09@GMAIL.COM', 1, 1, 0, 0, 0),
(19, '0903000010', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000010', '0903000010', 'parent10@gmail.com', 'PARENT10@GMAIL.COM', 1, 1, 0, 0, 0),
(20, '0903000011', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000011', '0903000011', 'parent11@gmail.com', 'PARENT11@GMAIL.COM', 1, 1, 0, 0, 0),
(21, '0903000012', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000012', '0903000012', 'parent12@gmail.com', 'PARENT12@GMAIL.COM', 1, 1, 0, 0, 0),
(22, '0903000013', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000013', '0903000013', 'parent13@gmail.com', 'PARENT13@GMAIL.COM', 1, 1, 0, 0, 0),
(23, '0903000014', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000014', '0903000014', 'parent14@gmail.com', 'PARENT14@GMAIL.COM', 1, 1, 0, 0, 0),
(24, '0903000015', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000015', '0903000015', 'parent15@gmail.com', 'PARENT15@GMAIL.COM', 1, 1, 0, 0, 0),
(25, '0903000016', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000016', '0903000016', 'parent16@gmail.com', 'PARENT16@GMAIL.COM', 1, 1, 0, 0, 0),
(26, '0903000017', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000017', '0903000017', 'parent17@gmail.com', 'PARENT17@GMAIL.COM', 1, 1, 0, 0, 0),
(27, '0903000018', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000018', '0903000018', 'parent18@gmail.com', 'PARENT18@GMAIL.COM', 1, 1, 0, 0, 0),
(28, '0903000019', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000019', '0903000019', 'parent19@gmail.com', 'PARENT19@GMAIL.COM', 1, 1, 0, 0, 0),
(29, '0903000020', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000020', '0903000020', 'parent20@gmail.com', 'PARENT20@GMAIL.COM', 1, 1, 0, 0, 0),
(30, '0903000021', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000021', '0903000021', 'parent21@gmail.com', 'PARENT21@GMAIL.COM', 1, 1, 0, 0, 0),
(31, '0903000022', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000022', '0903000022', 'parent22@gmail.com', 'PARENT22@GMAIL.COM', 1, 1, 0, 0, 0),
(32, '0903000023', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000023', '0903000023', 'parent23@gmail.com', 'PARENT23@GMAIL.COM', 1, 1, 0, 0, 0),
(33, '0903000024', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000024', '0903000024', 'parent24@gmail.com', 'PARENT24@GMAIL.COM', 1, 1, 0, 0, 0),
(34, '0903000025', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000025', '0903000025', 'parent25@gmail.com', 'PARENT25@GMAIL.COM', 1, 1, 0, 0, 0),
(35, '0903000026', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000026', '0903000026', 'parent26@gmail.com', 'PARENT26@GMAIL.COM', 1, 1, 0, 0, 0),
(36, '0903000027', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000027', '0903000027', 'parent27@gmail.com', 'PARENT27@GMAIL.COM', 1, 1, 0, 0, 0),
(37, '0903000028', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000028', '0903000028', 'parent28@gmail.com', 'PARENT28@GMAIL.COM', 1, 1, 0, 0, 0),
(38, '0903000029', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000029', '0903000029', 'parent29@gmail.com', 'PARENT29@GMAIL.COM', 1, 1, 0, 0, 0),
(39, '0903000030', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000030', '0903000030', 'parent30@gmail.com', 'PARENT30@GMAIL.COM', 1, 1, 0, 0, 0),
(40, '0903000031', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000031', '0903000031', 'parent31@gmail.com', 'PARENT31@GMAIL.COM', 1, 1, 0, 0, 0),
(41, '0903000032', 'AQAAAAEAAYagAAAAEC6HKD2QDvhS8m5c9Duz4NOU58WkyUN9bmGfcOroUwKEPRzGTfq/iK0FpwtJKstQ7A==', 'Parent', NULL, 1, '2026-08-01 08:00:00', '0903000032', '0903000032', 'parent32@gmail.com', 'PARENT32@GMAIL.COM', 1, 1, 0, 0, 0),
(42, 'Admin123', 'AQAAAAEAAYagAAAAEA+AWZjR0IhBuYCPRJRirThOiEuVVXHavJYTz6EKYObooCUQg1QFd6ljLShDFl9ciQ==', 'Admin', NULL, 1, '2026-08-01 08:00:00', 'Admin123', 'ADMIN123', 'admin123@toantuduy.edu.vn', 'ADMIN123@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0),
(43, 'Teacher123', 'AQAAAAEAAYagAAAAEA+AWZjR0IhBuYCPRJRirThOiEuVVXHavJYTz6EKYObooCUQg1QFd6ljLShDFl9ciQ==', 'Teacher', NULL, 1, '2026-08-01 08:00:00', 'Teacher123', 'TEACHER123', 'teacher123@toantuduy.edu.vn', 'TEACHER123@TOANTUDUY.EDU.VN', 1, 1, 0, 0, 0);
SET IDENTITY_INSERT [dbo].[ACCOUNT] OFF;

INSERT INTO [dbo].[ADMIN_PROFILE] (account_id, full_name, avatar, created_at)
SELECT id, 
       CASE 
           WHEN UserName = 'Admin123' THEN N'Quản trị viên Demo'
           ELSE N'Quản trị viên Hệ thống'
       END, 
       NULL, 
       created_at
FROM [dbo].[ACCOUNT]
WHERE role = 'Admin';

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
-- 4. BẢNG TEACHER_PROFILE (9 giáo viên chuyên môn cao)
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
(8, 9, N'Vũ Minh Đức', 'gv.vuminhduc@toantuduy.edu.vn', N'Số học & Kỹ năng tính nhanh', '2026-08-01 08:00:00'),
(9, 43, N'Giáo viên Demo', 'teacher123@toantuduy.edu.vn', N'Toán tư duy & Logic giải quyết vấn đề', '2026-08-01 08:00:00');
SET IDENTITY_INSERT [dbo].[TEACHER_PROFILE] OFF;

-- =============================================================================
-- 4.1. BẢNG TEACHER_SALARY_CONFIG (đơn giá theo buổi)
-- =============================================================================
INSERT INTO [dbo].[TEACHER_SALARY_CONFIG] (teacher_id, base_rate_per_session) VALUES
(1, 350000.00),
(2, 330000.00),
(3, 380000.00),
(4, 340000.00),
(5, 400000.00),
(6, 370000.00),
(7, 320000.00),
(8, 300000.00);

-- =============================================================================
-- 5. BẢNG PARENT_PROFILE (32 phụ huynh)
-- =============================================================================
-- PIN để NULL vì dịch vụ yêu cầu giá trị hash từ ASP.NET PasswordHasher, không phải PIN thô.
SET IDENTITY_INSERT [dbo].[PARENT_PROFILE] ON;
INSERT INTO [dbo].[PARENT_PROFILE] (id, account_id, full_name, pin, avatar, created_at) VALUES
(1, 10, N'Trần Quang Huy', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/1.jpg', '2026-08-01 08:00:00'),
(2, 11, N'Nguyễn Thu Trang', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/2.jpg', '2026-08-01 08:00:00'),
(3, 12, N'Lê Hoàng Nam', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/3.jpg', '2026-08-01 08:00:00'),
(4, 13, N'Phạm Mỹ Duyên', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/4.jpg', '2026-08-01 08:00:00'),
(5, 14, N'Vũ Đình Trọng', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/5.jpg', '2026-08-01 08:00:00'),
(6, 15, N'Hoàng Lan Anh', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/6.jpg', '2026-08-01 08:00:00'),
(7, 16, N'Đỗ Quốc Hùng', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/7.jpg', '2026-08-01 08:00:00'),
(8, 17, N'Bùi Phương Thảo', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/8.jpg', '2026-08-01 08:00:00'),
(9, 18, N'Ngô Tiến Đạt', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/9.jpg', '2026-08-01 08:00:00'),
(10, 19, N'Đặng Kim Ngân', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/10.jpg', '2026-08-01 08:00:00'),
(11, 20, N'Dương Gia Bảo', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/11.jpg', '2026-08-01 08:00:00'),
(12, 21, N'Lý Khánh Linh', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/12.jpg', '2026-08-01 08:00:00'),
(13, 22, N'Trịnh Anh Tuấn', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/13.jpg', '2026-08-01 08:00:00'),
(14, 23, N'Phan Ngọc Ánh', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/14.jpg', '2026-08-01 08:00:00'),
(15, 24, N'Mai Hữu Nghĩa', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/15.jpg', '2026-08-01 08:00:00'),
(16, 25, N'Võ Bích Hạnh', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/16.jpg', '2026-08-01 08:00:00'),
(17, 26, N'Lâm Khải Hoàn', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/17.jpg', '2026-08-01 08:00:00'),
(18, 27, N'Châu Thùy Dung', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/18.jpg', '2026-08-01 08:00:00'),
(19, 28, N'Đoàn Minh Triết', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/19.jpg', '2026-08-01 08:00:00'),
(20, 29, N'Hồ Thu Hà', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/20.jpg', '2026-08-01 08:00:00'),
(21, 30, N'Tạ Thanh Bình', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/21.jpg', '2026-08-01 08:00:00'),
(22, 31, N'Đinh Hồng Nhung', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/22.jpg', '2026-08-01 08:00:00'),
(23, 32, N'Lương Tuấn Khang', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/23.jpg', '2026-08-01 08:00:00'),
(24, 33, N'Tô Yến Nhi', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/24.jpg', '2026-08-01 08:00:00'),
(25, 34, N'Quách Văn Thái', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/25.jpg', '2026-08-01 08:00:00'),
(26, 35, N'Lê Thị Hương', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/26.jpg', '2026-08-01 08:00:00'),
(27, 36, N'Nguyễn Đức Long', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/27.jpg', '2026-08-01 08:00:00'),
(28, 37, N'Trần Thị Cẩm Tú', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/28.jpg', '2026-08-01 08:00:00'),
(29, 38, N'Phạm Quốc Việt', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/29.jpg', '2026-08-01 08:00:00'),
(30, 39, N'Vũ Thị Hồng', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/30.jpg', '2026-08-01 08:00:00'),
(31, 40, N'Hoàng Trọng Nghĩa', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/men/31.jpg', '2026-08-01 08:00:00'),
(32, 41, N'Đặng Thanh Mai', 'AQAAAAEAAYagAAAAECcPH8q1HdYNoBPESYOZmY91FexYXCFX3uFrV4DvZAKC3zAg41SMtY1H4Z09+QxvgQ==', 'https://randomuser.me/api/portraits/women/32.jpg', '2026-08-01 08:00:00');
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
(20, 3, N'Ngân hàng Đánh giá Năng lực Toàn diện', N'Bộ đề kiểm tra đánh giá chỉ số thông minh toán học (MQ)'),
(21, 1, N'Ngân hàng Đề thi Học sinh giỏi Soroban & Bàn tính Thần tốc', N'Bộ đề thi chuẩn hóa phân hạng Soroban cấp độ 1-3, rèn luyện tốc độ gảy hạt và ảo tính siêu tốc'),
(22, 2, N'Ngân hàng Thử thách Finger Math Siêu phản xạ & Tư duy Hình ảnh', N'Bộ câu hỏi phản xạ ngón tay 2 bàn tay trong phạm vi 99, kết hợp tư duy màu sắc và quy luật số học'),
(23, 3, N'Ngân hàng Bài toán thực tế Mathnasium & Suy luận Bar Model', N'Tập hợp các bài toán ứng dụng thực tế theo giáo trình Mathnasium: chuyển động, tính tuổi, chia tỉ lệ'),
(24, 5, N'Ngân hàng Tư duy Socrates - Phản biện & Nghịch lý Toán học', N'Các bài toán mở, câu đố logic đa chiều kích thích trẻ tự lập luận, bảo vệ quan điểm và tranh luận');
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
(40, 15, N'Mai Quỳnh Chi', 'https://api.dicebear.com/7.x/bottts/svg?seed=student40', 680, N'Nữ', 36, 9, 1, '2026-08-15 08:00:00'),
(41, 1, N'Bùi Anh Thư', 'https://api.dicebear.com/7.x/bottts/svg?seed=student41', 620, N'Nữ', 30, 8, 1, '2026-08-15 08:00:00'),
(42, 2, N'Nguyễn Minh Khôi', 'https://api.dicebear.com/7.x/bottts/svg?seed=student42', 780, N'Nam', 38, 10, 1, '2026-08-15 08:00:00'),
(43, 3, N'Trần Bảo Vy', 'https://api.dicebear.com/7.x/bottts/svg?seed=student43', 890, N'Nữ', 44, 14, 1, '2026-08-15 08:00:00'),
(44, 4, N'Lê Quốc Huy', 'https://api.dicebear.com/7.x/bottts/svg?seed=student44', 510, N'Nam', 25, 6, 1, '2026-08-15 08:00:00'),
(45, 5, N'Phạm Đăng Khoa', 'https://api.dicebear.com/7.x/bottts/svg?seed=student45', 940, N'Nam', 48, 16, 1, '2026-08-15 08:00:00'),
(46, 6, N'Vũ Thảo Linh', 'https://api.dicebear.com/7.x/bottts/svg?seed=student46', 670, N'Nữ', 33, 9, 1, '2026-08-15 08:00:00'),
(47, 7, N'Hoàng Gia Huy', 'https://api.dicebear.com/7.x/bottts/svg?seed=student47', 430, N'Nam', 21, 5, 1, '2026-08-15 08:00:00'),
(48, 8, N'Đỗ Ngọc Mai', 'https://api.dicebear.com/7.x/bottts/svg?seed=student48', 1050, N'Nữ', 55, 20, 1, '2026-08-15 08:00:00'),
(49, 9, N'Đinh Tuấn Kiệt', 'https://api.dicebear.com/7.x/bottts/svg?seed=student49', 720, N'Nam', 36, 11, 1, '2026-08-15 08:00:00'),
(50, 10, N'Lâm Phương Nghi', 'https://api.dicebear.com/7.x/bottts/svg?seed=student50', 860, N'Nữ', 42, 13, 1, '2026-08-15 08:00:00');
SET IDENTITY_INSERT [dbo].[STUDENT_PROFILE] OFF;

-- =============================================================================
-- 9. BẢNG CLASSES (20 lớp học Toán Tư Duy)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[CLASSES] ON;
INSERT INTO [dbo].[CLASSES] (id, course_level_id, teacher_id, name, max_capacity, start_date, end_date, status, created_at, updated_at) VALUES
(1, 1, 9, N'Lớp Soroban Cơ bản SRB-01', 15, '2026-09-01 08:00:00', '2026-11-30 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(2, 1, 1, N'Lớp Soroban Cơ bản SRB-02', 15, '2026-09-05 08:00:00', '2026-12-05 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(3, 2, 1, N'Lớp Soroban Trung cấp SRB-03', 12, '2026-09-10 08:00:00', '2026-12-10 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(4, 3, 1, N'Lớp Soroban Nâng cao SRB-04', 10, '2026-09-15 08:00:00', '2026-12-15 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(5, 4, 9, N'Lớp Finger Math Mầm non FGM-01', 12, '2026-09-01 08:00:00', '2026-11-15 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(6, 5, 2, N'Lớp Finger Math Cơ bản FGM-02', 15, '2026-09-02 08:00:00', '2026-11-20 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(7, 6, 2, N'Lớp Finger Math Nâng cao FGM-03', 12, '2026-09-03 08:00:00', '2026-11-25 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(8, 7, 3, N'Lớp Mathnasium Level 1 MTN-01', 10, '2026-09-01 08:00:00', '2026-12-01 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(9, 8, 3, N'Lớp Mathnasium Level 2 MTN-02', 10, '2026-09-05 08:00:00', '2026-12-05 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(10, 9, 3, N'Lớp Mathnasium Level 3 MTN-03', 10, '2026-09-10 08:00:00', '2026-12-10 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(11, 10, 4, N'Lớp Kumon Starter KMN-01', 15, '2026-09-01 08:00:00', '2026-11-30 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(12, 11, 4, N'Lớp Kumon Elementary KMN-02', 15, '2026-09-05 08:00:00', '2026-12-05 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(13, 12, 4, N'Lớp Kumon Advanced KMN-03', 12, '2026-09-10 08:00:00', '2026-12-10 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(14, 13, 5, N'Lớp I-MATH Foundation IMT-01', 12, '2026-09-01 08:00:00', '2026-11-20 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(15, 14, 5, N'Lớp I-MATH Explorer IMT-02', 12, '2026-09-05 08:00:00', '2026-11-25 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(16, 15, 5, N'Lớp I-MATH Master IMT-03', 10, '2026-09-10 08:00:00', '2026-12-01 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(17, 16, 6, N'Lớp CPA Concrete CPA-01', 12, '2026-09-01 08:00:00', '2026-11-15 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(18, 17, 6, N'Lớp CPA Pictorial CPA-02', 12, '2026-09-05 08:00:00', '2026-11-20 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(19, 18, 7, N'Lớp CPA Abstract CPA-03', 12, '2026-09-10 08:00:00', '2026-12-01 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(20, 8, 8, N'Lớp Tư duy Không gian MTN-SP01', 10, '2026-09-15 08:00:00', '2026-12-15 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(21, 3, 9, N'Lớp Soroban Siêu Tốc & Ảo Tính SRB-VIP01', 15, '2026-09-01 08:00:00', '2026-11-30 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL),
(22, 13, 9, N'Lớp Toán Tư Duy Phản Biện & Logic IMT-PRO01', 12, '2026-09-05 08:00:00', '2026-12-05 17:00:00', 'ACTIVE', '2026-08-01 08:00:00', NULL);
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
(1, 1, 'MULTIPLE_CHOICE', N'Khi gảy bàn tính Soroban, hạt ở hàng trên có giá trị quy ước bằng bao nhiêu?
A. 1
B. 5
C. 10
D. 50', 'https://images.unsplash.com/photo-1596495578065-6e0763fa1178?w=600', 'EASY', N'B. 5
- Lời giải: Cấu tạo bàn tính Soroban có 1 thanh ngang phân tách: hạt ở phía trên có giá trị 5, các hạt ở phía dưới có giá trị 1.'),
(2, 1, 'FILL_IN', N'Tính nhẩm nhanh thao tác hạt trên bàn tính Soroban: 3 + 1 + 5 - 2 = ?', NULL, 'EASY', N'7
- Hướng dẫn giải: Thực hiện lần lượt: gảy 3 hạt dưới lên (+3), thêm 1 hạt dưới (+1 = 4), gảy hạt 5 trên xuống (+5 = 9), gảy 2 hạt dưới xuống (-2 = 7).'),
(3, 2, 'MULTIPLE_CHOICE', N'Trong công thức Anh bạn nhỏ của Soroban, bạn nhỏ của 4 để tạo thành 5 là số nào?
A. 1
B. 2
C. 3
D. 5', NULL, 'EASY', N'A. 1
- Lời giải: Hai số có tổng bằng 5 được gọi là Anh bạn nhỏ của nhau. Do 4 + 1 = 5 nên bạn nhỏ của 4 là 1.'),
(4, 2, 'MULTIPLE_CHOICE', N'Để thực hiện phép tính +4 khi cột không đủ 4 hạt dưới trên bàn tính Soroban, ta thực hiện thao tác nào?
A. Thêm 5 và bớt 1 hạt dưới
B. Thêm 10 và bớt 6
C. Thêm 5 và bớt 4 hạt dưới
D. Gảy 4 hạt dưới lên', NULL, 'MEDIUM', N'A. Thêm 5 và bớt 1 hạt dưới
- Lời giải: Áp dụng công thức Anh bạn nhỏ cộng (+4 = +5 - 1): gảy hạt 5 xuống và gạt 1 hạt dưới xuống.'),
(5, 3, 'FILL_IN', N'Trong công thức Anh bạn lớn của Soroban, bạn lớn của 7 là mấy để tạo thành 10?', NULL, 'MEDIUM', N'3
- Hướng dẫn giải: Hai số có tổng bằng 10 được gọi là Anh bạn lớn của nhau. Vì 7 + 3 = 10 nên bạn lớn của 7 là 3.'),
(6, 3, 'MULTIPLE_CHOICE', N'Thực hiện phép tính qua cầu 10: 8 + 5 = ? (Quy tắc: Cộng 10 bớt bạn lớn của 5)
A. 12
B. 13
C. 14
D. 15', NULL, 'MEDIUM', N'B. 13
- Lời giải: Ta có 8 + 5 = 8 + 10 - 5 = 18 - 5 = 13. Thao tác: thêm 1 ở cột hàng chục và bớt 5 ở cột đơn vị.'),
(7, 4, 'FILL_IN', N'Thực hiện ảo tính Anzan (tưởng tượng bàn tính trong tâm trí): 12 + 15 - 7 + 20 = ?', NULL, 'HARD', N'40
- Hướng dẫn giải: 12 + 15 = 27; 27 - 7 = 20; 20 + 20 = 40. Học sinh hình dung chuyển động của hạt tính trên thanh định vị.'),
(8, 4, 'MULTIPLE_CHOICE', N'Phương pháp ảo tính Anzan Soroban kích thích sự phối hợp của vùng não nào mạnh mẽ nhất?
A. Bán cầu não trái (Logic, phân tích)
B. Bán cầu não phải (Hình ảnh, không gian)
C. Đồng thời cả hai bán cầu não (Toàn não)
D. Vùng tiểu não điều khiển vận động', 'https://www.youtube.com/watch?v=0k2Gbp475m8', 'MEDIUM', N'C. Đồng thời cả hai bán cầu não (Toàn não)
- Lời giải: Não phải hình dung bàn tính ảo dưới dạng hình ảnh di động, não trái đồng thời xử lý các quy tắc số học logic, giúp tối ưu hóa kết nối hai bán cầu não.'),
(9, 5, 'MULTIPLE_CHOICE', N'Trong quy ước Finger Math bàn tay phải, ngón cái xòe ra biểu thị giá trị số mấy?
A. 1
B. 5
C. 9
D. 10', 'https://images.unsplash.com/photo-1509228468518-180dd4864904?w=600', 'EASY', N'B. 5
- Lời giải: Trên bàn tay phải (hàng đơn vị), 4 ngón dài (trỏ, giữa, áp út, út) mỗi ngón có giá trị là 1, riêng ngón cái có giá trị đặc biệt là 5.'),
(10, 5, 'FILL_IN', N'Xòe ngón cái và 3 ngón dài (trỏ, giữa, áp út) của bàn tay phải biểu thị số mấy?', NULL, 'EASY', N'8
- Hướng dẫn giải: Ngón cái mang giá trị 5, cộng thêm 3 ngón dài mỗi ngón 1: 5 + 1 + 1 + 1 = 8.'),
(11, 6, 'FILL_IN', N'Tính nhẩm bằng Finger Math phối hợp hai bàn tay: 23 + 15 = ?', NULL, 'MEDIUM', N'38
- Hướng dẫn giải: Tay trái (hàng chục): 2 + 1 = 3 (xòe 3 ngón). Tay phải (hàng đơn vị): 3 + 5 = 8 (xòe ngón cái và 3 ngón). Kết quả: 38.'),
(12, 6, 'MULTIPLE_CHOICE', N'Trên phương pháp Finger Math hai bàn tay, số lớn nhất có thể biểu diễn được là số nào?
A. 89
B. 90
C. 99
D. 100', NULL, 'EASY', N'C. 99
- Lời giải: Tay trái biểu diễn tối đa số 90 (ngón cái 50 + 4 ngón 40), tay phải biểu diễn tối đa số 9 (ngón cái 5 + 4 ngón 4). Tổng cộng là 99.'),
(13, 7, 'MULTIPLE_CHOICE', N'Tính nhẩm phản xạ ngón tay tốc độ cao: 47 + 22 - 18 = ?
A. 51
B. 52
C. 50
D. 49', NULL, 'HARD', N'A. 51
- Lời giải: 47 + 22 = 69; 69 - 18 = 51. Thao tác: tay trái 4+2=6, 6-1=5; tay phải 7+2=9, 9-8=1. Kết quả là 51.'),
(14, 7, 'FILL_IN', N'Một bạn nhỏ giơ tay trái xòe 4 ngón dài (gập ngón cái) và tay phải xòe 4 ngón dài (gập ngón cái). Số đó là số bao nhiêu?', NULL, 'MEDIUM', N'44
- Hướng dẫn giải: Tay trái có 4 ngón dài = 40. Tay phải có 4 ngón dài = 4. Số biểu thị là 44.'),
(15, 8, 'MULTIPLE_CHOICE', N'Số liền trước của số tròn chục nhỏ nhất có 3 chữ số là số nào?
A. 90
B. 99
C. 101
D. 109', NULL, 'EASY', N'B. 99
- Lời giải: Số tròn chục nhỏ nhất có 3 chữ số là 100. Số liền trước của 100 là 100 - 1 = 99.'),
(16, 8, 'FILL_IN', N'Có bao nhiêu số chẵn có 2 chữ số lớn hơn 20 và nhỏ hơn 30?', NULL, 'EASY', N'4
- Hướng dẫn giải: Các số chẵn trong khoảng từ 21 đến 29 là: 22, 24, 26, 28. Tổng cộng có 4 số.'),
(17, 9, 'MULTIPLE_CHOICE', N'Một hình lập phương chuẩn có bao nhiêu mặt, bao nhiêu đỉnh và bao nhiêu cạnh?
A. 6 mặt, 8 đỉnh, 12 cạnh
B. 6 mặt, 12 đỉnh, 8 cạnh
C. 8 mặt, 6 đỉnh, 12 cạnh
D. 4 mặt, 4 đỉnh, 6 cạnh', 'https://images.unsplash.com/photo-1509228468518-180dd4864904?w=600', 'MEDIUM', N'A. 6 mặt, 8 đỉnh, 12 cạnh
- Lời giải: Hình lập phương gồm 6 mặt vuông bằng nhau, 8 đỉnh và 12 cạnh có độ dài bằng nhau.'),
(18, 9, 'MULTIPLE_CHOICE', N'Hình học nào dưới đây vừa có 4 cạnh bằng nhau, vừa có 4 góc vuông?
A. Hình chữ nhật
B. Hình thoi
C. Hình vuông
D. Hình bình hành', NULL, 'EASY', N'C. Hình vuông
- Lời giải: Hình vuông là tứ giác đặc biệt có 4 cạnh bằng nhau (tính chất hình thoi) và 4 góc vuông (tính chất hình chữ nhật).'),
(19, 10, 'FILL_IN', N'Một trang trại nuôi gà và thỏ. Người ta đếm được tất cả 10 cái đầu và 28 cái chân. Hỏi có bao nhiêu con thỏ?', NULL, 'HARD', N'4
- Hướng dẫn giải: Giả sử tất cả 10 con đều là gà thì có: 10 x 2 = 20 cái chân. Số chân hụt đi là: 28 - 20 = 8 chân. Mỗi con thỏ hơn gà: 4 - 2 = 2 chân. Vậy số thỏ là: 8 : 2 = 4 con thỏ.'),
(20, 10, 'MULTIPLE_CHOICE', N'Mẹ mua 3 hộp sữa và 2 cái bánh hết 55 nghìn đồng. Biết giá 1 hộp sữa đắt hơn 1 cái bánh 5 nghìn đồng. Giá tiền 1 cái bánh là bao nhiêu?
A. 8 nghìn đồng
B. 10 nghìn đồng
C. 12 nghìn đồng
D. 15 nghìn đồng', NULL, 'HARD', N'A. 8 nghìn đồng
- Lời giải: Gọi giá 1 cái bánh là x nghìn. Giá 1 hộp sữa là x + 5. Ta có: 3*(x + 5) + 2*x = 55 => 5x + 15 = 55 => 5x = 40 => x = 8 nghìn đồng.'),
(21, 11, 'FILL_IN', N'Tìm số tiếp theo trong dãy số quy luật nhân đôi: 2, 4, 8, 16, 32, ... ?', NULL, 'EASY', N'64
- Hướng dẫn giải: Mỗi số đứng sau gấp 2 lần số đứng trước: 32 x 2 = 64.'),
(22, 11, 'MULTIPLE_CHOICE', N'Tìm số còn thiếu trong dãy số bình phương: 1, 4, 9, 16, [ ? ], 36, 49
A. 20
B. 24
C. 25
D. 30', NULL, 'MEDIUM', N'C. 25
- Lời giải: Dãy số là bình phương của các số tự nhiên liên tiếp: 1^2=1, 2^2=4, 3^2=9, 4^2=16, 5^2=25, 6^2=36, 7^2=49. Số cần tìm là 25.'),
(23, 12, 'FILL_IN', N'Tính nhanh bằng cách nhóm số hợp lý: 125 x 8 x 25 x 4 = ?', NULL, 'MEDIUM', N'100000
- Hướng dẫn giải: Ghép đôi số tròn chục, tròn trăm: (125 x 8) x (25 x 4) = 1.000 x 100 = 100.000.'),
(24, 12, 'MULTIPLE_CHOICE', N'Khẳng định nào sau đây về tính chẵn lẻ của phép nhân là luôn luôn ĐÚNG?
A. Tích của hai số lẻ là một số chẵn
B. Tích của một số lẻ với một số chẵn luôn là số chẵn
C. Tổng của hai số lẻ luôn là một số lẻ
D. Tích của hai số chẵn có thể là số lẻ', NULL, 'EASY', N'B. Tích của một số lẻ với một số chẵn luôn là số chẵn
- Lời giải: Số chẵn có dạng 2k, do đó khi nhân với bất kỳ số nguyên nào thì kết quả luôn chứa thừa số 2, tức là một số chẵn.'),
(25, 13, 'MULTIPLE_CHOICE', N'Rút gọn phân số 48/64 về dạng phân số tối giản ta được kết quả là:
A. 6/8
B. 3/4
C. 12/16
D. 24/32', NULL, 'MEDIUM', N'B. 3/4
- Lời giải: ƯCLN của 48 và 64 là 16. Chia cả tử và mẫu cho 16: 48:16 = 3, 64:16 = 4. Phân số tối giản là 3/4.'),
(26, 13, 'FILL_IN', N'Tính giá trị biểu thức phân số: (3/4 + 1/2) : 5/8 = ?', NULL, 'HARD', N'2
- Hướng dẫn giải: 3/4 + 1/2 = 3/4 + 2/4 = 5/4. Tiếp tục: 5/4 : 5/8 = 5/4 x 8/5 = 8/4 = 2.'),
(27, 14, 'MULTIPLE_CHOICE', N'Bạn Nam nói: ''Số chia hết cho 2 thì chắc chắn chia hết cho 4''. Lập luận của Nam đúng hay sai?
A. Luôn đúng
B. Sai, ví dụ số 6 chia hết cho 2 nhưng không chia hết cho 4
C. Chỉ đúng với số có 2 chữ số
D. Không thể kết luận', NULL, 'EASY', N'B. Sai, ví dụ số 6 chia hết cho 2 nhưng không chia hết cho 4
- Lời giải: Mệnh đề đảo mới đúng: Số chia hết cho 4 thì chia hết cho 2. Ví dụ phản chứng số 6, 10, 14 chia hết cho 2 nhưng không chia hết cho 4.'),
(28, 14, 'ESSAY', N'Giải thích tính chất bắc cầu trong bất đẳng thức: Nếu A > B và B > C thì hãy chứng minh A > C bằng ví dụ thực tế.', NULL, 'MEDIUM', N'- Hướng dẫn chấm & Lời giải:
Bước 1: Nêu định nghĩa tính chất bắc cầu: thứ bậc so sánh được bảo toàn qua phần tử trung gian B.
Bước 2: Lấy ví dụ thực tế (chiều cao, cân nặng, hoặc độ dài thước kẻ). Ví dụ: Lan cao hơn Mai (140cm > 135cm), Mai cao hơn Hoa (135cm > 130cm) => Lan chắc chắn cao hơn Hoa (140cm > 130cm).
Bước 3: Kết luận đúng tính logic.'),
(29, 15, 'MULTIPLE_CHOICE', N'Làm thế nào chia đều 7 quả táo cho 8 bạn nhỏ mà không bổ bất kỳ quả nào quá 4 phần?
A. Bổ 4 quả làm đôi và 3 quả làm 4 phần
B. Bổ mỗi quả làm 8 phần nhỏ
C. Cân cả 7 quả táo rồi chia theo khối lượng
D. Bổ mỗi quả làm 3 phần', NULL, 'HARD', N'A. Bổ 4 quả làm đôi và 3 quả làm 4 phần
- Lời giải: Bổ 4 quả làm đôi được 8 nửa quả (mỗi bạn 1/2 quả). Bổ 3 quả làm 4 được 12 phần tư (mỗi bạn lấy thêm 1/4 quả và còn dư chia đều). Tổng mỗi bạn nhận 1/2 + 1/4 + 1/8 quả mà không quả nào bị chia quá 4 nhát.'),
(30, 15, 'FILL_IN', N'Nếu ngày hôm qua là ngày mai của thứ Hai, thì ngày mai là ngày hôm qua của thứ mấy?', NULL, 'HARD', N'Thứ Năm
- Hướng dẫn giải: ''Ngày mai của thứ Hai'' là thứ Ba => Hôm qua là thứ Ba => Hôm nay là thứ Tư => Ngày mai là thứ Năm => Thứ Năm chính là hôm qua của thứ Sáu (hoặc ngày mai là Thứ Năm).'),
(31, 16, 'MULTIPLE_CHOICE', N'Bạn đang lái xe buýt có 10 hành khách. Trạm 1 xuống 3 người, lên 2 người. Trạm 2 xuống 4 người, lên 5 người. Hỏi mắt người lái xe màu gì?
A. Mắt màu nâu
B. Bằng màu mắt của chính bạn (vì bạn là người lái xe)
C. Mắt màu đen
D. Không xác định được', NULL, 'EASY', N'B. Bằng màu mắt của chính bạn (vì bạn là người lái xe)
- Lời giải: Đề bài mở đầu bằng câu: ''BẠN đang lái xe buýt'', do đó người lái xe chính là bạn, màu mắt của người lái là màu mắt của chính người giải.'),
(32, 17, 'FILL_IN', N'Bé có 3 đĩa cam, mỗi đĩa xếp đúng 4 quả cam tươi. Viết phép tính nhân tương ứng và cho biết tổng số cam là bao nhiêu quả?', NULL, 'EASY', N'12
- Hướng dẫn giải: Phép tính nhân: 4 x 3 = 12 quả cam (hoặc 3 x 4 = 12).'),
(33, 17, 'MULTIPLE_CHOICE', N'Giai đoạn ''Cụ thể'' (Concrete) trong tiến trình CPA đóng vai trò gì quan trọng nhất đối với trẻ mầm non?
A. Ghi nhớ các công thức đại số phức tạp
B. Tương tác trực tiếp bằng xúc giác với que tính, khối hình thực tế để hình thành biểu tượng toán học
C. Tăng tốc độ bấm máy tính cầm tay
D. Làm quen ngay với các phương trình ẩn x', NULL, 'EASY', N'B. Tương tác trực tiếp bằng xúc giác với que tính, khối hình thực tế để hình thành biểu tượng toán học
- Lời giải: Phương pháp CPA xuất phát từ cụ thể (Concrete): trẻ cần cầm, nắm, sờ các đồ vật thật trước khi chuyển sang hình ảnh tượng trưng.'),
(34, 18, 'MULTIPLE_CHOICE', N'Mô hình thanh Bar Model: Thanh A dài hơn thanh B là 12 đơn vị. Tổng độ dài 2 thanh là 40 đơn vị. Độ dài của thanh B (số bé) là:
A. 14 đơn vị
B. 26 đơn vị
C. 16 đơn vị
D. 28 đơn vị', NULL, 'MEDIUM', N'A. 14 đơn vị
- Lời giải: Bài toán Tổng - Hiệu: Số bé = (Tổng - Hiệu) : 2 = (40 - 12) : 2 = 28 : 2 = 14 đơn vị.'),
(35, 18, 'FILL_IN', N'Vẽ sơ đồ đoạn thẳng: Tuổi bố gấp 4 lần tuổi con, bố hơn con 30 tuổi. Hỏi năm nay con bao nhiêu tuổi?', NULL, 'MEDIUM', N'10
- Hướng dẫn giải: Hiệu số phần bằng nhau: 4 - 1 = 3 phần. Giá trị 1 phần (tuổi con): 30 : 3 = 10 tuổi.'),
(36, 19, 'MULTIPLE_CHOICE', N'Chuyển mô hình thanh ''Tìm hai số biết tổng là S và tỉ số là a/b'' sang công thức đại số tìm số bé là:
A. S : (a + b) x a
B. S : (a - b) x a
C. S x a + b
D. S : a x b', NULL, 'HARD', N'A. S : (a + b) x a
- Lời giải: Tổng số phần bằng nhau là (a + b). Giá trị 1 phần là S : (a + b). Số bé chiếm a phần nên bằng [S : (a + b)] x a.'),
(37, 19, 'FILL_IN', N'Tìm nghiệm x trong phương trình bậc nhất cơ bản: 2x + 15 = 45. Giá trị của x là bao nhiêu?', NULL, 'EASY', N'15
- Hướng dẫn giải: 2x = 45 - 15 = 30 => x = 30 : 2 = 15.'),
(38, 20, 'MULTIPLE_CHOICE', N'Trong một buổi giao lưu toán học có 8 bạn tham gia. Mỗi bạn bắt tay với tất cả các bạn còn lại đúng 1 lần. Có tất cả bao nhiêu cái bắt tay?
A. 28
B. 32
C. 56
D. 64', NULL, 'HARD', N'A. 28
- Lời giải: Số cái bắt tay là tổ hợp chập 2 của 8: C(8,2) = (8 x 7) : 2 = 28 cái bắt tay.'),
(39, 20, 'FILL_IN', N'Tìm số tự nhiên nhỏ nhất có 2 chữ số mà khi chia cho cả 2, 3 và 5 đều dư 1:', NULL, 'MEDIUM', N'31
- Hướng dẫn giải: BCNN của 2, 3, 5 là 30. Số chia cho 2, 3, 5 dư 1 có dạng 30k + 1. Với k = 1 ta được số nhỏ nhất có 2 chữ số là 31.'),
(40, 20, 'MULTIPLE_CHOICE', N'Tổng của 5 số tự nhiên liên tiếp bất kỳ luôn luôn có tính chất chia hết nào sau đây?
A. Luôn chia hết cho 5
B. Luôn chia hết cho 10
C. Luôn là số nguyên tố
D. Luôn là số lẻ', NULL, 'MEDIUM', N'A. Luôn chia hết cho 5
- Lời giải: Gọi 5 số liên tiếp là n, n+1, n+2, n+3, n+4. Tổng của chúng là: 5n + 10 = 5(n + 2), luôn luôn chia hết cho 5.'),
(41, 21, 'MULTIPLE_CHOICE', N'Trong kỳ thi Soroban quốc tế, thao tác ''Gảy kép'' (Pinch in / Pinch out) được áp dụng khi nào?
A. Khi gảy đồng thời hạt 5 và các hạt 1 cùng tiến về hoặc rời xa thanh ngang
B. Khi cộng hai số có 3 chữ số
C. Khi xóa toàn bộ bàn tính về số 0
D. Khi đổi cột hàng chục sang hàng trăm', 'https://www.youtube.com/watch?v=0k2Gbp475m8', 'MEDIUM', N'A. Khi gảy đồng thời hạt 5 và các hạt 1 cùng tiến về hoặc rời xa thanh ngang
- Lời giải: Gảy kép là kỹ thuật dùng đồng thời ngón cái và ngón trỏ gộp hạt 5 và các hạt dưới (ví dụ gộp số 6, 7, 8, 9) chỉ trong 1 thao tác duy nhất.'),
(42, 21, 'FILL_IN', N'Thực hiện phép tính liên hoàn Soroban cấp độ Flash Anzan (tốc độ 3 giây): 45 + 54 - 23 + 12 - 38 = ?', NULL, 'HARD', N'50
- Hướng dẫn giải: 45 + 54 = 99; 99 - 23 = 76; 76 + 12 = 88; 88 - 38 = 50.'),
(43, 21, 'MULTIPLE_CHOICE', N'Khi thực hiện phép nhân 3 chữ số với 1 chữ số trên Soroban (VD: 342 x 6), ta bắt đầu đặt kết quả từ cột nào?
A. Cột hàng đơn vị
B. Cột hàng trăm (tổng số chữ số của 2 thừa số: 3 + 1 = 4)
C. Cột bất kỳ
D. Cột ngoài cùng bên phải', NULL, 'HARD', N'B. Cột hàng trăm (tổng số chữ số của 2 thừa số: 3 + 1 = 4)
- Lời giải: Quy tắc nhân Soroban: Cột bắt đầu ghi nhận tích = Tổng số chữ số của các thừa số (m + n). Với 3 chữ số nhân 1 chữ số, m + n = 4 (cột thứ 4 tính từ đơn vị sang trái).'),
(44, 22, 'MULTIPLE_CHOICE', N'Trên đôi bàn tay Finger Math, thao tác ''Bung anh béo, thu 4 anh gầy'' tương ứng với phép tính nào?
A. Phép tính +1
B. Phép tính +5 (hoặc công thức +4 khi cột đầy)
C. Phép tính -5
D. Phép tính +9', 'https://images.unsplash.com/photo-1509228468518-180dd4864904?w=600', 'EASY', N'B. Phép tính +5 (hoặc công thức +4 khi cột đầy)
- Lời giải: Trong ngôn ngữ Finger Math cho trẻ nhỏ: ngón cái gọi là ''anh béo'' (giá trị 5), 4 ngón dài gọi là ''anh gầy'' (mỗi ngón 1). Bung anh béo thu 4 anh gầy là thao tác đổi 4 hạt 1 lấy 1 hạt 5 khi cộng thêm 1 từ 4 lên 5.'),
(45, 22, 'FILL_IN', N'Tính nhẩm siêu tốc hai bàn tay Finger Math: 36 + 42 - 17 = ?', NULL, 'MEDIUM', N'61
- Hướng dẫn giải: Bước 1: 36 + 42 = 78 (tay trái 30+40=70, tay phải 6+2=8). Bước 2: 78 - 17 = 61 (tay trái 70-10=60, tay phải 8-7=1). Kết quả là 61.'),
(46, 22, 'ESSAY', N'Hãy trình bày quy tắc tính nhẩm qua cầu 10 trên hai bàn tay Finger Math cho phép tính: 28 + 5 = ?', NULL, 'MEDIUM', N'- Hướng dẫn chấm & Lời giải:
Bước 1: Biểu diễn số 28 (tay trái giơ 2 ngón = 20, tay phải giơ ngón cái + 3 ngón = 8).
Bước 2: Cộng thêm 5: Vì tay phải chỉ còn 1 ngón chưa xòe nên không đủ 5. Áp dụng quy tắc qua 10: Thêm 1 ở tay trái (+10 thành 30), bớt bạn lớn của 5 là 5 ở tay phải (-5 bằng cách gập ngón cái).
Bước 3: Đọc kết quả: Tay trái có 3 ngón (30), tay phải có 3 ngón (3) => Kết quả là 33.'),
(47, 23, 'MULTIPLE_CHOICE', N'Hai ô tô xuất phát cùng một lúc từ hai tỉnh A và B cách nhau 180 km, đi ngược chiều nhau. Vận tốc xe thứ nhất là 50 km/h, xe thứ hai là 40 km/h. Sau bao lâu hai xe gặp nhau?
A. 1.5 giờ
B. 2 giờ
C. 2.5 giờ
D. 3 giờ', NULL, 'MEDIUM', N'B. 2 giờ
- Lời giải: Tổng vận tốc hai xe: 50 + 40 = 90 (km/h). Thời gian để hai xe gặp nhau: 180 : 90 = 2 giờ.'),
(48, 23, 'FILL_IN', N'Một bể nước hình hộp chữ nhật có chiều dài 2m, chiều rộng 1.5m và chiều cao 1.2m. Hỏi bể đó chứa được tối đa bao nhiêu lít nước? (Biết 1 dm3 = 1 lít)', NULL, 'MEDIUM', N'3600
- Hướng dẫn giải: Thể tích bể: 2 x 1.5 x 1.2 = 3.6 m3. Đổi ra dm3: 3.6 m3 = 3600 dm3 = 3600 lít nước.'),
(49, 23, 'MULTIPLE_CHOICE', N'Phương pháp Bar Model (Singapore / Mathnasium) giải quyết bài toán: ''Số bi của An bằng 2/3 số bi của Bình, Bình có nhiều hơn An 15 viên bi''. Số bi của Bình là:
A. 30 viên
B. 45 viên
C. 60 viên
D. 75 viên', 'https://images.unsplash.com/photo-1596495578065-6e0763fa1178?w=600', 'HARD', N'B. 45 viên
- Lời giải: Vẽ sơ đồ thanh: An 2 phần, Bình 3 phần. Hiệu số phần: 3 - 2 = 1 phần tương ứng 15 viên. Số bi của Bình là: 15 x 3 = 45 viên bi.'),
(50, 24, 'MULTIPLE_CHOICE', N'Nghịch lý Achilles và Rùa của Zeno lập luận rằng kẻ chạy nhanh hơn không bao giờ bắt kịp kẻ chạy chậm nếu kẻ chậm xuất phát trước. Sai lầm cốt lõi của lập luận Zeno nằm ở đâu?
A. Giả định rằng tổng của vô hạn khoảng thời gian nhỏ luôn là vô hạn
B. Vận tốc của Achilles quá chậm
C. Rùa không bao giờ dừng lại nghỉ
D. Khoảng cách giữa hai bên bị co lại', NULL, 'HARD', N'A. Giả định rằng tổng của vô hạn khoảng thời gian nhỏ luôn là vô hạn
- Lời giải: Zeno sai lầm khi coi chuỗi vô hạn các khoảng thời gian luôn tiến tới vô cực. Trong giải tích hiện đại, chuỗi hình học vô hạn có thể hội tụ về một giới hạn hữu hạn, do đó Achilles sẽ vượt qua rùa tại thời điểm xác định.'),
(51, 24, 'FILL_IN', N'Có 9 đồng tiền vàng giống hệt nhau, trong đó có 1 đồng tiền giả nhẹ hơn các đồng còn lại. Bằng cân đĩa thăng bằng (không có quả cân), số lần cân ít nhất để chắc chắn tìm ra đồng tiền giả là bao nhiêu lần?', NULL, 'HARD', N'2
- Hướng dẫn giải: Chia 9 đồng tiền thành 3 nhóm (mỗi nhóm 3 đồng: A, B, C). Lần 1: Cân nhóm A và nhóm B. Nếu cân thăng bằng thì đồng giả ở nhóm C; nếu lệch thì đồng giả ở nhóm nhẹ hơn. Lần 2: Lấy nhóm 3 đồng chứa tiền giả, đặt 2 đồng lên 2 đĩa cân. Nếu lệch ta tìm ra ngay, nếu thăng bằng thì đồng còn lại là giả. Chỉ cần đúng 2 lần cân!'),
(52, 24, 'ESSAY', N'Hãy chỉ ra bước ngụy biện (lỗi toán học) trong phép ''chứng minh'' 1 = 2 sau đây:
1. Cho a = b
2. a^2 = ab
3. a^2 - b^2 = ab - b^2
4. (a - b)(a + b) = b(a - b)
5. Chia cả hai vế cho (a - b) ta được: a + b = b
6. Vì a = b nên b + b = b => 2b = b => 2 = 1.', NULL, 'HARD', N'- Hướng dẫn chấm & Lời giải:
Bước 1: Xác định bước sai là Bước 5: Chia cả hai vế cho (a - b).
Bước 2: Giải thích lý do: Vì giả thiết ban đầu là a = b nên a - b = 0. Phép chia cho 0 là không hợp lệ và vô nghĩa trong toán học.
Bước 3: Kết luận: Mọi suy luận từ bước 5 trở đi đều không có giá trị.'),
(53, 1, 'MULTIPLE_CHOICE', N'Thao tác xóa số và chuẩn bị bàn tính Soroban về trạng thái số 0 được gọi là gì?
A. Clear / Trả về vạch 0
B. Reset
C. Format
D. Refresh', NULL, 'EASY', N'A. Clear / Trả về vạch 0
- Lời giải: Thao tác dùng ngón trỏ hoặc ngón cái lướt nhẹ dọc thanh ngang để tách toàn bộ hạt trên lên trên và hạt dưới xuống dưới, đưa bàn tính về giá trị 0.'),
(54, 1, 'FILL_IN', N'Gảy bàn tính Soroban: 15 + 23 - 16 = ?', NULL, 'EASY', N'22
- Hướng dẫn giải: 15 + 23 = 38; 38 - 16 = 22.'),
(55, 5, 'MULTIPLE_CHOICE', N'Phép tính: 4 + 4 trên Finger Math bàn tay phải sử dụng công thức nào?
A. +4 = +5 - 1
B. +4 = +10 - 6
C. +4 = +4
D. +4 = +5 - 2', NULL, 'EASY', N'A. +4 = +5 - 1
- Lời giải: Vì tay phải đang có 4 ngón dài xòe, muốn cộng thêm 4 ta bung ngón cái (+5) và gập bớt 1 ngón dài (-1), được kết quả là 8.'),
(56, 5, 'FILL_IN', N'Xòe ngón cái và ngón trỏ của bàn tay phải biểu thị số mấy?', NULL, 'EASY', N'6
- Hướng dẫn giải: Ngón cái (5) + ngón trỏ (1) = 6.'),
(57, 8, 'MULTIPLE_CHOICE', N'Số nào dưới đây chia hết cho cả 2 và 5?
A. 125
B. 240
C. 332
D. 455', NULL, 'EASY', N'B. 240
- Lời giải: Dấu hiệu chia hết: Một số chia hết cho cả 2 và 5 khi và chỉ khi chữ số tận cùng của nó là 0. Số 240 thỏa mãn điều kiện này.'),
(58, 8, 'FILL_IN', N'Tìm số tròn trăm lớn nhất có 3 chữ số khác nhau:', NULL, 'EASY', N'900
- Hướng dẫn giải: Các số tròn trăm có 3 chữ số là 100, 200, ..., 900. Số lớn nhất là 900.'),
(59, 14, 'MULTIPLE_CHOICE', N'Trong một hộp kín có 5 viên bi đỏ và 5 viên bi xanh. Không nhìn vào hộp, phải lấy ra ít nhất bao nhiêu viên bi để chắc chắn có ít nhất 2 viên bi cùng màu?
A. 2 viên
B. 3 viên
C. 5 viên
D. 6 viên', NULL, 'MEDIUM', N'B. 3 viên
- Lời giải: Nguyên lý Dirichlet: Có 2 màu bi (đỏ, xanh). Trường hợp xấu nhất lấy 2 viên là 1 đỏ và 1 xanh. Viên thứ 3 chắc chắn sẽ trùng màu với 1 trong 2 viên trước. Do đó cần lấy ít nhất 3 viên bi.'),
(60, 14, 'FILL_IN', N'Một đoàn tàu dài 120m chạy qua một cây cầu dài 480m với vận tốc 36 km/h. Hỏi đoàn tàu mất bao nhiêu giây để vượt qua hoàn toàn cây cầu?', NULL, 'HARD', N'60
- Hướng dẫn giải: Đổi vận tốc: 36 km/h = 10 m/s. Quãng đường tàu đi để qua hết cầu: Chiều dài cầu + Chiều dài tàu = 480 + 120 = 600m. Thời gian vượt qua: 600 : 10 = 60 giây.'),
(61, 2, 'MULTIPLE_CHOICE', N'Trong công thức Anh bạn nhỏ trừ của Soroban, phép tính -3 khi cột không đủ hạt dưới được thực hiện bằng cách:
A. Bớt 5 thêm 2
B. Bớt 5 thêm 3
C. Bớt 10 thêm 7
D. Bớt 5 bớt 2', NULL, 'MEDIUM', N'A. Bớt 5 thêm 2
- Lời giải: Công thức Anh bạn nhỏ trừ: -3 = -5 + 2. Gạt hạt 5 lên và gạt thêm 2 hạt dưới lên.'),
(62, 3, 'FILL_IN', N'Thực hiện phép tính qua cầu 10: 9 + 6 = 9 + 10 - [ ? ]', NULL, 'MEDIUM', N'4
- Hướng dẫn giải: Bạn lớn của 6 là 4 (vì 6 + 4 = 10). Phép tính là: 9 + 10 - 4 = 15.'),
(63, 6, 'MULTIPLE_CHOICE', N'Số 75 trên hai bàn tay Finger Math được tạo thành từ:
A. Tay trái: ngón cái + 2 ngón dài; Tay phải: ngón cái
B. Tay trái: 2 ngón dài; Tay phải: ngón cái
C. Tay trái: ngón cái; Tay phải: ngón cái + 2 ngón dài
D. Tay trái: 4 ngón dài; Tay phải: 3 ngón dài', NULL, 'EASY', N'A. Tay trái: ngón cái + 2 ngón dài; Tay phải: ngón cái
- Lời giải: 75 gồm 70 ở hàng chục (ngón cái 50 + 2 ngón 20 = 70) và 5 ở hàng đơn vị (ngón cái 5).'),
(64, 7, 'FILL_IN', N'Tính nhẩm nhanh Finger Math: 99 - 44 - 33 + 12 = ?', NULL, 'MEDIUM', N'34
- Hướng dẫn giải: 99 - 44 = 55; 55 - 33 = 22; 22 + 12 = 34.'),
(65, 9, 'MULTIPLE_CHOICE', N'Hình bình hành có diện tích 48 cm2, độ dài đáy là 8 cm. Chiều cao tương ứng với đáy đó là:
A. 4 cm
B. 6 cm
C. 8 cm
D. 12 cm', NULL, 'EASY', N'B. 6 cm
- Lời giải: Diện tích hình bình hành = Đáy x Chiều cao => Chiều cao = Diện tích : Đáy = 48 : 8 = 6 cm.'),
(66, 10, 'FILL_IN', N'Hiện nay mẹ 36 tuổi, con 10 tuổi. Hỏi cách đây bao nhiêu năm tuổi mẹ gấp 3 lần tuổi con?', NULL, 'HARD', N'3
- Hướng dẫn giải: Hiệu số tuổi của mẹ và con không thay đổi theo thời gian: 36 - 10 = 26 tuổi. Khi mẹ gấp 3 lần tuổi con, hiệu số phần là: 3 - 1 = 2 phần. Tuổi con khi đó là: 26 : 2 = 13... Khoan: Khi tuổi mẹ gấp 3 tuổi con thì mẹ hơn con 2 lần tuổi con => 26 : 2 = 13 tuổi (tương lai sau 3 năm). Nhưng câu hỏi là ''mấy năm nữa'' hoặc nếu cách đây 3 năm: 33 và 7 (không gấp 3). Đặt: (36 + x) = 3*(10 + x) => 36 + x = 30 + 3x => 2x = 6 => x = 3 năm nữa. Vậy đáp số là 3.'),
(67, 15, 'MULTIPLE_CHOICE', N'Một người nông dân mang một con sói, một con dê và một bắp cải qua sông bằng một chiếc thuyền nhỏ chỉ chở được người nông dân và MỘT thứ khác. Nếu vắng mặt người, sói sẽ ăn thịt dê, dê sẽ ăn bắp cải. Vật nào phải được chở qua sông ĐẦU TIÊN?
A. Con sói
B. Con dê
C. Bắp cải
D. Con nào trước cũng được', NULL, 'MEDIUM', N'B. Con dê
- Lời giải: Nếu chở sói hoặc bắp cải trước thì dê và bắp cải (hoặc sói và dê) ở lại cùng nhau và sẽ bị ăn thịt. Bắt buộc phải chở con DÊ qua sông trước.'),
(68, 16, 'FILL_IN', N'Có một cái hồ sen, mỗi ngày số lượng hoa sen nở tăng gấp đôi. Sau 30 ngày thì hoa sen phủ kín mặt hồ. Hỏi sau bao nhiêu ngày thì hoa sen nở phủ kín MỘT NỬA mặt hồ?', NULL, 'MEDIUM', N'29
- Hướng dẫn giải: Vì mỗi ngày số hoa sen tăng gấp đôi, nên ngày trước đó mặt hồ sẽ có số lượng bằng một nửa ngày hôm sau. Do đó, vào ngày thứ 29 mặt hồ đã phủ kín 1/2, và sang ngày 30 nở gấp đôi để phủ kín hoàn toàn.'),
(69, 18, 'MULTIPLE_CHOICE', N'Mô hình Bar Model phân số: Một cửa hàng có 120 kg gạo, đã bán đi 2/5 số gạo đó. Số gạo CÒN LẠI trong cửa hàng là:
A. 48 kg
B. 72 kg
C. 60 kg
D. 80 kg', NULL, 'MEDIUM', N'B. 72 kg
- Lời giải: Phân số chỉ số gạo còn lại: 1 - 2/5 = 3/5. Số gạo còn lại là: 120 x 3/5 = 72 kg.'),
(70, 20, 'ESSAY', N'Chứng minh rằng trong 13 người bất kỳ, luôn luôn tìm được ít nhất 2 người có cùng tháng sinh.', NULL, 'EASY', N'- Hướng dẫn chấm & Lời giải:
Bước 1: Nêu số lượng tháng sinh có thể có trong một năm là 12 tháng (từ tháng 1 đến tháng 12).
Bước 2: Áp dụng nguyên lý chuồng bồ câu (Dirichlet): Coi 12 tháng sinh là 12 cái chuồng, 13 người là 13 chú chim bồ câu.
Bước 3: Vì 13 > 12 nên theo nguyên lý Dirichlet, chắc chắn có ít nhất một chuồng chứa từ 2 chú chim trở lên, tức là có ít nhất 2 người sinh cùng một tháng.');
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
INSERT INTO [dbo].[CLASS_SCHEDULE] (id, class_id, day_of_week, start_time, end_time, created_at, updated_at) VALUES
(1, 1, 1, '2026-09-01 17:30:00', '2026-09-01 19:00:00', '2026-08-01 08:00:00', NULL),
(2, 1, 3, '2026-09-01 17:30:00', '2026-09-01 19:00:00', '2026-08-01 08:00:00', NULL),
(3, 2, 2, '2026-09-01 18:00:00', '2026-09-01 19:30:00', '2026-08-01 08:00:00', NULL),
(4, 2, 4, '2026-09-01 18:00:00', '2026-09-01 19:30:00', '2026-08-01 08:00:00', NULL),
(5, 3, 3, '2026-09-01 19:00:00', '2026-09-01 20:30:00', '2026-08-01 08:00:00', NULL),
(6, 3, 5, '2026-09-01 19:00:00', '2026-09-01 20:30:00', '2026-08-01 08:00:00', NULL),
(7, 4, 6, '2026-09-01 08:30:00', '2026-09-01 10:00:00', '2026-08-01 08:00:00', NULL),
(8, 4, 0, '2026-09-01 08:30:00', '2026-09-01 10:00:00', '2026-08-01 08:00:00', NULL),
(9, 5, 1, '2026-09-01 10:15:00', '2026-09-01 11:45:00', '2026-08-01 08:00:00', NULL),
(10, 5, 3, '2026-09-01 10:15:00', '2026-09-01 11:45:00', '2026-08-01 08:00:00', NULL),
(11, 6, 2, '2026-09-01 14:30:00', '2026-09-01 16:00:00', '2026-08-01 08:00:00', NULL),
(12, 6, 4, '2026-09-01 14:30:00', '2026-09-01 16:00:00', '2026-08-01 08:00:00', NULL),
(13, 7, 3, '2026-09-01 17:30:00', '2026-09-01 19:00:00', '2026-08-01 08:00:00', NULL),
(14, 7, 5, '2026-09-01 17:30:00', '2026-09-01 19:00:00', '2026-08-01 08:00:00', NULL),
(15, 8, 6, '2026-09-01 18:00:00', '2026-09-01 19:30:00', '2026-08-01 08:00:00', NULL),
(16, 8, 0, '2026-09-01 18:00:00', '2026-09-01 19:30:00', '2026-08-01 08:00:00', NULL),
(17, 9, 1, '2026-09-01 19:00:00', '2026-09-01 20:30:00', '2026-08-01 08:00:00', NULL),
(18, 9, 3, '2026-09-01 19:00:00', '2026-09-01 20:30:00', '2026-08-01 08:00:00', NULL),
(19, 10, 2, '2026-09-01 08:30:00', '2026-09-01 10:00:00', '2026-08-01 08:00:00', NULL),
(20, 10, 4, '2026-09-01 08:30:00', '2026-09-01 10:00:00', '2026-08-01 08:00:00', NULL),
(21, 11, 3, '2026-09-01 10:15:00', '2026-09-01 11:45:00', '2026-08-01 08:00:00', NULL),
(22, 11, 5, '2026-09-01 10:15:00', '2026-09-01 11:45:00', '2026-08-01 08:00:00', NULL),
(23, 12, 6, '2026-09-01 14:30:00', '2026-09-01 16:00:00', '2026-08-01 08:00:00', NULL),
(24, 12, 0, '2026-09-01 14:30:00', '2026-09-01 16:00:00', '2026-08-01 08:00:00', NULL),
(25, 13, 1, '2026-09-01 17:30:00', '2026-09-01 19:00:00', '2026-08-01 08:00:00', NULL),
(26, 13, 3, '2026-09-01 17:30:00', '2026-09-01 19:00:00', '2026-08-01 08:00:00', NULL),
(27, 14, 2, '2026-09-01 18:00:00', '2026-09-01 19:30:00', '2026-08-01 08:00:00', NULL),
(28, 14, 4, '2026-09-01 18:00:00', '2026-09-01 19:30:00', '2026-08-01 08:00:00', NULL),
(29, 15, 3, '2026-09-01 19:00:00', '2026-09-01 20:30:00', '2026-08-01 08:00:00', NULL),
(30, 15, 5, '2026-09-01 19:00:00', '2026-09-01 20:30:00', '2026-08-01 08:00:00', NULL),
(31, 16, 6, '2026-09-01 08:30:00', '2026-09-01 10:00:00', '2026-08-01 08:00:00', NULL),
(32, 16, 0, '2026-09-01 08:30:00', '2026-09-01 10:00:00', '2026-08-01 08:00:00', NULL),
(33, 17, 1, '2026-09-01 10:15:00', '2026-09-01 11:45:00', '2026-08-01 08:00:00', NULL),
(34, 17, 3, '2026-09-01 10:15:00', '2026-09-01 11:45:00', '2026-08-01 08:00:00', NULL),
(35, 18, 2, '2026-09-01 14:30:00', '2026-09-01 16:00:00', '2026-08-01 08:00:00', NULL),
(36, 18, 4, '2026-09-01 14:30:00', '2026-09-01 16:00:00', '2026-08-01 08:00:00', NULL),
(37, 19, 3, '2026-09-01 17:30:00', '2026-09-01 19:00:00', '2026-08-01 08:00:00', NULL),
(38, 19, 5, '2026-09-01 17:30:00', '2026-09-01 19:00:00', '2026-08-01 08:00:00', NULL),
(39, 20, 6, '2026-09-01 18:00:00', '2026-09-01 19:30:00', '2026-08-01 08:00:00', NULL),
(40, 20, 0, '2026-09-01 18:00:00', '2026-09-01 19:30:00', '2026-08-01 08:00:00', NULL),
(41, 21, 2, '2026-09-01 18:00:00', '2026-09-01 19:30:00', '2026-08-01 08:00:00', NULL),
(42, 21, 4, '2026-09-01 18:00:00', '2026-09-01 19:30:00', '2026-08-01 08:00:00', NULL),
(43, 22, 6, '2026-09-01 09:00:00', '2026-09-01 10:30:00', '2026-08-01 08:00:00', NULL),
(44, 22, 0, '2026-09-01 09:00:00', '2026-09-01 10:30:00', '2026-08-01 08:00:00', NULL);
SET IDENTITY_INSERT [dbo].[CLASS_SCHEDULE] OFF;

-- =============================================================================
-- 15. BẢNG CLASS_STUDENT (40 lượt xếp lớp: 2 học sinh / lớp)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[CLASS_STUDENT] ON;
INSERT INTO [dbo].[CLASS_STUDENT] (id, class_id, student_id, joined_at, status) VALUES
(1, 1, 1, '2026-08-28 09:00:00', 'ACTIVE'),
(2, 1, 2, '2026-08-28 09:00:00', 'ACTIVE'),
(3, 1, 41, '2026-08-28 09:00:00', 'ACTIVE'),
(4, 1, 42, '2026-08-28 09:00:00', 'ACTIVE'),
(5, 1, 43, '2026-08-28 09:00:00', 'ACTIVE'),
(6, 1, 44, '2026-08-28 09:00:00', 'ACTIVE'),
(7, 2, 3, '2026-08-28 09:00:00', 'ACTIVE'),
(8, 2, 4, '2026-08-28 09:00:00', 'ACTIVE'),
(9, 3, 5, '2026-08-28 09:00:00', 'ACTIVE'),
(10, 3, 6, '2026-08-28 09:00:00', 'ACTIVE'),
(11, 4, 7, '2026-08-28 09:00:00', 'ACTIVE'),
(12, 4, 8, '2026-08-28 09:00:00', 'ACTIVE'),
(13, 5, 9, '2026-08-28 09:00:00', 'ACTIVE'),
(14, 5, 10, '2026-08-28 09:00:00', 'ACTIVE'),
(15, 5, 45, '2026-08-28 09:00:00', 'ACTIVE'),
(16, 5, 46, '2026-08-28 09:00:00', 'ACTIVE'),
(17, 5, 47, '2026-08-28 09:00:00', 'ACTIVE'),
(18, 6, 11, '2026-08-28 09:00:00', 'ACTIVE'),
(19, 6, 12, '2026-08-28 09:00:00', 'ACTIVE'),
(20, 7, 13, '2026-08-28 09:00:00', 'ACTIVE'),
(21, 7, 14, '2026-08-28 09:00:00', 'ACTIVE'),
(22, 8, 15, '2026-08-28 09:00:00', 'ACTIVE'),
(23, 8, 16, '2026-08-28 09:00:00', 'ACTIVE'),
(24, 9, 17, '2026-08-28 09:00:00', 'ACTIVE'),
(25, 9, 18, '2026-08-28 09:00:00', 'ACTIVE'),
(26, 10, 19, '2026-08-28 09:00:00', 'ACTIVE'),
(27, 10, 20, '2026-08-28 09:00:00', 'ACTIVE'),
(28, 11, 21, '2026-08-28 09:00:00', 'ACTIVE'),
(29, 11, 22, '2026-08-28 09:00:00', 'ACTIVE'),
(30, 12, 23, '2026-08-28 09:00:00', 'ACTIVE'),
(31, 12, 24, '2026-08-28 09:00:00', 'ACTIVE'),
(32, 13, 25, '2026-08-28 09:00:00', 'ACTIVE'),
(33, 13, 26, '2026-08-28 09:00:00', 'ACTIVE'),
(34, 14, 27, '2026-08-28 09:00:00', 'ACTIVE'),
(35, 14, 28, '2026-08-28 09:00:00', 'ACTIVE'),
(36, 15, 29, '2026-08-28 09:00:00', 'ACTIVE'),
(37, 15, 30, '2026-08-28 09:00:00', 'ACTIVE'),
(38, 16, 31, '2026-08-28 09:00:00', 'ACTIVE'),
(39, 16, 32, '2026-08-28 09:00:00', 'ACTIVE'),
(40, 17, 33, '2026-08-28 09:00:00', 'ACTIVE'),
(41, 17, 34, '2026-08-28 09:00:00', 'ACTIVE'),
(42, 18, 35, '2026-08-28 09:00:00', 'ACTIVE'),
(43, 18, 36, '2026-08-28 09:00:00', 'ACTIVE'),
(44, 19, 37, '2026-08-28 09:00:00', 'ACTIVE'),
(45, 19, 38, '2026-08-28 09:00:00', 'ACTIVE'),
(46, 20, 39, '2026-08-28 09:00:00', 'ACTIVE'),
(47, 20, 40, '2026-08-28 09:00:00', 'ACTIVE'),
(48, 21, 3, '2026-08-28 09:00:00', 'ACTIVE'),
(49, 21, 5, '2026-08-28 09:00:00', 'ACTIVE'),
(50, 21, 8, '2026-08-28 09:00:00', 'ACTIVE'),
(51, 21, 48, '2026-08-28 09:00:00', 'ACTIVE'),
(52, 21, 49, '2026-08-28 09:00:00', 'ACTIVE'),
(53, 21, 50, '2026-08-28 09:00:00', 'ACTIVE'),
(54, 22, 11, '2026-08-28 09:00:00', 'ACTIVE'),
(55, 22, 14, '2026-08-28 09:00:00', 'ACTIVE'),
(56, 22, 18, '2026-08-28 09:00:00', 'ACTIVE'),
(57, 22, 22, '2026-08-28 09:00:00', 'ACTIVE'),
(58, 22, 29, '2026-08-28 09:00:00', 'ACTIVE'),
(59, 22, 33, '2026-08-28 09:00:00', 'ACTIVE');
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
(40, 20, '2026-09-14 00:00:00', '2026-09-14 17:30:00', '2026-09-14 19:00:00', 'COMPLETED', 0),
(41, 21, '2026-09-08 00:00:00', '2026-09-08 18:00:00', '2026-09-08 19:30:00', 'COMPLETED', 1),
(42, 21, '2026-09-15 00:00:00', '2026-09-15 18:00:00', '2026-09-15 19:30:00', 'COMPLETED', 0),
(43, 22, '2026-09-12 00:00:00', '2026-09-12 09:00:00', '2026-09-12 10:30:00', 'COMPLETED', 1),
(44, 22, '2026-09-19 00:00:00', '2026-09-19 09:00:00', '2026-09-19 10:30:00', 'COMPLETED', 0);
SET IDENTITY_INSERT [dbo].[LESSON] OFF;

-- =============================================================================
-- 17. BẢNG ASSIGNMENT (25 bài tập về nhà và kiểm tra)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[ASSIGNMENT] ON;
INSERT INTO [dbo].[ASSIGNMENT] (id, lesson_id, stage_id, title, start_time, deadline, time_limit_minutes, max_attempt, status) VALUES
(1, 1, 'STAGE-SRB-01', N'BTVN Tuần 1: Luyện gảy hạt 1 đến 9 trên bàn tính Soroban', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 30, 2, 'CLOSED'),
(2, 2, 'STAGE-SRB-02', N'BTVN Tuần 2: Công thức Anh bạn nhỏ +4 và +3', '2026-09-15 08:00:00', '2026-09-20 23:59:00', 30, 2, 'CLOSED'),
(3, 3, 'STAGE-SRB-01', N'BTVN Tuần 1: Nhận diện cấu tạo bàn tính Soroban', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 25, 2, 'CLOSED'),
(4, 5, 'STAGE-SRB-03', N'Thử thách Anzan: Tính nhẩm 5 dãy số liên tiếp', '2026-09-12 08:00:00', '2026-09-18 23:59:00', 20, 1, 'CLOSED'),
(5, 7, 'STAGE-SRB-04', N'Kiểm tra định kỳ: Nhân 2 chữ số với 1 chữ số', '2026-09-20 08:00:00', '2026-09-25 23:59:00', 45, 1, 'OPEN'),
(6, 9, 'STAGE-FGM-01', N'BTVN Finger Math: Quy ước ngón cái và ngón trỏ', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 20, 3, 'CLOSED'),
(7, 11, 'STAGE-FGM-02', N'BTVN Finger Math: Phép cộng không nhớ trong phạm vi 50', '2026-09-15 08:00:00', '2026-09-20 23:59:00', 25, 2, 'CLOSED'),
(8, 13, 'STAGE-FGM-03', N'Thử thách ngón tay: Phản xạ nhanh 20 câu tính nhẩm', '2026-09-18 08:00:00', '2026-09-23 23:59:00', 15, 1, 'CLOSED'),
(9, 15, 'STAGE-MTN-01', N'Phiếu bài tập Mathnasium A1: Cấu trúc số chẵn lẻ', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 30, 2, 'CLOSED'),
(10, 17, 'STAGE-MTN-02', N'Phiếu bài tập Mathnasium B2: Nhận biết hình khối không gian', '2026-09-15 08:00:00', '2026-09-20 23:59:00', 35, 2, 'CLOSED'),
(11, 19, 'STAGE-MTN-03', N'Bài toán có lời văn: Giải bài toán gà và thỏ', '2026-09-18 08:00:00', '2026-09-24 23:59:00', 40, 1, 'OPEN'),
(12, 21, 'STAGE-KMN-01', N'Phiếu tự học Kumon 01: Quy luật dãy số tăng đều', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 20, 2, 'CLOSED'),
(13, 23, 'STAGE-KMN-02', N'Phiếu tự học Kumon 02: Tốc độ cộng trừ có nhớ', '2026-09-15 08:00:00', '2026-09-20 23:59:00', 25, 2, 'CLOSED'),
(14, 25, 'STAGE-KMN-03', N'Phiếu tự học Kumon 03: Rút gọn phân số tối giản', '2026-09-20 08:00:00', '2026-09-26 23:59:00', 30, 1, 'OPEN'),
(15, 27, 'STAGE-IMT-01', N'Tư duy phản biện: Nhận diện lập luận đúng và ngụy biện', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 30, 1, 'CLOSED'),
(16, 29, 'STAGE-IMT-02', N'Phương pháp Socrates: Tìm 3 cách giải cho 1 bài toán', '2026-09-16 08:00:00', '2026-09-21 23:59:00', 40, 1, 'CLOSED'),
(17, 31, 'STAGE-IMT-03', N'Câu đố logic: Nghịch lý thời gian và ngày trong tuần', '2026-09-22 08:00:00', '2026-09-28 23:59:00', 30, 1, 'OPEN'),
(18, 33, 'STAGE-CPA-01', N'CPA Giai đoạn 1: Đếm và nhóm vật thể thực hành', '2026-09-08 08:00:00', '2026-09-13 23:59:00', 25, 2, 'CLOSED'),
(19, 35, 'STAGE-CPA-02', N'CPA Giai đoạn 2: Vẽ sơ đồ thanh Bar Model bài toán Tổng - Tỉ', '2026-09-16 08:00:00', '2026-09-21 23:59:00', 35, 2, 'CLOSED'),
(20, 37, 'STAGE-CPA-03', N'CPA Giai đoạn 3: Lập phương trình đại số từ mô hình thanh', '2026-09-22 08:00:00', '2026-09-28 23:59:00', 40, 1, 'OPEN'),
(21, 39, 'STAGE-MTN-04', N'Hình học không gian: Thử thách cắt ghép và đối xứng', '2026-09-18 08:00:00', '2026-09-24 23:59:00', 30, 2, 'CLOSED'),
(22, 2, 'STAGE-SRB-05', N'Ôn tập giữa kỳ: Tổng hợp các công thức Anh bạn nhỏ', '2026-09-25 08:00:00', '2026-09-30 23:59:00', 45, 1, 'OPEN'),
(23, 12, 'STAGE-FGM-04', N'Kiểm tra định kỳ: Tính nhẩm 50 phép tính liên tục', '2026-09-25 08:00:00', '2026-09-30 23:59:00', 30, 1, 'OPEN'),
(24, 16, 'STAGE-MTN-05', N'Đố vui toán học: Logic bắt tay và số học vui', '2026-09-26 08:00:00', '2026-10-02 23:59:00', 25, 2, 'OPEN'),
(25, 36, 'STAGE-CPA-04', N'Bar Model nâng cao: Bài toán Hiệu - Tỉ kết hợp chuyển đổi', '2026-09-26 08:00:00', '2026-10-02 23:59:00', 40, 1, 'OPEN'),
(26, 41, 'STAGE-SRB-VIP', N'Đề thi HSG Soroban: Tốc độ gảy hạt và ảo tính Flash Anzan', '2026-09-10 08:00:00', '2026-09-20 23:59:00', 30, 2, 'OPEN'),
(27, 43, 'STAGE-IMT-PRO', N'Tư duy phản biện Socrates: Phân tích nghịch lý và bài toán thực tế', '2026-09-15 08:00:00', '2026-09-25 23:59:00', 45, 1, 'OPEN');
SET IDENTITY_INSERT [dbo].[ASSIGNMENT] OFF;

-- =============================================================================
-- 18. BẢNG INVOICE (40 hóa đơn học phí tương ứng 40 học sinh)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[INVOICE] ON;
INSERT INTO [dbo].[INVOICE] (id, parent_id, student_id, class_id, txn_ref, amount, status, payment_method, transaction_id, created_at) VALUES
(1, 1, 1, 1, 'SEED-INV-0001', 2400000, 'PAID', 'VNPAY', 'VNPAY202609010001', '2026-08-28 10:00:00'),
(2, 1, 2, 1, 'SEED-INV-0002', 2400000, 'PAID', 'VNPAY', 'VNPAY202609010002', '2026-08-28 10:00:00'),
(3, 2, 3, 2, 'SEED-INV-0003', 2400000, 'PAID', 'VNPAY', 'VNPAY202609010003', '2026-08-28 10:00:00'),
(4, 3, 4, 2, 'SEED-INV-0004', 2400000, 'PAID', 'VNPAY', 'VNPAY202609010004', '2026-08-28 10:00:00'),
(5, 4, 5, 3, 'SEED-INV-0005', 2800000, 'PAID', 'VNPAY', 'VNPAY202609010005', '2026-08-28 10:00:00'),
(6, 5, 6, 3, 'SEED-INV-0006', 2800000, 'PAID', 'VNPAY', 'VNPAY202609010006', '2026-08-28 10:00:00'),
(7, 6, 7, 4, 'SEED-INV-0007', 3200000, 'PAID', 'VNPAY', 'VNPAY202609010007', '2026-08-28 10:00:00'),
(8, 7, 8, 4, 'SEED-INV-0008', 3200000, 'PAID', 'VNPAY', 'VNPAY202609010008', '2026-08-28 10:00:00'),
(9, 8, 9, 5, 'SEED-INV-0009', 1800000, 'PAID', 'VNPAY', 'VNPAY202609010009', '2026-08-28 10:00:00'),
(10, 8, 10, 5, 'SEED-INV-0010', 1800000, 'PAID', 'VNPAY', 'VNPAY202609010010', '2026-08-28 10:00:00'),
(11, 9, 11, 6, 'SEED-INV-0011', 2200000, 'PAID', 'VNPAY', 'VNPAY202609010011', '2026-08-28 10:00:00'),
(12, 10, 12, 6, 'SEED-INV-0012', 2200000, 'PAID', 'VNPAY', 'VNPAY202609010012', '2026-08-28 10:00:00'),
(13, 11, 13, 7, 'SEED-INV-0013', 2600000, 'PAID', 'VNPAY', 'VNPAY202609010013', '2026-08-28 10:00:00'),
(14, 12, 14, 7, 'SEED-INV-0014', 2600000, 'PAID', 'VNPAY', 'VNPAY202609010014', '2026-08-28 10:00:00'),
(15, 13, 15, 8, 'SEED-INV-0015', 3000000, 'PAID', 'VNPAY', 'VNPAY202609010015', '2026-08-28 10:00:00'),
(16, 14, 16, 8, 'SEED-INV-0016', 3000000, 'PAID', 'VNPAY', 'VNPAY202609010016', '2026-08-28 10:00:00'),
(17, 15, 17, 9, 'SEED-INV-0017', 3400000, 'PAID', 'VNPAY', 'VNPAY202609010017', '2026-08-28 10:00:00'),
(18, 16, 18, 9, 'SEED-INV-0018', 3400000, 'PAID', 'VNPAY', 'VNPAY202609010018', '2026-08-28 10:00:00'),
(19, 17, 19, 10, 'SEED-INV-0019', 3800000, 'PAID', 'VNPAY', 'VNPAY202609010019', '2026-08-28 10:00:00'),
(20, 18, 20, 10, 'SEED-INV-0020', 3800000, 'PAID', 'VNPAY', 'VNPAY202609010020', '2026-08-28 10:00:00'),
(21, 19, 21, 11, 'SEED-INV-0021', 2000000, 'PAID', 'VNPAY', 'VNPAY202609010021', '2026-08-28 10:00:00'),
(22, 20, 22, 11, 'SEED-INV-0022', 2000000, 'PAID', 'VNPAY', 'VNPAY202609010022', '2026-08-28 10:00:00'),
(23, 21, 23, 12, 'SEED-INV-0023', 2400000, 'PAID', 'VNPAY', 'VNPAY202609010023', '2026-08-28 10:00:00'),
(24, 22, 24, 12, 'SEED-INV-0024', 2400000, 'PAID', 'VNPAY', 'VNPAY202609010024', '2026-08-28 10:00:00'),
(25, 23, 25, 13, 'SEED-INV-0025', 2800000, 'PAID', 'VNPAY', 'VNPAY202609010025', '2026-08-28 10:00:00'),
(26, 24, 26, 13, 'SEED-INV-0026', 2800000, 'PAID', 'VNPAY', 'VNPAY202609010026', '2026-08-28 10:00:00'),
(27, 25, 27, 14, 'SEED-INV-0027', 2600000, 'PAID', 'VNPAY', 'VNPAY202609010027', '2026-08-28 10:00:00'),
(28, 26, 28, 14, 'SEED-INV-0028', 2600000, 'PAID', 'VNPAY', 'VNPAY202609010028', '2026-08-28 10:00:00'),
(29, 27, 29, 15, 'SEED-INV-0029', 3100000, 'PAID', 'VNPAY', 'VNPAY202609010029', '2026-08-28 10:00:00'),
(30, 28, 30, 15, 'SEED-INV-0030', 3100000, 'PAID', 'VNPAY', 'VNPAY202609010030', '2026-08-28 10:00:00'),
(31, 29, 31, 16, 'SEED-INV-0031', 3600000, 'PAID', 'VNPAY', 'VNPAY202609010031', '2026-08-28 10:00:00'),
(32, 30, 32, 16, 'SEED-INV-0032', 3600000, 'PAID', 'VNPAY', 'VNPAY202609010032', '2026-08-28 10:00:00'),
(33, 31, 33, 17, 'SEED-INV-0033', 2200000, 'PENDING', 'CASH', NULL, '2026-08-28 10:00:00'),
(34, 32, 34, 17, 'SEED-INV-0034', 2200000, 'PENDING', 'CASH', NULL, '2026-08-28 10:00:00'),
(35, 2, 35, 18, 'SEED-INV-0035', 2600000, 'PENDING', 'CASH', NULL, '2026-08-28 10:00:00'),
(36, 3, 36, 18, 'SEED-INV-0036', 2600000, 'PENDING', 'CASH', NULL, '2026-08-28 10:00:00'),
(37, 5, 37, 19, 'SEED-INV-0037', 3000000, 'PENDING', 'CASH', NULL, '2026-08-28 10:00:00'),
(38, 7, 38, 19, 'SEED-INV-0038', 3000000, 'CANCELLED', NULL, NULL, '2026-08-28 10:00:00'),
(39, 11, 39, 20, 'SEED-INV-0039', 3400000, 'CANCELLED', NULL, NULL, '2026-08-28 10:00:00'),
(40, 15, 40, 20, 'SEED-INV-0040', 3400000, 'CANCELLED', NULL, NULL, '2026-08-28 10:00:00');
SET IDENTITY_INSERT [dbo].[INVOICE] OFF;

-- =============================================================================
-- 19. BẢNG ATTENDANCE (40 lượt điểm danh buổi học)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[ATTENDANCE] ON;
INSERT INTO [dbo].[ATTENDANCE] (id, lesson_id, student_id, status, teacher_notes) VALUES
(1, 1, 1, 'PRESENT', N'Bé tập trung tốt, tính nhẩm nhanh và chủ động phát biểu'),
(2, 1, 2, 'PRESENT', N'Thao tác gảy hạt Soroban chính xác, phản xạ tốt'),
(3, 1, 41, 'PRESENT', N'Bé làm quen bàn tính rất nhanh, tự tin phát biểu'),
(4, 1, 42, 'LATE', N'Đến muộn 10 phút do trời mưa, sau đó bắt nhịp tốt'),
(5, 3, 3, 'PRESENT', N'Bé đến đúng giờ, thực hiện tốt bài tập khởi động'),
(6, 3, 4, 'PRESENT', N'Bé nắm vững quy tắc Finger Math ngón tay cái'),
(7, 5, 5, 'ABSENT', N'Vắng mặt có phép (gia đình có đơn xin nghỉ)'),
(8, 5, 6, 'PRESENT', N'Bé giải được câu đố tư duy khó đầu tiên trong lớp'),
(9, 7, 7, 'PRESENT', N'Tiếp thu bài nhanh, giúp đỡ bạn cùng bàn'),
(10, 7, 8, 'LATE', N'Đến muộn 15 phút do kẹt xe giờ cao điểm'),
(11, 9, 9, 'PRESENT', N'Tích cực giơ tay xây dựng bài học Finger Math'),
(12, 9, 10, 'PRESENT', N'Làm đúng toàn bộ bài tập luyện nhanh tại lớp'),
(13, 9, 45, 'PRESENT', N'Bé phản xạ ngón tay rất linh hoạt'),
(14, 11, 11, 'PRESENT', N'Bé tập trung tốt, tính nhẩm nhanh và chủ động phát biểu'),
(15, 11, 12, 'PRESENT', N'Thao tác gảy hạt Soroban chính xác, phản xạ tốt'),
(16, 13, 13, 'PRESENT', N'Học sinh tự giác làm phiếu bài tập Kumon'),
(17, 13, 14, 'PRESENT', N'Tốc độ tính toán tiến bộ rõ rệt'),
(18, 15, 15, 'PRESENT', N'Cần rèn luyện thêm thao tác gảy hạt 5'),
(19, 15, 16, 'PRESENT', N'Bé giải được bài toán thực tế rất sáng tạo'),
(20, 17, 17, 'PRESENT', N'Tiếp thu bài nhanh, giúp đỡ bạn cùng bàn'),
(21, 19, 19, 'PRESENT', N'Tích cực giơ tay xây dựng bài học'),
(22, 19, 20, 'ABSENT', N'Vắng mặt có phép (gia đình có đơn xin nghỉ)'),
(23, 21, 21, 'PRESENT', N'Hoàn thành xuất sắc phiếu bài tập tại lớp'),
(24, 23, 23, 'PRESENT', N'Nắm vững lý thuyết phân số và tỉ số'),
(25, 41, 3, 'PRESENT', N'Ảo tính Flash Anzan cực kỳ xuất sắc, đạt điểm tuyệt đối'),
(26, 41, 5, 'PRESENT', N'Thao tác tay thuần thục, tự tin trước các dãy số dài'),
(27, 41, 8, 'PRESENT', N'Tập trung cao độ trong phần luyện tốc độ 3 giây'),
(28, 41, 48, 'PRESENT', N'Học sinh mới hòa nhập nhanh, tính nhẩm không cần nhìn bàn tính'),
(29, 41, 49, 'LATE', N'Đến muộn 5 phút do tan học muộn ở trường tiểu học'),
(30, 41, 50, 'PRESENT', N'Rất chăm chỉ và có tư duy không gian nhạy bén'),
(31, 43, 11, 'PRESENT', N'Tranh luận sôi nổi trong bài toán nghịch lý Zeno'),
(32, 43, 14, 'PRESENT', N'Chỉ ra được lỗi ngụy biện toán học rất logic'),
(33, 43, 18, 'PRESENT', N'Đặt ra nhiều câu hỏi hay mở rộng vấn đề'),
(34, 43, 22, 'PRESENT', N'Tự tin trình bày giải pháp trước cả lớp'),
(35, 43, 29, 'PRESENT', N'Vẽ sơ đồ thanh Bar Model rõ ràng, khoa học'),
(36, 43, 33, 'ABSENT', N'Vắng mặt có phép do tham gia kỳ thi cờ vua cấp quận');
SET IDENTITY_INSERT [dbo].[ATTENDANCE] OFF;

-- =============================================================================
-- 19.1. BẢNG SALARY_ADJUSTMENT (thưởng/phạt tháng 09/2026)
-- =============================================================================
INSERT INTO [dbo].[SALARY_ADJUSTMENT] (teacher_id, type, amount, reason, applied_date) VALUES
(1, N'Bonus',   500000.00, N'Thưởng hoàn thành kế hoạch chuyên môn tháng', '2026-09-30'),
(1, N'Penalty', 100000.00, N'Đi trễ một buổi học', '2026-09-14'),
(2, N'Bonus',   300000.00, N'Hỗ trợ sinh hoạt chuyên đề Finger Math', '2026-09-28'),
(3, N'Penalty', 150000.00, N'Nộp báo cáo chuyên môn trễ hạn', '2026-09-25'),
(5, N'Bonus',   450000.00, N'Được phụ huynh đánh giá xuất sắc', '2026-09-29'),
(7, N'Penalty', 100000.00, N'Đi trễ một buổi học', '2026-09-15');

-- =============================================================================
-- 19.2. BẢNG PAYROLL_SUMMARY (tổng hợp lương tháng 09/2026)
-- Tính theo Lesson COMPLETED có ít nhất một bản ghi ATTENDANCE.
-- =============================================================================
;WITH EligibleSessions AS
(
    SELECT c.teacher_id, COUNT(*) AS total_valid_sessions
    FROM [dbo].[LESSON] AS lesson
    INNER JOIN [dbo].[CLASSES] AS c ON c.id = lesson.class_id
    WHERE c.teacher_id IS NOT NULL
      AND lesson.lesson_date >= '2026-09-01'
      AND lesson.lesson_date < '2026-10-01'
      AND lesson.status = 'COMPLETED'
      AND EXISTS
      (
          SELECT 1
          FROM [dbo].[ATTENDANCE] AS attendance
          WHERE attendance.lesson_id = lesson.id
      )
    GROUP BY c.teacher_id
),
MonthlyAdjustments AS
(
    SELECT teacher_id,
           SUM(CASE WHEN type = N'Penalty' THEN amount ELSE 0 END) AS total_penalty,
           SUM(CASE WHEN type = N'Bonus' THEN amount ELSE 0 END) AS total_bonus
    FROM [dbo].[SALARY_ADJUSTMENT]
    WHERE applied_date >= '2026-09-01'
      AND applied_date < '2026-10-01'
    GROUP BY teacher_id
)
INSERT INTO [dbo].[PAYROLL_SUMMARY]
    (teacher_id, [month], [year], total_valid_sessions, base_salary,
     total_penalty, total_bonus, final_salary, email_status)
SELECT teacher.id,
       9,
       2026,
       COALESCE(sessions.total_valid_sessions, 0),
       COALESCE(sessions.total_valid_sessions, 0) * config.base_rate_per_session,
       COALESCE(adjustments.total_penalty, 0),
       COALESCE(adjustments.total_bonus, 0),
       COALESCE(sessions.total_valid_sessions, 0) * config.base_rate_per_session
           + COALESCE(adjustments.total_bonus, 0)
           - COALESCE(adjustments.total_penalty, 0),
       N'NotSent'
FROM [dbo].[TEACHER_PROFILE] AS teacher
INNER JOIN [dbo].[TEACHER_SALARY_CONFIG] AS config ON config.teacher_id = teacher.id
LEFT JOIN EligibleSessions AS sessions ON sessions.teacher_id = teacher.id
LEFT JOIN MonthlyAdjustments AS adjustments ON adjustments.teacher_id = teacher.id;

-- =============================================================================
-- 20. BẢNG LEAVE_REQUEST (20 đơn xin nghỉ học)
-- =============================================================================
SET IDENTITY_INSERT [dbo].[LEAVE_REQUEST] ON;
INSERT INTO [dbo].[LEAVE_REQUEST] (id, lesson_id, student_id, reason, status, created_at) VALUES
(1, 2, 2, N'Bé bị sốt virus, bác sĩ chỉ định nghỉ ngơi tĩnh dưỡng', 'PENDING', '2026-09-12 14:00:00'),
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
(1, 1, 1, 9.5, 'GRADED', N'Bài làm xuất sắc, phản xạ tính nhẩm nhanh và chính xác tuyệt đối!', '2026-09-12 20:15:00'),
(2, 1, 2, NULL, 'SUBMITTED', NULL, '2026-09-12 21:00:00'),
(3, 1, 41, 8.5, 'GRADED', N'Làm bài cẩn thận, trình bày sạch đẹp và đúng các bước.', '2026-09-12 20:30:00'),
(4, 1, 42, NULL, 'SUBMITTED', NULL, '2026-09-12 22:15:00'),
(5, 6, 9, 10.0, 'GRADED', N'Phản xạ Finger Math rất nhanh, nắm vững quy ước anh béo anh gầy.', '2026-09-12 19:45:00'),
(6, 6, 10, NULL, 'SUBMITTED', NULL, '2026-09-12 20:10:00'),
(7, 6, 45, 9.0, 'GRADED', N'Bé làm đúng toàn bộ các câu hỏi phản xạ ngón tay.', '2026-09-12 20:50:00'),
(8, 26, 3, 10.0, 'GRADED', N'Kỷ lục tính nhẩm mới của lớp: 10 câu Flash Anzan không sai câu nào!', '2026-09-16 19:30:00'),
(9, 26, 5, NULL, 'SUBMITTED', NULL, '2026-09-16 20:00:00'),
(10, 26, 48, NULL, 'SUBMITTED', NULL, '2026-09-16 21:15:00'),
(11, 26, 49, 8.5, 'GRADED', N'Tốc độ tốt, cần chú ý thêm ở phép tính có nhớ qua cầu 10.', '2026-09-16 20:45:00'),
(12, 27, 11, 9.5, 'GRADED', N'Lập luận sắc bén, tìm ra đúng bước ngụy biện chia cho 0.', '2026-09-18 19:00:00'),
(13, 27, 14, NULL, 'SUBMITTED', NULL, '2026-09-18 20:30:00'),
(14, 27, 18, 9.0, 'GRADED', N'Lời giải chi tiết, rõ ràng và có tính phản biện cao.', '2026-09-18 21:00:00'),
(15, 27, 22, NULL, 'SUBMITTED', NULL, '2026-09-18 22:00:00'),
(16, 2, 3, 8.5, 'GRADED', N'Cần chú ý kỹ câu hỏi đố tư duy logic, không vội vàng.', '2026-09-18 20:15:00'),
(17, 3, 5, 8.0, 'GRADED', N'Phát huy phản xạ tính nhẩm tốt hơn nữa nhé.', '2026-09-18 20:15:00'),
(18, 4, 7, 9.5, 'GRADED', N'Tư duy nhạy bén, tìm ra cách giải ngắn gọn.', '2026-09-18 20:15:00'),
(19, 9, 15, 9.0, 'GRADED', N'Nắm vững cấu trúc số và tính chất số chẵn lẻ.', '2026-09-18 20:15:00'),
(20, 15, 27, 8.5, 'GRADED', N'Phát hiện tốt lỗi sai trong lập luận toán học.', '2026-09-18 20:15:00');
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
(11, 6, 9, 1, 5.0),
(12, 6, 10, 2, 5.0),
(13, 7, 11, 1, 5.0),
(14, 7, 12, 2, 5.0),
(15, 8, 13, 1, 5.0),
(16, 8, 14, 2, 5.0),
(17, 9, 15, 1, 5.0),
(18, 9, 16, 2, 5.0),
(19, 10, 17, 1, 5.0),
(20, 10, 18, 2, 5.0),
(21, 11, 19, 1, 5.0),
(22, 11, 20, 2, 5.0),
(23, 12, 21, 1, 5.0),
(24, 12, 22, 2, 5.0),
(25, 13, 23, 1, 5.0),
(26, 13, 24, 2, 5.0),
(27, 14, 25, 1, 5.0),
(28, 14, 26, 2, 5.0),
(29, 15, 27, 1, 5.0),
(30, 15, 28, 2, 5.0),
(31, 16, 29, 1, 5.0),
(32, 16, 30, 2, 5.0),
(33, 17, 31, 1, 5.0),
(34, 18, 34, 1, 5.0),
(35, 18, 35, 2, 5.0),
(36, 19, 36, 1, 5.0),
(37, 19, 37, 2, 5.0),
(38, 20, 38, 1, 5.0),
(39, 20, 39, 2, 5.0),
(40, 26, 41, 1, 3.5),
(41, 26, 42, 2, 3.5),
(42, 26, 43, 3, 3.0),
(43, 27, 50, 1, 3.5),
(44, 27, 51, 2, 3.5),
(45, 27, 52, 3, 3.0);
SET IDENTITY_INSERT [dbo].[QUESTION_ASSIGNMENT] OFF;

    COMMIT TRANSACTION;
    PRINT N'=============================================================================';
    PRINT N'>>> SEED DATA HOÀN TẤT THÀNH CÔNG! Toàn bộ 25 bảng đã được nạp dữ liệu chuẩn.';
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
