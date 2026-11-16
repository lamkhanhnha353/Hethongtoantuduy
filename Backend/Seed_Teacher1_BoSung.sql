-- =============================================================================
-- SCRIPT BỔ SUNG DỮ LIỆU CHO GIÁO VIÊN TEST (TEACHER_PROFILE id = 1, account_id = 22)
-- =============================================================================
-- Mục tiêu:
--   1. Đảm bảo tồn tại TEACHER_PROFILE (id = 1, account_id = 22 - Thầy Giáo Test Toàn Năng)
--   2. Bổ sung 4 lớp đảm nhận tạo từ các COURSE / COURSE_LEVEL có sẵn trong CSDL:
--        - Mỗi lớp có từ 10 đến 14 học viên ACTIVE (>= 10 học viên)
--        - Mỗi lớp có từ 16 đến 24 buổi học (16 buổi hoặc 24 buổi, khớp cấp độ khóa học)
--   3. Bổ sung 4 ngân hàng câu hỏi & 36 câu hỏi bám sát Web_Frontend TeacherQuestionsView.vue
--      (đủ 5 dạng: MULTIPLE_CHOICE, TRUE_FALSE, FILL_IN, TIMED_SEQUENCE, ESSAY;
--       đủ 3 độ khó: EASY, MEDIUM, HARD) gắn vào các khóa học giảng dạy.
--   4. Bổ sung Điểm danh (ATTENDANCE + LEAVE_REQUEST) & Lương (TEACHER_SALARY_CONFIG,
--      SALARY_ADJUSTMENT, PAYROLL_SUMMARY) tuân thủ đúng quy chế:
--        * Luật tính lương: Buổi học ĐÃ BẮT ĐẦU (COMPLETED) VÀ giáo viên CÓ ĐIỂM DANH
--          trong vòng 24 giờ của buổi học.
--        * Bao gồm cả các trường hợp đối chứng:
--            - Buổi đã kết thúc > 24h nhưng quên điểm danh -> khóa & KHÔNG tính lương
--            - Buổi vừa học trong vòng 24h chưa điểm danh -> giáo viên có thể test điểm danh
--            - Buổi bị hủy (CANCELED) -> KHÔNG tính lương
--            - Các buổi tương lai (SCHEDULED) -> chưa bắt đầu, KHÔNG tính lương
--
-- Tính chất:
--   - Additive & Idempotent (an toàn khi chạy lại nhiều lần, chỉ làm mới phần [T1-BS]
--     và đồng bộ lại PAYROLL_SUMMARY của giáo viên 1, không xóa dữ liệu khác).
--   - Chạy được trên cả ToanTuDuyDb đã nạp DataSeed_Bogus_1000.sql lẫn SeedData.sql.
-- =============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- -------------------------------------------------------------------------
    -- 0. Tham số thời gian (theo giờ Việt Nam UTC+7) & kiểm tra giáo viên
    -- -------------------------------------------------------------------------
    DECLARE @NowLocal       DATETIME2(0) = CAST(DATEADD(HOUR, 7, GETUTCDATE()) AS DATETIME2(0));
    DECLARE @Today          DATE         = CAST(@NowLocal AS DATE);
    DECLARE @TeacherId      INT          = 1;
    DECLARE @AccountId      INT          = 22;
    DECLARE @BaseRate       DECIMAL(18,2) = 250000.00;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[TEACHER_PROFILE] WHERE id = @TeacherId)
    BEGIN
        IF EXISTS (SELECT 1 FROM [dbo].[TEACHER_PROFILE] WHERE account_id = @AccountId)
        BEGIN
            SELECT TOP 1 @TeacherId = id FROM [dbo].[TEACHER_PROFILE] WHERE account_id = @AccountId ORDER BY id;
        END
        ELSE
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM [dbo].[ACCOUNT] WHERE id = @AccountId)
            BEGIN
                THROW 50001, N'Không tìm thấy ACCOUNT id = 22 cho giáo viên test. Vui lòng nạp SeedData.sql hoặc DataSeed_Bogus_1000.sql trước.', 1;
            END

            SET IDENTITY_INSERT [dbo].[TEACHER_PROFILE] ON;
            INSERT INTO [dbo].[TEACHER_PROFILE] (id, account_id, full_name, email, avatar, specialize, created_at)
            VALUES (
                1,
                22,
                N'Thầy Giáo Test Toàn Năng',
                'teacher001@toantuduy.edu.vn',
                'https://api.dicebear.com/7.x/avataaars/svg?seed=teacher_22',
                N'I-Math & Phát Triển Tư Duy Phản Biện',
                '2026-05-14 18:48:13'
            );
            SET IDENTITY_INSERT [dbo].[TEACHER_PROFILE] OFF;
            SET @TeacherId = 1;
        END
    END

    IF (SELECT COUNT(*) FROM [dbo].[STUDENT_PROFILE]) < 15
    BEGIN
        THROW 50002, N'CSDL cần tối thiểu 15 học viên trong STUDENT_PROFILE để xếp lớp >= 10 học viên.', 1;
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[COURSE_LEVEL] WHERE total_sessions BETWEEN 16 AND 24 AND status = 'ACTIVE')
    BEGIN
        THROW 50003, N'CSDL chưa có COURSE_LEVEL ACTIVE nào trong khoảng 16 đến 24 buổi.', 1;
    END

    -- -------------------------------------------------------------------------
    -- 1. Dọn dữ liệu [T1-BS] cũ (nếu chạy lại script) theo đúng thứ tự FK
    -- -------------------------------------------------------------------------
    DECLARE @OldClasses TABLE (id INT PRIMARY KEY);
    INSERT INTO @OldClasses (id)
    SELECT id
    FROM [dbo].[CLASSES]
    WHERE teacher_id = @TeacherId
      AND name LIKE N'[[]T1-BS]%';

    DECLARE @OldLessons TABLE (id INT PRIMARY KEY);
    INSERT INTO @OldLessons (id)
    SELECT l.id
    FROM [dbo].[LESSON] l
    INNER JOIN @OldClasses oc ON l.class_id = oc.id;

    DECLARE @OldAssignments TABLE (id INT PRIMARY KEY);
    INSERT INTO @OldAssignments (id)
    SELECT a.id
    FROM [dbo].[ASSIGNMENT] a
    INNER JOIN @OldLessons ol ON a.lesson_id = ol.id;

    DECLARE @OldBanks TABLE (id INT PRIMARY KEY);
    INSERT INTO @OldBanks (id)
    SELECT id
    FROM [dbo].[QUESTION_BANK]
    WHERE name LIKE N'[[]T1-BS]%';

    DELETE qa
    FROM [dbo].[QUESTION_ASSIGNMENT] qa
    WHERE qa.assignment_id IN (SELECT id FROM @OldAssignments)
       OR qa.question_id IN (
            SELECT q.id
            FROM [dbo].[QUESTION] q
            INNER JOIN @OldBanks ob ON q.bank_id = ob.id
       );

    DELETE sa
    FROM [dbo].[STUDENT_ASSIGNMENT] sa
    WHERE sa.assignment_id IN (SELECT id FROM @OldAssignments);

    DELETE a
    FROM [dbo].[ASSIGNMENT] a
    INNER JOIN @OldAssignments oa ON a.id = oa.id;

    DELETE att
    FROM [dbo].[ATTENDANCE] att
    INNER JOIN @OldLessons ol ON att.lesson_id = ol.id;

    DELETE lr
    FROM [dbo].[LEAVE_REQUEST] lr
    INNER JOIN @OldLessons ol ON lr.lesson_id = ol.id;

    DELETE l
    FROM [dbo].[LESSON] l
    INNER JOIN @OldLessons ol ON l.id = ol.id;

    DELETE cs
    FROM [dbo].[CLASS_STUDENT] cs
    INNER JOIN @OldClasses oc ON cs.class_id = oc.id;

    DELETE inv
    FROM [dbo].[INVOICE] inv
    WHERE inv.txn_ref LIKE 'T1BS[_]%'
       OR inv.class_id IN (SELECT id FROM @OldClasses);

    DELETE sch
    FROM [dbo].[CLASS_SCHEDULE] sch
    INNER JOIN @OldClasses oc ON sch.class_id = oc.id;

    DELETE c
    FROM [dbo].[CLASSES] c
    INNER JOIN @OldClasses oc ON c.id = oc.id;

    DELETE q
    FROM [dbo].[QUESTION] q
    INNER JOIN @OldBanks ob ON q.bank_id = ob.id;

    DELETE qb
    FROM [dbo].[QUESTION_BANK] qb
    INNER JOIN @OldBanks ob ON qb.id = ob.id;

    DELETE adj
    FROM [dbo].[SALARY_ADJUSTMENT] adj
    WHERE adj.teacher_id = @TeacherId
      AND adj.reason LIKE N'[[]T1-BS]%';

    -- -------------------------------------------------------------------------
    -- 2. Chọn 4 khóa học & cấp độ có sẵn (16..24 buổi) từ DB
    -- -------------------------------------------------------------------------
    DECLARE @Level1Id INT, @Course1Id INT, @Sessions1 INT, @Fee1 INT;
    DECLARE @Level2Id INT, @Course2Id INT, @Sessions2 INT, @Fee2 INT;
    DECLARE @Level3Id INT, @Course3Id INT, @Sessions3 INT, @Fee3 INT;
    DECLARE @Level4Id INT, @Course4Id INT, @Sessions4 INT, @Fee4 INT;

    -- Cấp độ 1: ưu tiên I-Math 16 buổi (khớp chuyên môn của Thầy 1)
    SELECT TOP 1
        @Level1Id = cl.id,
        @Course1Id = cl.course_id,
        @Sessions1 = cl.total_sessions,
        @Fee1 = CAST(ROUND(cl.fee, 0) AS INT)
    FROM [dbo].[COURSE_LEVEL] cl
    INNER JOIN [dbo].[COURSE] c ON cl.course_id = c.id
    WHERE cl.status = 'ACTIVE'
      AND cl.total_sessions BETWEEN 16 AND 24
    ORDER BY
        CASE WHEN c.code LIKE 'IMT%' OR c.name LIKE N'%I-Math%' THEN 0 ELSE 1 END,
        CASE WHEN cl.total_sessions = 16 THEN 0 ELSE 1 END,
        cl.order_index,
        cl.id;

    -- Cấp độ 2: ưu tiên Mathnasium Suy Luận Bar Model 24 buổi
    SELECT TOP 1
        @Level2Id = cl.id,
        @Course2Id = cl.course_id,
        @Sessions2 = cl.total_sessions,
        @Fee2 = CAST(ROUND(cl.fee, 0) AS INT)
    FROM [dbo].[COURSE_LEVEL] cl
    INNER JOIN [dbo].[COURSE] c ON cl.course_id = c.id
    WHERE cl.status = 'ACTIVE'
      AND cl.total_sessions BETWEEN 16 AND 24
      AND cl.id <> @Level1Id
    ORDER BY
        CASE WHEN cl.course_id <> @Course1Id THEN 0 ELSE 1 END,
        CASE WHEN c.code = 'MTN-LOGIC' THEN 0 WHEN c.code LIKE 'MTN%' OR c.name LIKE N'%Bar Model%' OR c.name LIKE N'%Mathnasium%' THEN 1 ELSE 2 END,
        CASE WHEN cl.total_sessions = 24 THEN 0 ELSE 1 END,
        cl.order_index,
        cl.id;

    -- Cấp độ 3: ưu tiên Singapore CPA 16..20 buổi
    SELECT TOP 1
        @Level3Id = cl.id,
        @Course3Id = cl.course_id,
        @Sessions3 = cl.total_sessions,
        @Fee3 = CAST(ROUND(cl.fee, 0) AS INT)
    FROM [dbo].[COURSE_LEVEL] cl
    INNER JOIN [dbo].[COURSE] c ON cl.course_id = c.id
    WHERE cl.status = 'ACTIVE'
      AND cl.total_sessions BETWEEN 16 AND 24
      AND cl.id NOT IN (@Level1Id, @Level2Id)
    ORDER BY
        CASE WHEN cl.course_id NOT IN (@Course1Id, @Course2Id) THEN 0 ELSE 1 END,
        CASE WHEN c.code LIKE 'CPA%' OR c.name LIKE N'%Singapore%' OR c.name LIKE N'%CPA%' THEN 0 ELSE 1 END,
        CASE WHEN cl.total_sessions IN (16, 18, 20) THEN 0 ELSE 1 END,
        cl.order_index,
        cl.id;

    -- Cấp độ 4: ưu tiên Olympic SASMO & Kangaroo (fallback Soroban) 18..24 buổi
    SELECT TOP 1
        @Level4Id = cl.id,
        @Course4Id = cl.course_id,
        @Sessions4 = cl.total_sessions,
        @Fee4 = CAST(ROUND(cl.fee, 0) AS INT)
    FROM [dbo].[COURSE_LEVEL] cl
    INNER JOIN [dbo].[COURSE] c ON cl.course_id = c.id
    WHERE cl.status = 'ACTIVE'
      AND cl.total_sessions BETWEEN 16 AND 24
      AND cl.id NOT IN (@Level1Id, @Level2Id, @Level3Id)
    ORDER BY
        CASE WHEN cl.course_id NOT IN (@Course1Id, @Course2Id, @Course3Id) THEN 0 ELSE 1 END,
        CASE WHEN c.code LIKE 'OLYMP%' OR c.name LIKE N'%Olympic%' THEN 0 WHEN c.code LIKE 'SRB%' OR c.name LIKE N'%Soroban%' THEN 1 ELSE 2 END,
        CASE WHEN cl.total_sessions = 24 THEN 0 ELSE 1 END,
        cl.order_index,
        cl.id;

    IF @Level1Id IS NULL OR @Level2Id IS NULL OR @Level3Id IS NULL OR @Level4Id IS NULL
    BEGIN
        THROW 50004, N'Không đủ 4 COURSE_LEVEL ACTIVE có số buổi từ 16 đến 24 trong CSDL.', 1;
    END

    -- -------------------------------------------------------------------------
    -- 3. Bảng tạm chứa lịch & sinh ngày học khớp CLASS_SCHEDULE (0=CN..6=T7)
    -- -------------------------------------------------------------------------
    -- Công thức ((DATEPART(WEEKDAY, d) + @@DATEFIRST - 1) % 7) luôn trả về
    -- 0=Chủ Nhật, 1=Thứ 2, ..., 6=Thứ 7 độc lập với thiết lập DATEFIRST của server.
    DECLARE @TodayDow INT = ((DATEPART(WEEKDAY, @Today) + @@DATEFIRST - 1) % 7);

    -- Lớp 3 luôn có một buổi trùng thứ của hôm nay (để tạo buổi test điểm danh trong hạn 24h)
    DECLARE @C3DowB INT = @TodayDow;
    DECLARE @C3DowA INT = (@TodayDow + 6) % 7;

    DECLARE @GeneratedLessons TABLE (
        class_slot      INT NOT NULL,
        seq_no          INT NOT NULL,
        lesson_date     DATE NOT NULL,
        start_time      DATETIME2(0) NOT NULL,
        end_time        DATETIME2(0) NOT NULL,
        status          NVARCHAR(30) NOT NULL,
        is_importan     BIT NOT NULL,
        att_mode        VARCHAR(20) NOT NULL, -- VALID_24H | MISSED_24H | OPEN_24H | NONE
        title           NVARCHAR(255) NOT NULL
    );

    -- ---------- Sinh lịch Lớp 1: @Sessions1 buổi ĐÃ QUA (> 3 ngày trước) -----
    -- Lịch: Thứ 3 (2) & Thứ 5 (4), 18:00 - 19:30
    DECLARE @Dates1 TABLE (lesson_date DATE PRIMARY KEY);
    DECLARE @CursorDate DATE = DATEADD(DAY, -3, @Today);
    WHILE (SELECT COUNT(*) FROM @Dates1) < @Sessions1
    BEGIN
        IF ((DATEPART(WEEKDAY, @CursorDate) + @@DATEFIRST - 1) % 7) IN (2, 4)
            INSERT INTO @Dates1 (lesson_date) VALUES (@CursorDate);
        SET @CursorDate = DATEADD(DAY, -1, @CursorDate);
    END

    ;WITH Ordered1 AS (
        SELECT lesson_date, ROW_NUMBER() OVER (ORDER BY lesson_date ASC) AS seq_no
        FROM @Dates1
    )
    INSERT INTO @GeneratedLessons (class_slot, seq_no, lesson_date, start_time, end_time, status, is_importan, att_mode, title)
    SELECT
        1,
        seq_no,
        lesson_date,
        DATEADD(HOUR, 18, CAST(lesson_date AS DATETIME2(0))),
        DATEADD(MINUTE, 19 * 60 + 30, CAST(lesson_date AS DATETIME2(0))),
        'COMPLETED',
        CASE WHEN seq_no IN (@Sessions1 / 2, @Sessions1) THEN 1 ELSE 0 END,
        -- Buổi cuối (seq_no = @Sessions1) cố tình chưa điểm danh khi đã quá hạn 24h để đối chứng quy chế lương
        CASE WHEN seq_no = @Sessions1 THEN 'MISSED_24H' ELSE 'VALID_24H' END,
        CASE
            WHEN seq_no = @Sessions1
                THEN CONCAT(N'Buổi ', seq_no, N': Tổng kết I-Math & Đánh giá tư duy (Quá hạn 24h chưa điểm danh)')
            WHEN seq_no = @Sessions1 / 2
                THEN CONCAT(N'Buổi ', seq_no, N': Kiểm tra giữa khóa - Tư duy phản biện đa chiều')
            ELSE CONCAT(N'Buổi ', seq_no, N': I-Math Khám phá & Lập luận logic chuyên đề ', seq_no)
        END
    FROM Ordered1;

    -- ---------- Sinh lịch Lớp 2: @Sessions2 buổi (14 quá khứ + còn lại tương lai)
    -- Lịch: Thứ 2 (1) & Thứ 4 (3), 18:30 - 20:00
    DECLARE @PastCnt2 INT = 14;
    DECLARE @FutCnt2  INT = @Sessions2 - @PastCnt2;
    DECLARE @Dates2   TABLE (lesson_date DATE PRIMARY KEY);

    SET @CursorDate = DATEADD(DAY, -2, @Today);
    WHILE (SELECT COUNT(*) FROM @Dates2) < @PastCnt2
    BEGIN
        IF ((DATEPART(WEEKDAY, @CursorDate) + @@DATEFIRST - 1) % 7) IN (1, 3)
            INSERT INTO @Dates2 (lesson_date) VALUES (@CursorDate);
        SET @CursorDate = DATEADD(DAY, -1, @CursorDate);
    END

    SET @CursorDate = DATEADD(DAY, 1, @Today);
    WHILE (SELECT COUNT(*) FROM @Dates2) < @Sessions2
    BEGIN
        IF ((DATEPART(WEEKDAY, @CursorDate) + @@DATEFIRST - 1) % 7) IN (1, 3)
            INSERT INTO @Dates2 (lesson_date) VALUES (@CursorDate);
        SET @CursorDate = DATEADD(DAY, 1, @CursorDate);
    END

    ;WITH Ordered2 AS (
        SELECT lesson_date, ROW_NUMBER() OVER (ORDER BY lesson_date ASC) AS seq_no
        FROM @Dates2
    )
    INSERT INTO @GeneratedLessons (class_slot, seq_no, lesson_date, start_time, end_time, status, is_importan, att_mode, title)
    SELECT
        2,
        seq_no,
        lesson_date,
        DATEADD(MINUTE, 18 * 60 + 30, CAST(lesson_date AS DATETIME2(0))),
        DATEADD(HOUR, 20, CAST(lesson_date AS DATETIME2(0))),
        CASE WHEN seq_no <= @PastCnt2 THEN 'COMPLETED' ELSE 'SCHEDULED' END,
        CASE WHEN seq_no IN (12, @Sessions2) THEN 1 ELSE 0 END,
        CASE WHEN seq_no <= @PastCnt2 THEN 'VALID_24H' ELSE 'NONE' END,
        CONCAT(
            N'Buổi ', seq_no,
            CASE WHEN seq_no <= @PastCnt2
                 THEN N': Bar Model - Phân tích sơ đồ đoạn thẳng phần '
                 ELSE N': Bar Model - Giải toán tỉ lệ & logic phần '
            END,
            seq_no
        )
    FROM Ordered2;

    -- ---------- Sinh lịch Lớp 3: @Sessions3 buổi (6 quá khứ + 1 trong 24h hôm nay + còn lại tương lai)
    -- Lịch: @C3DowA & @C3DowB (VD: T7 & CN)
    DECLARE @PastCnt3 INT = 6;
    DECLARE @FutCnt3  INT = @Sessions3 - @PastCnt3 - 1; -- trừ 1 buổi mở 24h hôm nay
    DECLARE @Open3Start DATETIME2(0) = DATEADD(HOUR, -3, @NowLocal);
    DECLARE @Open3End   DATETIME2(0) = DATEADD(MINUTE, -90, @NowLocal);
    DECLARE @Open3Date  DATE         = CAST(@Open3Start AS DATE);

    DECLARE @Dates3Past TABLE (lesson_date DATE PRIMARY KEY);
    SET @CursorDate = DATEADD(DAY, -1, @Open3Date);
    WHILE (SELECT COUNT(*) FROM @Dates3Past) < @PastCnt3
    BEGIN
        IF ((DATEPART(WEEKDAY, @CursorDate) + @@DATEFIRST - 1) % 7) IN (@C3DowA, @C3DowB)
            INSERT INTO @Dates3Past (lesson_date) VALUES (@CursorDate);
        SET @CursorDate = DATEADD(DAY, -1, @CursorDate);
    END

    DECLARE @Dates3Fut TABLE (lesson_date DATE PRIMARY KEY);
    SET @CursorDate = DATEADD(DAY, 1, @Today);
    WHILE (SELECT COUNT(*) FROM @Dates3Fut) < @FutCnt3
    BEGIN
        IF ((DATEPART(WEEKDAY, @CursorDate) + @@DATEFIRST - 1) % 7) IN (@C3DowA, @C3DowB)
            INSERT INTO @Dates3Fut (lesson_date) VALUES (@CursorDate);
        SET @CursorDate = DATEADD(DAY, 1, @CursorDate);
    END

    ;WITH Ordered3Past AS (
        SELECT lesson_date, ROW_NUMBER() OVER (ORDER BY lesson_date ASC) AS seq_no
        FROM @Dates3Past
    )
    INSERT INTO @GeneratedLessons (class_slot, seq_no, lesson_date, start_time, end_time, status, is_importan, att_mode, title)
    SELECT
        3,
        seq_no,
        lesson_date,
        DATEADD(HOUR, 9, CAST(lesson_date AS DATETIME2(0))),
        DATEADD(MINUTE, 10 * 60 + 30, CAST(lesson_date AS DATETIME2(0))),
        'COMPLETED',
        0,
        'VALID_24H',
        CONCAT(N'Buổi ', seq_no, N': Chuẩn CPA Singapore - Học cụ & Mô hình hóa phần ', seq_no)
    FROM Ordered3Past;

    -- Buổi thứ 7: Vừa kết thúc ~1.5 giờ trước -> đang trong cửa sổ 24h, chưa chốt điểm danh
    INSERT INTO @GeneratedLessons (class_slot, seq_no, lesson_date, start_time, end_time, status, is_importan, att_mode, title)
    VALUES (
        3,
        @PastCnt3 + 1,
        @Open3Date,
        @Open3Start,
        @Open3End,
        'SCHEDULED',
        1,
        'OPEN_24H',
        CONCAT(N'Buổi ', @PastCnt3 + 1, N': Chuyên đề CPA Thực hành (Đang trong hạn điểm danh 24h)')
    );

    ;WITH Ordered3Fut AS (
        SELECT lesson_date, (@PastCnt3 + 1) + ROW_NUMBER() OVER (ORDER BY lesson_date ASC) AS seq_no
        FROM @Dates3Fut
    )
    INSERT INTO @GeneratedLessons (class_slot, seq_no, lesson_date, start_time, end_time, status, is_importan, att_mode, title)
    SELECT
        3,
        seq_no,
        lesson_date,
        DATEADD(HOUR, 9, CAST(lesson_date AS DATETIME2(0))),
        DATEADD(MINUTE, 10 * 60 + 30, CAST(lesson_date AS DATETIME2(0))),
        'SCHEDULED',
        CASE WHEN seq_no = @Sessions3 THEN 1 ELSE 0 END,
        'NONE',
        CONCAT(N'Buổi ', seq_no, N': Chuẩn CPA Singapore - Tư duy trừu tượng phần ', seq_no)
    FROM Ordered3Fut;

    -- ---------- Sinh lịch Lớp 4: @Sessions4 buổi (8 quá khứ gồm 7 COMPLETED + 1 CANCELED, còn lại tương lai)
    -- Lịch: Thứ 4 (3) & Thứ 6 (5), 19:00 - 20:30
    DECLARE @PastCnt4 INT = 8;
    DECLARE @Dates4   TABLE (lesson_date DATE PRIMARY KEY);

    SET @CursorDate = DATEADD(DAY, -2, @Today);
    WHILE (SELECT COUNT(*) FROM @Dates4) < @PastCnt4
    BEGIN
        IF ((DATEPART(WEEKDAY, @CursorDate) + @@DATEFIRST - 1) % 7) IN (3, 5)
            INSERT INTO @Dates4 (lesson_date) VALUES (@CursorDate);
        SET @CursorDate = DATEADD(DAY, -1, @CursorDate);
    END

    SET @CursorDate = DATEADD(DAY, 1, @Today);
    WHILE (SELECT COUNT(*) FROM @Dates4) < @Sessions4
    BEGIN
        IF ((DATEPART(WEEKDAY, @CursorDate) + @@DATEFIRST - 1) % 7) IN (3, 5)
            INSERT INTO @Dates4 (lesson_date) VALUES (@CursorDate);
        SET @CursorDate = DATEADD(DAY, 1, @CursorDate);
    END

    ;WITH Ordered4 AS (
        SELECT lesson_date, ROW_NUMBER() OVER (ORDER BY lesson_date ASC) AS seq_no
        FROM @Dates4
    )
    INSERT INTO @GeneratedLessons (class_slot, seq_no, lesson_date, start_time, end_time, status, is_importan, att_mode, title)
    SELECT
        4,
        seq_no,
        lesson_date,
        DATEADD(HOUR, 19, CAST(lesson_date AS DATETIME2(0))),
        DATEADD(MINUTE, 20 * 60 + 30, CAST(lesson_date AS DATETIME2(0))),
        CASE
            WHEN seq_no = 4 THEN 'CANCELED'
            WHEN seq_no <= @PastCnt4 THEN 'COMPLETED'
            ELSE 'SCHEDULED'
        END,
        CASE WHEN seq_no IN (8, @Sessions4) THEN 1 ELSE 0 END,
        CASE WHEN seq_no <= @PastCnt4 AND seq_no <> 4 THEN 'VALID_24H' ELSE 'NONE' END,
        CASE
            WHEN seq_no = 4 THEN N'Buổi 4: Luyện đề SASMO (Hủy lịch - Nghỉ sự kiện trung tâm)'
            ELSE CONCAT(N'Buổi ', seq_no, N': Chiến thuật giải nhanh Olympic SASMO & Kangaroo bài ', seq_no)
        END
    FROM Ordered4;

    -- -------------------------------------------------------------------------
    -- 4. Tạo 4 Lớp đảm nhận (CLASSES) + Lịch tuần (CLASS_SCHEDULE)
    -- -------------------------------------------------------------------------
    DECLARE @Class1Id INT, @Class2Id INT, @Class3Id INT, @Class4Id INT;
    DECLARE @C1Start DATETIME2(0), @C1End DATETIME2(0);
    DECLARE @C2Start DATETIME2(0), @C2End DATETIME2(0);
    DECLARE @C3Start DATETIME2(0), @C3End DATETIME2(0);
    DECLARE @C4Start DATETIME2(0), @C4End DATETIME2(0);

    SELECT @C1Start = CAST(MIN(lesson_date) AS DATETIME2(0)), @C1End = CAST(MAX(lesson_date) AS DATETIME2(0)) FROM @GeneratedLessons WHERE class_slot = 1;
    SELECT @C2Start = CAST(MIN(lesson_date) AS DATETIME2(0)), @C2End = CAST(MAX(lesson_date) AS DATETIME2(0)) FROM @GeneratedLessons WHERE class_slot = 2;
    SELECT @C3Start = CAST(MIN(lesson_date) AS DATETIME2(0)), @C3End = CAST(MAX(lesson_date) AS DATETIME2(0)) FROM @GeneratedLessons WHERE class_slot = 3;
    SELECT @C4Start = CAST(MIN(lesson_date) AS DATETIME2(0)), @C4End = CAST(MAX(lesson_date) AS DATETIME2(0)) FROM @GeneratedLessons WHERE class_slot = 4;

    INSERT INTO [dbo].[CLASSES] (course_level_id, teacher_id, name, max_capacity, start_date, end_date, status, created_at, updated_at)
    VALUES (@Level1Id, @TeacherId, CONCAT(N'[T1-BS] I-Math Phản Biện Đa Chiều K26A (', @Sessions1, N' buổi)'), 16, @C1Start, @C1End, 'COMPLETED', DATEADD(DAY, -7, @C1Start), @NowLocal);
    SET @Class1Id = SCOPE_IDENTITY();

    INSERT INTO [dbo].[CLASSES] (course_level_id, teacher_id, name, max_capacity, start_date, end_date, status, created_at, updated_at)
    VALUES (@Level2Id, @TeacherId, CONCAT(N'[T1-BS] Suy Luận Bar Model K26B (', @Sessions2, N' buổi)'), 18, @C2Start, @C2End, 'ACTIVE', DATEADD(DAY, -7, @C2Start), @NowLocal);
    SET @Class2Id = SCOPE_IDENTITY();

    INSERT INTO [dbo].[CLASSES] (course_level_id, teacher_id, name, max_capacity, start_date, end_date, status, created_at, updated_at)
    VALUES (@Level3Id, @TeacherId, CONCAT(N'[T1-BS] Singapore Chuẩn CPA K26C (', @Sessions3, N' buổi)'), 15, @C3Start, @C3End, 'ACTIVE', DATEADD(DAY, -7, @C3Start), @NowLocal);
    SET @Class3Id = SCOPE_IDENTITY();

    INSERT INTO [dbo].[CLASSES] (course_level_id, teacher_id, name, max_capacity, start_date, end_date, status, created_at, updated_at)
    VALUES (@Level4Id, @TeacherId, CONCAT(N'[T1-BS] Đội Tuyển Olympic SASMO K26D (', @Sessions4, N' buổi)'), 16, @C4Start, @C4End, 'ACTIVE', DATEADD(DAY, -7, @C4Start), @NowLocal);
    SET @Class4Id = SCOPE_IDENTITY();

    INSERT INTO [dbo].[CLASS_SCHEDULE] (class_id, day_of_week, start_time, end_time, room, created_at, updated_at) VALUES
    (@Class1Id, 2, '18:00:00', '19:30:00', N'P.301 - I-Math',      DATEADD(DAY, -7, @C1Start), @NowLocal),
    (@Class1Id, 4, '18:00:00', '19:30:00', N'P.301 - I-Math',      DATEADD(DAY, -7, @C1Start), @NowLocal),
    (@Class2Id, 1, '18:30:00', '20:00:00', N'P.204 - Bar Model',   DATEADD(DAY, -7, @C2Start), @NowLocal),
    (@Class2Id, 3, '18:30:00', '20:00:00', N'P.204 - Bar Model',   DATEADD(DAY, -7, @C2Start), @NowLocal),
    (@Class3Id, @C3DowA, '09:00:00', '10:30:00', N'P.105 - Singapore CPA', DATEADD(DAY, -7, @C3Start), @NowLocal),
    (@Class3Id, @C3DowB, '09:00:00', '10:30:00', N'P.105 - Singapore CPA', DATEADD(DAY, -7, @C3Start), @NowLocal),
    (@Class4Id, 3, '19:00:00', '20:30:00', N'P.402 - Olympic',     DATEADD(DAY, -7, @C4Start), @NowLocal),
    (@Class4Id, 5, '19:00:00', '20:30:00', N'P.402 - Olympic',     DATEADD(DAY, -7, @C4Start), @NowLocal);

    -- -------------------------------------------------------------------------
    -- 5. Tạo Học viên cho từng lớp (>= 10 học viên/lớp: 12, 14, 10, 11) + Hóa đơn PAID
    -- -------------------------------------------------------------------------
    -- Lấy tối đa 47 học viên ACTIVE hiện có; nếu CSDL ít học viên (ví dụ SeedData.sql
    -- chỉ có 15 học viên) thì xoay vòng modulo giữa các lớp nhưng đảm bảo trong
    -- cùng 1 lớp KHÔNG trùng học viên.
    DECLARE @StudentPool TABLE (
        rn          INT IDENTITY(1,1) PRIMARY KEY,
        student_id  INT NOT NULL,
        parent_id   INT NOT NULL
    );
    INSERT INTO @StudentPool (student_id, parent_id)
    SELECT TOP 50 sp.id, sp.parent_id
    FROM [dbo].[STUDENT_PROFILE] sp
    WHERE sp.is_active = 1
    ORDER BY sp.id;

    DECLARE @PoolCount INT = (SELECT COUNT(*) FROM @StudentPool);
    IF @PoolCount < 14
    BEGIN
        INSERT INTO @StudentPool (student_id, parent_id)
        SELECT TOP 50 sp.id, sp.parent_id
        FROM [dbo].[STUDENT_PROFILE] sp
        WHERE sp.id NOT IN (SELECT student_id FROM @StudentPool)
        ORDER BY sp.id;
        SET @PoolCount = (SELECT COUNT(*) FROM @StudentPool);
    END

    IF @PoolCount < 14
    BEGIN
        THROW 50005, N'Cần ít nhất 14 học viên trong STUDENT_PROFILE để gán lớp tối đa 14 học viên.', 1;
    END

    DECLARE @EnrollmentSpec TABLE (
        class_id    INT NOT NULL,
        student_id  INT NOT NULL,
        parent_id   INT NOT NULL,
        fee_amount  INT NOT NULL,
        paid_at     DATETIME2(0) NOT NULL,
        PRIMARY KEY (class_id, student_id)
    );

    -- Lớp 1: 12 học viên (offset 0)
    INSERT INTO @EnrollmentSpec (class_id, student_id, parent_id, fee_amount, paid_at)
    SELECT @Class1Id, sp.student_id, sp.parent_id, @Fee1, DATEADD(DAY, -4, @C1Start)
    FROM @StudentPool sp
    WHERE sp.rn BETWEEN 1 AND 12;

    -- Lớp 2: 14 học viên (offset 12 nếu đủ học viên, ngược lại quay vòng từ đầu)
    INSERT INTO @EnrollmentSpec (class_id, student_id, parent_id, fee_amount, paid_at)
    SELECT TOP 14
        @Class2Id,
        sp.student_id,
        sp.parent_id,
        @Fee2,
        DATEADD(DAY, -4, @C2Start)
    FROM @StudentPool sp
    ORDER BY CASE WHEN sp.rn > 12 THEN 0 ELSE 1 END, sp.rn;

    -- Lớp 3: 10 học viên
    INSERT INTO @EnrollmentSpec (class_id, student_id, parent_id, fee_amount, paid_at)
    SELECT TOP 10
        @Class3Id,
        sp.student_id,
        sp.parent_id,
        @Fee3,
        DATEADD(DAY, -4, @C3Start)
    FROM @StudentPool sp
    ORDER BY CASE WHEN sp.rn > 26 THEN 0 ELSE 1 END, sp.rn;

    -- Lớp 4: 11 học viên
    INSERT INTO @EnrollmentSpec (class_id, student_id, parent_id, fee_amount, paid_at)
    SELECT TOP 11
        @Class4Id,
        sp.student_id,
        sp.parent_id,
        @Fee4,
        DATEADD(DAY, -4, @C4Start)
    FROM @StudentPool sp
    ORDER BY CASE WHEN sp.rn > 36 THEN 0 ELSE 1 END, sp.rn;

    DECLARE @InsertedInvoices TABLE (
        invoice_id  INT NOT NULL,
        class_id    INT NOT NULL,
        student_id  INT NOT NULL,
        paid_at     DATETIME2(0) NOT NULL
    );

    INSERT INTO [dbo].[INVOICE]
        (parent_id, student_id, class_id, txn_ref, amount, status, payment_method, transaction_id, created_at, expired_at, paid_at)
    OUTPUT INSERTED.id, INSERTED.class_id, INSERTED.student_id, INSERTED.paid_at
    INTO @InsertedInvoices (invoice_id, class_id, student_id, paid_at)
    SELECT
        e.parent_id,
        e.student_id,
        e.class_id,
        CONCAT('T1BS_', e.class_id, '_', e.student_id),
        e.fee_amount,
        'PAID',
        'VNPAY',
        CONCAT('VNP_T1BS_', e.class_id, '_', e.student_id),
        DATEADD(MINUTE, -15, e.paid_at),
        e.paid_at,
        e.paid_at
    FROM @EnrollmentSpec e;

    INSERT INTO [dbo].[CLASS_STUDENT] (class_id, student_id, invoice_id, joined_at, status)
    SELECT
        inv.class_id,
        inv.student_id,
        inv.invoice_id,
        inv.paid_at,
        'ACTIVE'
    FROM @InsertedInvoices inv;

    -- -------------------------------------------------------------------------
    -- 6. Chèn 80 Buổi học (LESSON) & giữ ánh xạ lesson_id <-> seq_no / att_mode
    -- -------------------------------------------------------------------------
    DECLARE @InsertedLessons TABLE (
        lesson_id   INT NOT NULL,
        class_id    INT NOT NULL,
        lesson_date DATE NOT NULL,
        status      NVARCHAR(30) NOT NULL,
        title       NVARCHAR(255) NOT NULL
    );

    INSERT INTO [dbo].[LESSON] (class_id, title, lesson_date, start_time, end_time, status, is_importan)
    OUTPUT INSERTED.id, INSERTED.class_id, INSERTED.lesson_date, INSERTED.status, INSERTED.title
    INTO @InsertedLessons (lesson_id, class_id, lesson_date, status, title)
    SELECT
        CASE gl.class_slot
            WHEN 1 THEN @Class1Id
            WHEN 2 THEN @Class2Id
            WHEN 3 THEN @Class3Id
            ELSE @Class4Id
        END,
        gl.title,
        gl.lesson_date,
        gl.start_time,
        gl.end_time,
        gl.status,
        gl.is_importan
    FROM @GeneratedLessons gl
    ORDER BY gl.class_slot, gl.seq_no;

    -- -------------------------------------------------------------------------
    -- 7. Chèn Điểm danh (ATTENDANCE) & Đơn xin nghỉ phép (LEAVE_REQUEST)
    --    CHỈ chèn cho các buổi att_mode = 'VALID_24H' (buổi đã bắt đầu, COMPLETED,
    --    giáo viên đã điểm danh trong vòng 24h của buổi học).
    -- -------------------------------------------------------------------------
    ;WITH TargetLessons AS (
        SELECT
            il.lesson_id,
            il.class_id,
            il.lesson_date,
            gl.seq_no
        FROM @InsertedLessons il
        INNER JOIN @GeneratedLessons gl
            ON gl.class_slot = CASE il.class_id
                                    WHEN @Class1Id THEN 1
                                    WHEN @Class2Id THEN 2
                                    WHEN @Class3Id THEN 3
                                    ELSE 4
                               END
           AND gl.lesson_date = il.lesson_date
           AND gl.title = il.title
        WHERE gl.att_mode = 'VALID_24H'
    ),
    LessonStudents AS (
        SELECT
            tl.lesson_id,
            tl.class_id,
            tl.lesson_date,
            tl.seq_no,
            cs.student_id,
            ROW_NUMBER() OVER (PARTITION BY tl.lesson_id ORDER BY cs.student_id) AS st_idx
        FROM TargetLessons tl
        INNER JOIN [dbo].[CLASS_STUDENT] cs
            ON cs.class_id = tl.class_id
           AND cs.status = 'ACTIVE'
    )
    INSERT INTO [dbo].[ATTENDANCE] (lesson_id, student_id, status, teacher_notes)
    SELECT
        ls.lesson_id,
        ls.student_id,
        CASE
            WHEN ((ls.seq_no * 7 + ls.st_idx) % 23) = 0 THEN 'ABSENT'
            WHEN ((ls.seq_no * 5 + ls.st_idx) % 17) = 0 THEN 'EXCUSED'
            WHEN ((ls.seq_no * 3 + ls.st_idx) % 13) = 0 THEN 'LATE'
            ELSE 'PRESENT'
        END,
        CASE
            WHEN ((ls.seq_no * 7 + ls.st_idx) % 23) = 0
                THEN N'Nghỉ không phép, giáo viên đã nhắn tin nhắc phụ huynh xem lại bài.'
            WHEN ((ls.seq_no * 5 + ls.st_idx) % 17) = 0
                THEN N'Có đơn xin nghỉ phép đã duyệt, sẽ gửi tài liệu tự ôn buổi kế tiếp.'
            WHEN ((ls.seq_no * 3 + ls.st_idx) % 13) = 0
                THEN N'Đến trễ khoảng 10 phút nhưng nhanh chóng bắt nhịp bài tập tư duy.'
            WHEN ls.st_idx % 4 = 1
                THEN N'Tích cực giơ tay phát biểu và giải đúng câu hỏi nâng cao trên bảng.'
            ELSE NULL
        END
    FROM LessonStudents ls;

    -- Bổ sung LEAVE_REQUEST APPROVED khớp các học sinh được điểm danh EXCUSED
    INSERT INTO [dbo].[LEAVE_REQUEST] (student_id, lesson_id, reason, status, created_at)
    SELECT
        att.student_id,
        att.lesson_id,
        N'[T1-BS] Phụ huynh xin phép cho bé nghỉ vì lịch kiểm tra học kỳ / khám sức khỏe định kỳ.',
        'APPROVED',
        DATEADD(HOUR, -18, CAST(l.lesson_date AS DATETIME2(0)))
    FROM [dbo].[ATTENDANCE] att
    INNER JOIN @InsertedLessons l ON l.lesson_id = att.lesson_id
    WHERE att.status = 'EXCUSED';

    -- -------------------------------------------------------------------------
    -- 8. Bổ sung 4 Ngân hàng câu hỏi & 36 Câu hỏi chuẩn TeacherQuestionsView.vue
    -- -------------------------------------------------------------------------
    DECLARE @Bank1Id INT, @Bank2Id INT, @Bank3Id INT, @Bank4Id INT;

    INSERT INTO [dbo].[QUESTION_BANK] (course_id, name, description)
    VALUES (@Course1Id, N'[T1-BS] Ngân hàng I-Math: Tư duy Đa chiều & Phản biện K26',
            N'Bộ câu hỏi chuẩn I-Math gồm Trắc nghiệm 4 phương án, Đúng/Sai, Điền khuyết, Tính nhẩm theo nhịp và Tự luận phản biện.');
    SET @Bank1Id = SCOPE_IDENTITY();

    INSERT INTO [dbo].[QUESTION_BANK] (course_id, name, description)
    VALUES (@Course2Id, N'[T1-BS] Ngân hàng Mathnasium: Sơ đồ Bar Model & Cấu trúc số',
            N'Bộ câu hỏi trực quan hóa bài toán tổng-hiệu-tỉ bằng thanh Bar Model theo trình tự Dễ/Trung bình/Khó.');
    SET @Bank2Id = SCOPE_IDENTITY();

    INSERT INTO [dbo].[QUESTION_BANK] (course_id, name, description)
    VALUES (@Course3Id, N'[T1-BS] Ngân hàng Singapore CPA: Cụ thể - Hình ảnh - Trừu tượng',
            N'Luyện tập tư duy Singapore CPA kết hợp suy luận trực quan, điền số và giải thích bước lập luận.');
    SET @Bank3Id = SCOPE_IDENTITY();

    INSERT INTO [dbo].[QUESTION_BANK] (course_id, name, description)
    VALUES (@Course4Id, N'[T1-BS] Ngân hàng Olympic SASMO & Phản xạ Tính nhẩm K26',
            N'Tuyển chọn bài tập dãy số quy luật, tính nhẩm theo nhịp tốc độ và toán đố Olympic nâng cao.');
    SET @Bank4Id = SCOPE_IDENTITY();

    -- 36 Câu hỏi (9 câu / ngân hàng), định dạng đúng hệt buildMultipleChoiceContent / buildMultipleChoiceAnswer / TRUE_FALSE / FILL_IN / TIMED_SEQUENCE / ESSAY trong TeacherQuestionsView.vue
    INSERT INTO [dbo].[QUESTION] (bank_id, type, content, image_url, difficulty, answer) VALUES
    -- ===== Ngân hàng 1: I-Math Tư duy Đa chiều & Phản biện =====
    (@Bank1Id, 'MULTIPLE_CHOICE',
     N'Một phép biến đổi số học thực hiện bước 1 nhân đôi số ban đầu, bước 2 cộng thêm 6 rồi chia cho 2. Kết quả cuối cùng luôn lớn hơn số ban đầu bao nhiêu đơn vị?
A. 3 đơn vị
B. 6 đơn vị
C. 2 đơn vị
D. Phụ thuộc vào số ban đầu',
     NULL, 'EASY',
     N'A. 3 đơn vị
- Lời giải: Gọi số ban đầu là x. Ta có (2x + 6) : 2 = x + 3, nên luôn lớn hơn số ban đầu đúng 3 đơn vị.'),

    (@Bank1Id, 'MULTIPLE_CHOICE',
     N'Cho dãy quy luật: 2, 6, 12, 20, 30, (...). Số thích hợp điền vào dấu ba chấm là số nào?
A. 38
B. 40
C. 42
D. 44',
     NULL, 'MEDIUM',
     N'C. 42
- Lời giải: Hiệu giữa hai số liên tiếp tăng dần +4, +6, +8, +10, nên tiếp theo cộng +12: 30 + 12 = 42.'),

    (@Bank1Id, 'MULTIPLE_CHOICE',
     N'Trong một cuộc tranh luận toán học, bạn Minh phát biểu: "Mọi số chia hết cho 4 và 6 thì đều chia hết cho 24". Phát biểu phản biện nào sau đây chứng minh Minh sai?
A. Số 12 chia hết cho 4 và 6 nhưng không chia hết cho 24
B. Số 24 chia hết cho cả 4, 6 và 24
C. Số 36 không chia hết cho 6
D. Số 48 chỉ chia hết cho 4',
     NULL, 'HARD',
     N'A. Số 12 chia hết cho 4 và 6 nhưng không chia hết cho 24
- Lời giải: Bội chung nhỏ nhất của 4 và 6 là 12 (không phải 24). Số 12 là phản ví dụ bác bỏ khẳng định của Minh.'),

    (@Bank1Id, 'TRUE_FALSE',
     N'Khẳng định: "Tổng của hai số lẻ bất kỳ luôn là một số chẵn và chia hết cho 4."',
     NULL, 'MEDIUM',
     N'Sai
- Giải thích: Tổng hai số lẻ luôn chẵn nhưng không nhất thiết chia hết cho 4 (ví dụ: 3 + 3 = 6 không chia hết cho 4).'),

    (@Bank1Id, 'TRUE_FALSE',
     N'Khẳng định: "Nếu a là số tự nhiên lẻ thì tích a x (a + 1) luôn là số chẵn."',
     NULL, 'EASY',
     N'Đúng
- Giải thích: Trong hai số tự nhiên liên tiếp luôn có một số chẵn nên tích của chúng luôn chia hết cho 2.'),

    (@Bank1Id, 'FILL_IN',
     N'Tìm số tự nhiên nhỏ nhất có 2 chữ số sao cho khi chia cho 3 dư 1 và khi chia cho 5 dư 1.',
     NULL, 'MEDIUM',
     N'16
- Hướng dẫn giải: Số cần tìm trừ đi 1 sẽ là bội chung của 3 và 5 (bằng 15, 30,...). Với hai chữ số nhỏ nhất là 15 + 1 = 16.'),

    (@Bank1Id, 'TIMED_SEQUENCE',
     N'Tính nhẩm phản xạ theo nhịp (mỗi số 1 giây): 15 -> +27 -> -14 -> +32 -> -19 = ?',
     NULL, 'EASY',
     N'41
- Hướng dẫn giải: Cộng dồn từng nhịp: 15 + 27 = 42; 42 - 14 = 28; 28 + 32 = 60; 60 - 19 = 41.'),

    (@Bank1Id, 'TIMED_SEQUENCE',
     N'Tính nhẩm đa bước tốc độ (1.2 giây/nhịp): 48 -> +36 -> -29 -> +45 -> -18 = ?',
     NULL, 'HARD',
     N'82
- Hướng dẫn giải: 48 + 36 = 84; 84 - 29 = 55; 55 + 45 = 100; 100 - 18 = 82.'),

    (@Bank1Id, 'ESSAY',
     N'Bài toán ngụy biện: Một bạn chứng minh 2 = 1 từ giả sử a = b => a^2 = ab => a^2 - b^2 = ab - b^2 => (a-b)(a+b) = b(a-b) => a+b = b => 2b = b => 2 = 1. Hãy chỉ ra bước sai cốt lõi và giải thích lý do.',
     NULL, 'HARD',
     N'- Tiêu chí chấm & Lời giải chuẩn:
+ Bước 1 (0.5đ): Xác định chính xác bước rút gọn hai vế cho (a - b) để suy ra a + b = b là bước sai.
+ Bước 2 (1.0đ): Giải thích vì giả thiết ban đầu a = b nên (a - b) = 0; phép chia hai vế cho 0 không hợp lệ trong toán học.
+ Bước 3 (0.5đ): Kết luận phép biến đổi tương đương chỉ đúng khi nhân tử rút gọn khác 0.'),

    -- ===== Ngân hàng 2: Mathnasium Bar Model & Cấu trúc số =====
    (@Bank2Id, 'MULTIPLE_CHOICE',
     N'Tổng số bi của An và Bình là 54 viên. Biết số bi của An gấp đôi số bi của Bình. Hỏi An có nhiều hơn Bình bao nhiêu viên bi?
A. 18 viên
B. 36 viên
C. 27 viên
D. 12 viên',
     NULL, 'EASY',
     N'A. 18 viên
- Lời giải: Vẽ Bar Model gồm 3 phần bằng nhau (An 2 phần, Bình 1 phần). Giá trị 1 phần = 54 : 3 = 18 viên, cũng chính là hiệu số bi.'),

    (@Bank2Id, 'MULTIPLE_CHOICE',
     N'Hai kệ sách có tổng cộng 96 quyển. Nếu chuyển 12 quyển từ kệ A sang kệ B thì số sách ở hai kệ bằng nhau. Ban đầu kệ A có bao nhiêu quyển?
A. 48 quyển
B. 54 quyển
C. 60 quyển
D. 72 quyển',
     NULL, 'MEDIUM',
     N'C. 60 quyển
- Lời giải: Khi chuyển 12 quyển thì bằng nhau => kệ A hơn kệ B là 12 x 2 = 24 quyển. Kệ A = (96 + 24) : 2 = 60 quyển.'),

    (@Bank2Id, 'MULTIPLE_CHOICE',
     N'Một cửa hàng bán 3/5 số gạo trong ngày đầu và 1/4 số gạo còn lại vào ngày thứ hai thì còn đúng 30 kg. Ban đầu cửa hàng có bao nhiêu kg gạo?
A. 80 kg
B. 100 kg
C. 120 kg
D. 150 kg',
     NULL, 'HARD',
     N'B. 100 kg
- Lời giải: 30 kg ứng với 3/4 phần còn lại sau ngày 1 => sau ngày 1 còn 40 kg (bằng 2/5 ban đầu). Ban đầu có 40 : 2 x 5 = 100 kg.'),

    (@Bank2Id, 'TRUE_FALSE',
     N'Khẳng định: "Nếu thêm 8 đơn vị vào cả số bị trừ và số trừ thì hiệu của hai số tăng thêm 16 đơn vị."',
     NULL, 'EASY',
     N'Sai
- Giải thích: Khi cùng tăng (hoặc cùng giảm) cả số bị trừ và số trừ một lượng bằng nhau thì hiệu không đổi.'),

    (@Bank2Id, 'TRUE_FALSE',
     N'Khẳng định: "Trong sơ đồ Bar Model, nếu đoạn của Lan dài bằng 3/4 đoạn của Mai thì tỉ số phần hơn của Mai so với tổng hai bạn là 1/7."',
     NULL, 'MEDIUM',
     N'Đúng
- Giải thích: Lan chiếm 3 phần, Mai chiếm 4 phần => tổng 7 phần và Mai hơn Lan 4 - 3 = 1 phần, tức chiếm 1/7 tổng số.'),

    (@Bank2Id, 'FILL_IN',
     N'Hiệu hai số là 35. Nếu giữ nguyên số trừ và tăng số bị trừ thêm 19 đơn vị thì hiệu mới bằng bao nhiêu?',
     NULL, 'EASY',
     N'54
- Hướng dẫn giải: Tăng số bị trừ thêm 19 đơn vị làm hiệu tăng tương ứng: 35 + 19 = 54.'),

    (@Bank2Id, 'TIMED_SEQUENCE',
     N'Cộng trừ tròn chục nhanh theo nhịp: 25 -> +35 -> +18 -> -28 -> +40 = ?',
     NULL, 'EASY',
     N'90
- Hướng dẫn giải: Ghép cặp tròn chục: 25 + 35 = 60; 60 + 18 = 78; 78 - 28 = 50; 50 + 40 = 90.'),

    (@Bank2Id, 'TIMED_SEQUENCE',
     N'Tính nhẩm cấu trúc số (nhịp 1 giây): 120 -> -45 -> +25 -> +64 -> -34 = ?',
     NULL, 'MEDIUM',
     N'130
- Hướng dẫn giải: 120 - 45 = 75; 75 + 25 = 100; 100 + 64 = 164; 164 - 34 = 130.'),

    (@Bank2Id, 'ESSAY',
     N'Mẹ hơn con 28 tuổi. Sau 4 năm nữa, tuổi mẹ gấp 5 lần tuổi con. Hãy vẽ mô tả sơ đồ Bar Model và tính tuổi hiện nay của mỗi người.',
     NULL, 'HARD',
     N'- Tiêu chí chấm & Lời giải chuẩn:
+ Bước 1 (0.5đ): Nhận xét hiệu số tuổi không đổi theo thời gian (sau 4 năm mẹ vẫn hơn con 28 tuổi).
+ Bước 2 (0.5đ): Sau 4 năm, mẹ 5 phần, con 1 phần => 4 phần ứng với 28 tuổi.
+ Bước 3 (0.5đ): Tuổi con sau 4 năm là 28 : 4 = 7 tuổi => hiện nay con 7 - 4 = 3 tuổi.
+ Bước 4 (0.5đ): Tuổi mẹ hiện nay là 3 + 28 = 31 tuổi.'),

    -- ===== Ngân hàng 3: Singapore CPA =====
    (@Bank3Id, 'MULTIPLE_CHOICE',
     N'Có 4 khối lập phương giống hệt nhau xếp thành hàng ngang sát nhau. Hỏi có tất cả bao nhiêu mặt ngoài nhìn thấy được (không tính mặt đáy áp sát mặt bàn)?
A. 14 mặt
B. 16 mặt
C. 18 mặt
D. 20 mặt',
     NULL, 'MEDIUM',
     N'A. 14 mặt
- Lời giải: Mỗi khối có 3 mặt lộ (trên, trước, sau) => 4 x 3 = 12 mặt, cộng thêm 2 mặt ở hai đầu trái-phải: 12 + 2 = 14 mặt.'),

    (@Bank3Id, 'MULTIPLE_CHOICE',
     N'Dùng phương pháp tách số tròn trăm (Number Bond) để tính nhanh 298 + 147 = ?
A. 435
B. 445
C. 455
D. 443',
     NULL, 'EASY',
     N'B. 445
- Lời giải: Tách 147 = 2 + 145 => (298 + 2) + 145 = 300 + 145 = 445.'),

    (@Bank3Id, 'MULTIPLE_CHOICE',
     N'Một chiếc cân đĩa thăng bằng khi bên trái đặt 2 quả táo và 1 quả lê, bên phải đặt 1 quả táo và 3 quả cam. Biết 1 quả lê nặng bằng 2 quả cam. Hỏi 1 quả táo nặng bằng mấy quả cam?
A. 1 quả cam
B. 2 quả cam
C. 3 quả cam
D. 4 quả cam',
     NULL, 'HARD',
     N'A. 1 quả cam
- Lời giải: Bỏ 1 quả táo ở hai bên: 1 táo + 1 lê = 3 cam. Thay 1 lê = 2 cam => 1 táo + 2 cam = 3 cam => 1 táo = 1 cam.'),

    (@Bank3Id, 'TRUE_FALSE',
     N'Khẳng định: "Phép tính 37 x 99 cho kết quả đúng bằng 3700 - 37 = 3663."',
     NULL, 'EASY',
     N'Đúng
- Giải thích: Theo quy tắc phân phối CPA: 37 x (100 - 1) = 3700 - 37 = 3663.'),

    (@Bank3Id, 'TRUE_FALSE',
     N'Khẳng định: "Chu vi của một hình chữ nhật tăng gấp 4 lần nếu tăng cả chiều dài và chiều rộng lên gấp đôi."',
     NULL, 'MEDIUM',
     N'Sai
- Giải thích: Chu vi = 2 x (dài + rộng). Khi gấp đôi dài và rộng thì chu vi chỉ tăng gấp 2 lần (diện tích mới tăng gấp 4 lần).'),

    (@Bank3Id, 'FILL_IN',
     N'Áp dụng quy tắc bù trừ tìm số còn thiếu: 465 + 297 = 462 + (...).',
     NULL, 'EASY',
     N'300
- Hướng dẫn giải: Bớt 3 từ 465 (thành 462) chuyển sang 297 được 297 + 3 = 300.'),

    (@Bank3Id, 'TIMED_SEQUENCE',
     N'Chuỗi Number Bond (1 giây/bước): 18 -> +12 -> x3 -> -15 -> :5 = ?',
     NULL, 'MEDIUM',
     N'15
- Hướng dẫn giải: 18 + 12 = 30; 30 x 3 = 90; 90 - 15 = 75; 75 : 5 = 15.'),

    (@Bank3Id, 'TIMED_SEQUENCE',
     N'Tính nhẩm liên hoàn Singapore: 64 -> :2 -> +18 -> x2 -> -25 = ?',
     NULL, 'EASY',
     N'75
- Hướng dẫn giải: 64 : 2 = 32; 32 + 18 = 50; 50 x 2 = 100; 100 - 25 = 75.'),

    (@Bank3Id, 'ESSAY',
     N'Một bể cá hình hộp chữ nhật có chu vi đáy 140 cm, chiều dài hơn chiều rộng 10 cm. Hãy trình bày 3 bước chuyển từ mô hình hình ảnh sang biểu thức tính diện tích đáy của bể cá.',
     NULL, 'HARD',
     N'- Tiêu chí chấm & Lời giải chuẩn:
+ Bước 1 (0.5đ): Tính nửa chu vi đáy = chiều dài + chiều rộng = 140 : 2 = 70 cm.
+ Bước 2 (0.75đ): Tìm kích thước: chiều rộng = (70 - 10) : 2 = 30 cm; chiều dài = 30 + 10 = 40 cm.
+ Bước 3 (0.75đ): Diện tích đáy = 40 x 30 = 1200 cm2.'),

    -- ===== Ngân hàng 4: Olympic SASMO & Kangaroo =====
    (@Bank4Id, 'MULTIPLE_CHOICE',
     N'Tính tổng dãy số cách đều: S = 1 + 4 + 7 + 10 + ... + 28.
A. 135
B. 145
C. 150
D. 155',
     NULL, 'MEDIUM',
     N'B. 145
- Lời giải: Số số hạng = (28 - 1) : 3 + 1 = 10 số. Tổng S = (1 + 28) x 10 : 2 = 145.'),

    (@Bank4Id, 'MULTIPLE_CHOICE',
     N'Có 5 bạn bắt tay nhau đôi một một lần. Hỏi có tất cả bao nhiêu cái bắt tay?
A. 8
B. 10
C. 12
D. 20',
     NULL, 'EASY',
     N'B. 10
- Lời giải: Mỗi bạn bắt tay 4 bạn còn lại, mỗi cái bắt tay được đếm 2 lần => 5 x 4 : 2 = 10 cái bắt tay.'),

    (@Bank4Id, 'MULTIPLE_CHOICE',
     N'Chữ số tận cùng của tích A = 3 x 3 x 3 x ... x 3 (gồm 2026 thừa số 3) là chữ số nào?
A. 1
B. 3
C. 7
D. 9',
     NULL, 'HARD',
     N'D. 9
- Lời giải: Chu kỳ tận cùng của lũy thừa 3 là (3, 9, 7, 1) độ dài 4. Ta có 2026 chia 4 dư 2 => chữ số tận cùng là 9.'),

    (@Bank4Id, 'TRUE_FALSE',
     N'Khẳng định: "Trong 13 bạn bất kỳ trong lớp học, chắc chắn có ít nhất 2 bạn sinh cùng một tháng."',
     NULL, 'EASY',
     N'Đúng
- Giải thích: Theo nguyên lý Dirichlet (chuồng bồ câu), 1 năm có 12 tháng nên với 13 bạn chắc chắn có >= 2 bạn cùng tháng sinh.'),

    (@Bank4Id, 'TRUE_FALSE',
     N'Khẳng định: "Số 2026 có tổng các chữ số chia hết cho 3 nên chia hết cho 9."',
     NULL, 'MEDIUM',
     N'Sai
- Giải thích: Tổng các chữ số của 2026 là 2 + 0 + 2 + 6 = 10, không chia hết cho 3 và cũng không chia hết cho 9.'),

    (@Bank4Id, 'FILL_IN',
     N'Có bao nhiêu số có 2 chữ số chia hết cho 5?',
     NULL, 'EASY',
     N'18
- Hướng dẫn giải: Dãy gồm 10, 15, ..., 95. Số lượng = (95 - 10) : 5 + 1 = 18 số.'),

    (@Bank4Id, 'TIMED_SEQUENCE',
     N'Phản xạ tính nhanh Olympic (nhịp 0.8s): 25 -> x4 -> -35 -> +55 -> :12 = ?',
     NULL, 'MEDIUM',
     N'10
- Hướng dẫn giải: 25 x 4 = 100; 100 - 35 = 65; 65 + 55 = 120; 120 : 12 = 10.'),

    (@Bank4Id, 'TIMED_SEQUENCE',
     N'Chuỗi nhân chia linh hoạt: 16 -> x5 -> +40 -> :6 -> x7 = ?',
     NULL, 'HARD',
     N'140
- Hướng dẫn giải: 16 x 5 = 80; 80 + 40 = 120; 120 : 6 = 20; 20 x 7 = 140.'),

    (@Bank4Id, 'ESSAY',
     N'Bài toán SASMO: Một đồng hồ quả lắc cứ mỗi giờ lại đổ chuông đúng bằng số giờ đó (1 giờ đổ 1 tiếng, 2 giờ đổ 2 tiếng,..., 12 giờ đổ 12 tiếng). Biết thời gian giữa 2 tiếng chuông liên tiếp là 2 giây. Hỏi lúc 6 giờ cần bao nhiêu giây để nghe trọn vẹn hồi chuông, và trong cả 12 giờ (từ 1h đến 12h) đồng hồ đổ tất cả bao nhiêu tiếng chuông?',
     NULL, 'HARD',
     N'- Tiêu chí chấm & Lời giải chuẩn:
+ Ý 1 (1.0đ): Lúc 6 giờ có 6 tiếng chuông, giữa chúng có 6 - 1 = 5 khoảng cách => thời gian = 5 x 2 = 10 giây.
+ Ý 2 (1.0đ): Tổng số tiếng chuông từ 1h đến 12h = 1 + 2 + ... + 12 = (1 + 12) x 12 : 2 = 78 tiếng chuông.');

    -- -------------------------------------------------------------------------
    -- 9. Cấu hình Lương (TEACHER_SALARY_CONFIG), Điều chỉnh (SALARY_ADJUSTMENT)
    --    & Đồng bộ PAYROLL_SUMMARY theo đúng luật 24h
    -- -------------------------------------------------------------------------
    IF EXISTS (SELECT 1 FROM [dbo].[TEACHER_SALARY_CONFIG] WHERE teacher_id = @TeacherId)
    BEGIN
        UPDATE [dbo].[TEACHER_SALARY_CONFIG]
        SET base_rate_per_session = @BaseRate
        WHERE teacher_id = @TeacherId;
    END
    ELSE
    BEGIN
        INSERT INTO [dbo].[TEACHER_SALARY_CONFIG] (teacher_id, base_rate_per_session)
        VALUES (@TeacherId, @BaseRate);
    END

    -- Chuẩn hóa các bản ghi Bonus/Penalty viết hoa (từ Bogus) về đúng casing của PayrollService.cs ("Bonus" / "Penalty")
    UPDATE [dbo].[SALARY_ADJUSTMENT]
    SET type = CASE
                   WHEN UPPER(LTRIM(RTRIM(type))) = 'BONUS'   THEN N'Bonus'
                   WHEN UPPER(LTRIM(RTRIM(type))) = 'PENALTY' THEN N'Penalty'
                   ELSE type
               END
    WHERE teacher_id = @TeacherId
      AND UPPER(LTRIM(RTRIM(type))) IN ('BONUS', 'PENALTY');

    -- Bổ sung điều chỉnh lương tháng trước và tháng hiện tại cho Giáo viên 1
    DECLARE @PrevMonthDate DATE = DATEADD(MONTH, -1, @Today);
    DECLARE @PrevAdjDate   DATE = DATEFROMPARTS(YEAR(@PrevMonthDate), MONTH(@PrevMonthDate), 20);
    DECLARE @CurrAdjBonus  DATE = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), CASE WHEN DAY(@Today) >= 10 THEN 10 ELSE DAY(@Today) END);
    DECLARE @CurrAdjPen    DATE = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), CASE WHEN DAY(@Today) >= 12 THEN 12 ELSE DAY(@Today) END);

    INSERT INTO [dbo].[SALARY_ADJUSTMENT] (teacher_id, type, amount, reason, applied_date) VALUES
    (@TeacherId, N'Bonus',   300000.00, N'[T1-BS] Thưởng hoàn thành giáo án & nhận xét học viên đầy đủ đúng hạn 24h', @PrevAdjDate),
    (@TeacherId, N'Bonus',   500000.00, N'[T1-BS] Thưởng chất lượng chuyên môn các lớp I-Math, Bar Model & CPA K26',     @CurrAdjBonus),
    (@TeacherId, N'Penalty', 100000.00, N'[T1-BS] Khấu trừ quên chốt điểm danh trong hạn 24h (Buổi cuối lớp K26A)',    @CurrAdjPen);

    -- Tính toán lại & Upsert PAYROLL_SUMMARY cho mọi tháng phát sinh buổi dạy / điều chỉnh của Teacher 1
    -- Điều kiện buổi hợp lệ theo quy chế:
    --   1. Lớp thuộc @TeacherId
    --   2. Buổi học đã bắt đầu (COALESCE(l.start_time, l.lesson_date) <= @NowLocal) và status = 'COMPLETED'
    --   3. Có ít nhất 1 bản ghi điểm danh (giáo viên đã điểm danh trong hạn 24h)
    DECLARE @TargetMonths TABLE (
        [year]  INT NOT NULL,
        [month] INT NOT NULL,
        PRIMARY KEY ([year], [month])
    );

    DELETE FROM [dbo].[PAYROLL_SUMMARY]
    WHERE teacher_id = @TeacherId
      AND (
            [year] > YEAR(@Today)
            OR ([year] = YEAR(@Today) AND [month] > MONTH(@Today))
          )
      AND total_valid_sessions = 0
      AND total_bonus = 0
      AND total_penalty = 0;

    INSERT INTO @TargetMonths ([year], [month])
    SELECT DISTINCT YEAR(l.lesson_date), MONTH(l.lesson_date)
    FROM [dbo].[LESSON] l
    INNER JOIN [dbo].[CLASSES] c ON l.class_id = c.id
    WHERE c.teacher_id = @TeacherId
      AND (
            YEAR(l.lesson_date) < YEAR(@Today)
            OR (YEAR(l.lesson_date) = YEAR(@Today) AND MONTH(l.lesson_date) <= MONTH(@Today))
          )
    UNION
    SELECT DISTINCT YEAR(adj.applied_date), MONTH(adj.applied_date)
    FROM [dbo].[SALARY_ADJUSTMENT] adj
    WHERE adj.teacher_id = @TeacherId
      AND (
            YEAR(adj.applied_date) < YEAR(@Today)
            OR (YEAR(adj.applied_date) = YEAR(@Today) AND MONTH(adj.applied_date) <= MONTH(@Today))
          )
    UNION
    SELECT YEAR(@Today), MONTH(@Today)
    UNION
    SELECT YEAR(@PrevMonthDate), MONTH(@PrevMonthDate);

    DECLARE @RecalculatedPayroll TABLE (
        [year]                  INT NOT NULL,
        [month]                 INT NOT NULL,
        total_valid_sessions    INT NOT NULL,
        base_salary             DECIMAL(18,2) NOT NULL,
        total_penalty           DECIMAL(18,2) NOT NULL,
        total_bonus             DECIMAL(18,2) NOT NULL,
        final_salary            DECIMAL(18,2) NOT NULL,
        PRIMARY KEY ([year], [month])
    );

    ;WITH EligibleByMonth AS (
        SELECT
            YEAR(l.lesson_date) AS [year],
            MONTH(l.lesson_date) AS [month],
            COUNT(*) AS valid_sessions
        FROM [dbo].[LESSON] l
        INNER JOIN [dbo].[CLASSES] c ON l.class_id = c.id
        WHERE c.teacher_id = @TeacherId
          AND l.status = 'COMPLETED'
          AND COALESCE(l.start_time, l.lesson_date) <= @NowLocal
          AND EXISTS (
              SELECT 1
              FROM [dbo].[ATTENDANCE] att
              WHERE att.lesson_id = l.id
          )
        GROUP BY YEAR(l.lesson_date), MONTH(l.lesson_date)
    ),
    AdjustByMonth AS (
        SELECT
            YEAR(adj.applied_date) AS [year],
            MONTH(adj.applied_date) AS [month],
            SUM(CASE WHEN adj.type = N'Penalty' THEN adj.amount ELSE 0 END) AS total_penalty,
            SUM(CASE WHEN adj.type = N'Bonus'   THEN adj.amount ELSE 0 END) AS total_bonus
        FROM [dbo].[SALARY_ADJUSTMENT] adj
        WHERE adj.teacher_id = @TeacherId
        GROUP BY YEAR(adj.applied_date), MONTH(adj.applied_date)
    )
    INSERT INTO @RecalculatedPayroll ([year], [month], total_valid_sessions, base_salary, total_penalty, total_bonus, final_salary)
    SELECT
        tm.[year],
        tm.[month],
        COALESCE(ebm.valid_sessions, 0),
        COALESCE(ebm.valid_sessions, 0) * @BaseRate,
        COALESCE(abm.total_penalty, 0),
        COALESCE(abm.total_bonus, 0),
        COALESCE(ebm.valid_sessions, 0) * @BaseRate + COALESCE(abm.total_bonus, 0) - COALESCE(abm.total_penalty, 0)
    FROM @TargetMonths tm
    LEFT JOIN EligibleByMonth ebm ON ebm.[year] = tm.[year] AND ebm.[month] = tm.[month]
    LEFT JOIN AdjustByMonth abm   ON abm.[year] = tm.[year] AND abm.[month] = tm.[month];

    -- Cập nhật bảng lương đã tồn tại của các tháng có dữ liệu thực
    UPDATE ps
    SET
        ps.total_valid_sessions = rp.total_valid_sessions,
        ps.base_salary          = rp.base_salary,
        ps.total_penalty        = rp.total_penalty,
        ps.total_bonus          = rp.total_bonus,
        ps.final_salary         = rp.final_salary,
        ps.email_status         = CASE
                                      WHEN (ps.total_valid_sessions <> rp.total_valid_sessions
                                            OR ps.base_salary <> rp.base_salary
                                            OR ps.total_penalty <> rp.total_penalty
                                            OR ps.total_bonus <> rp.total_bonus
                                            OR ps.final_salary <> rp.final_salary)
                                           AND ps.email_status IN (N'Sent', N'Failed')
                                      THEN N'NotSent'
                                      ELSE ps.email_status
                                  END,
        ps.email_sent_at        = CASE
                                      WHEN (ps.total_valid_sessions <> rp.total_valid_sessions
                                            OR ps.final_salary <> rp.final_salary)
                                           AND ps.email_status IN (N'Sent', N'Failed')
                                      THEN NULL
                                      ELSE ps.email_sent_at
                                  END,
        ps.email_error_message  = NULL
    FROM [dbo].[PAYROLL_SUMMARY] ps
    INNER JOIN @RecalculatedPayroll rp
        ON ps.teacher_id = @TeacherId
       AND ps.[year] = rp.[year]
       AND ps.[month] = rp.[month];

    -- Chèn thêm các tháng chưa có trong PAYROLL_SUMMARY
    INSERT INTO [dbo].[PAYROLL_SUMMARY]
        (teacher_id, [month], [year], total_valid_sessions, base_salary, total_penalty, total_bonus, final_salary, email_status)
    SELECT
        @TeacherId,
        rp.[month],
        rp.[year],
        rp.total_valid_sessions,
        rp.base_salary,
        rp.total_penalty,
        rp.total_bonus,
        rp.final_salary,
        N'NotSent'
    FROM @RecalculatedPayroll rp
    WHERE NOT EXISTS (
        SELECT 1
        FROM [dbo].[PAYROLL_SUMMARY] ps
        WHERE ps.teacher_id = @TeacherId
          AND ps.[year] = rp.[year]
          AND ps.[month] = rp.[month]
    );

    COMMIT TRANSACTION;

    -- -------------------------------------------------------------------------
    -- 10. Báo cáo kiểm tra kết quả sau khi nạp
    -- -------------------------------------------------------------------------
    PRINT N'=== NẠP DỮ LIỆU BỔ SUNG CHO GIÁO VIÊN #1 THÀNH CÔNG ===';

    SELECT
        c.id,
        c.name AS class_name,
        co.code AS course_code,
        cl.total_sessions AS level_sessions,
        c.status,
        (SELECT COUNT(*) FROM [dbo].[CLASS_STUDENT] cs WHERE cs.class_id = c.id AND cs.status = 'ACTIVE') AS active_students,
        (SELECT COUNT(*) FROM [dbo].[LESSON] l WHERE l.class_id = c.id) AS seeded_lessons,
        (SELECT COUNT(*) FROM [dbo].[LESSON] l WHERE l.class_id = c.id AND l.status = 'COMPLETED'
            AND EXISTS (SELECT 1 FROM [dbo].[ATTENDANCE] a WHERE a.lesson_id = l.id)) AS payroll_eligible_lessons
    FROM [dbo].[CLASSES] c
    INNER JOIN [dbo].[COURSE_LEVEL] cl ON c.course_level_id = cl.id
    INNER JOIN [dbo].[COURSE] co ON cl.course_id = co.id
    WHERE c.teacher_id = @TeacherId
      AND c.name LIKE N'[[]T1-BS]%'
    ORDER BY c.id;

    SELECT
        qb.id,
        co.code AS course_code,
        qb.name AS bank_name,
        (SELECT COUNT(*) FROM [dbo].[QUESTION] q WHERE q.bank_id = qb.id) AS total_questions
    FROM [dbo].[QUESTION_BANK] qb
    INNER JOIN [dbo].[COURSE] co ON qb.course_id = co.id
    WHERE qb.name LIKE N'[[]T1-BS]%'
    ORDER BY qb.id;

    SELECT
        [year],
        [month],
        total_valid_sessions,
        base_salary,
        total_bonus,
        total_penalty,
        final_salary,
        email_status
    FROM [dbo].[PAYROLL_SUMMARY]
    WHERE teacher_id = @TeacherId
    ORDER BY [year] DESC, [month] DESC;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
