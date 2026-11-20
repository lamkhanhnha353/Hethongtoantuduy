using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.DTOs.Notification;
using ToanTuDuy.DTOs.Student;
using ToanTuDuy.Models;
using ToanTuDuy.Tests.Infrastructure;

namespace ToanTuDuy.Tests.Suites;

[Collection(BackendApiTestCollection.Name)]
public sealed class QuestionBankAndAssignmentTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private TestWebApplicationFactory Factory => fixture.Factory;

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Assignment_ValidatesQuestionTypes_AutoGradesFourTypes_AndPreventsConcurrentDoubleSubmit()
    {
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Quiz" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        int courseId, lessonId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "ASGN-C", Name = "Toán Quiz", MinAge = 6, MaxAge = 12, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            courseId = course.Id;

            var level = new CourseLevel { CourseId = course.Id, Code = "ASGN-L", Name = "L1", Fee = 1000000m, TotalSessions = 2, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacher.ProfileId, Name = "Lớp Quiz", MaxCapacity = 10, StartDate = DateTime.UtcNow.Date, Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();

            db.ClassStudents.Add(new ClassStudent { ClassId = cls.Id, StudentId = student.Id, Status = "ACTIVE" });

            var lesson = new Lesson { ClassId = cls.Id, Title = "Buổi Quiz", LessonDate = DateTime.UtcNow.Date, Status = "SCHEDULED" };
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
            lessonId = lesson.Id;
        }

        var bank = (await (await teacher.Client.PostAsJsonAsync("/api/question-banks", new QuestionBankRequest
        {
            CourseId = courseId,
            Name = "Bank Tổng Hợp"
        })).Content.ReadFromJsonAsync<QuestionBankResponse>())!;

        var bulkResp = await teacher.Client.PostAsJsonAsync("/api/question-banks/questions/bulk", new BulkQuestionCreateRequest
        {
            BankId = bank.Id,
            Questions =
            [
                new QuestionItemDto { Type = "MULTIPLE_CHOICE", Content = "2 + 3 = ? A. 4 B. 5 C. 6 D. 7", Difficulty = "EASY", Answer = "B. 5" },
                new QuestionItemDto { Type = "TRUE_FALSE", Content = "5 > 3 là đúng hay sai?", Difficulty = "EASY", Answer = "Đúng vì 5 lớn hơn 3" },
                new QuestionItemDto { Type = "FILL_IN", Content = "10 - 4 = ?", Difficulty = "MEDIUM", Answer = "6\nLời giải: 10 trừ 4 bằng 6" },
                new QuestionItemDto { Type = "TIMED_SEQUENCE", Content = "Tính nhẩm theo nhịp: 5, +3, -2", Difficulty = "MEDIUM", Answer = "6" },
                new QuestionItemDto { Type = "ESSAY", Content = "Chứng minh...", Difficulty = "HARD", Answer = "Bài giải" }
            ]
        });
        Assert.Equal(HttpStatusCode.OK, bulkResp.StatusCode);
        var questions = (await bulkResp.Content.ReadFromJsonAsync<List<QuestionResponse>>())!;

        var start = DateTime.UtcNow.AddMinutes(-10);
        var deadline = DateTime.UtcNow.AddHours(2);

        // ESSAY question rejected -> 400
        var essayResp = await teacher.Client.PostAsJsonAsync("/api/assignments", new CreateAssignmentRequest
        {
            LessonId = lessonId,
            Title = "Essay Invalid",
            StartTime = start,
            Deadline = deadline,
            TimeLimitMinutes = 15,
            MaxAttempt = 1,
            Questions = [new QuestionAssignmentItemDto { QuestionId = questions[4].Id, OrderIndex = 1, PointValue = 2.5f }]
        });
        Assert.Equal(HttpStatusCode.BadRequest, essayResp.StatusCode);

        // Valid assignment with 4 supported types
        var createAssignment = await teacher.Client.PostAsJsonAsync("/api/assignments", new CreateAssignmentRequest
        {
            LessonId = lessonId,
            Title = "Bài luyện tập 4 dạng",
            StartTime = start,
            Deadline = deadline,
            TimeLimitMinutes = 20,
            MaxAttempt = 1,
            AssignAllStudentsInClass = true,
            Questions = questions.Take(4).Select((q, idx) => new QuestionAssignmentItemDto
            {
                QuestionId = q.Id,
                OrderIndex = idx + 1,
                PointValue = 2.5f
            }).ToList()
        });
        Assert.Equal(HttpStatusCode.Created, createAssignment.StatusCode);
        var assignment = (await createAssignment.Content.ReadFromJsonAsync<AssignmentDetailResponse>())!;

        // Parent fetches practice detail (answers hidden, MC options & sequenceTerms parsed)
        var practice = (await parent.Client.GetFromJsonAsync<StudentPracticeAssignmentDetailDto>(
            $"/api/assignments/{assignment.Id}/student/{student.Id}/practice"))!;
        Assert.Equal(4, practice.Questions.Count);
        Assert.Equal(4, practice.Questions[0].Options.Count);
        Assert.Equal(3, practice.Questions[3].SequenceTerms.Count);

        // Submit 3 correct + 1 wrong
        var submitPayload = new SubmitStudentPracticeRequest
        {
            Answers =
            [
                new SubmitStudentPracticeAnswerDto { QuestionAssignmentId = practice.Questions[0].QuestionAssignmentId, Answer = "b" },
                new SubmitStudentPracticeAnswerDto { QuestionAssignmentId = practice.Questions[1].QuestionAssignmentId, Answer = "đúng" },
                new SubmitStudentPracticeAnswerDto { QuestionAssignmentId = practice.Questions[2].QuestionAssignmentId, Answer = " 6 " },
                new SubmitStudentPracticeAnswerDto { QuestionAssignmentId = practice.Questions[3].QuestionAssignmentId, Answer = "99" }
            ]
        };

        var submitResp = await parent.Client.PostAsJsonAsync(
            $"/api/assignments/{assignment.Id}/student/{student.Id}/practice/submit",
            submitPayload);
        Assert.Equal(HttpStatusCode.OK, submitResp.StatusCode);
        var graded = (await submitResp.Content.ReadFromJsonAsync<StudentPracticeSubmissionResponseDto>())!;
        Assert.Equal(7.5f, graded.Score);
        Assert.Equal(10f, graded.TotalPoints);
        Assert.Equal(3, graded.CorrectCount);
        Assert.Equal("SUBMITTED", graded.Status);

        // Clone assignment for concurrency test
        var cloned = (await (await teacher.Client.PostAsJsonAsync("/api/assignments/clone", new CloneAssignmentRequest
        {
            SourceAssignmentId = assignment.Id,
            TargetLessonId = lessonId,
            NewTitle = "Bài Concurrency",
            StartTime = start,
            Deadline = deadline,
            AssignAllStudentsInClass = true
        })).Content.ReadFromJsonAsync<AssignmentDetailResponse>())!;

        var practice2 = (await parent.Client.GetFromJsonAsync<StudentPracticeAssignmentDetailDto>(
            $"/api/assignments/{cloned.Id}/student/{student.Id}/practice"))!;
        var concurrentPayload = new SubmitStudentPracticeRequest
        {
            Answers = practice2.Questions.Select(q => new SubmitStudentPracticeAnswerDto
            {
                QuestionAssignmentId = q.QuestionAssignmentId,
                Answer = "B"
            }).ToList()
        };

        var tasks = Enumerable.Range(0, 3)
            .Select(_ => parent.Client.PostAsJsonAsync(
                $"/api/assignments/{cloned.Id}/student/{student.Id}/practice/submit",
                concurrentPayload))
            .ToArray();
        var responses = await Task.WhenAll(tasks);

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(2, responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest));
    }

    [Fact]
    public async Task QuestionBanks_CrudPagedFiltering_And_BulkDeleteDetachesFromAssignments()
    {
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);

        int courseId, lessonId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "QB-C", Name = "Toán QB", MinAge = 6, MaxAge = 12, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            courseId = course.Id;

            var level = new CourseLevel { CourseId = course.Id, Code = "QB-L", Name = "L1", Fee = 1000000m, TotalSessions = 1, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacher.ProfileId, Name = "Lớp QB", MaxCapacity = 10, StartDate = DateTime.UtcNow.Date, Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();

            var lesson = new Lesson { ClassId = cls.Id, Title = "Buổi QB", LessonDate = DateTime.UtcNow.Date, Status = "SCHEDULED" };
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
            lessonId = lesson.Id;
        }

        // Create & Update QuestionBank
        var bankResp = await teacher.Client.PostAsJsonAsync("/api/question-banks", new QuestionBankRequest
        {
            CourseId = courseId,
            Name = "Bank Gốc",
            Description = "Mô tả"
        });
        Assert.Equal(HttpStatusCode.Created, bankResp.StatusCode);
        var bank = (await bankResp.Content.ReadFromJsonAsync<QuestionBankResponse>())!;

        Assert.Equal(HttpStatusCode.OK, (await teacher.Client.PutAsJsonAsync($"/api/question-banks/{bank.Id}", new QuestionBankRequest
        {
            CourseId = courseId,
            Name = "Bank Đã Sửa",
            Description = "Mô tả mới"
        })).StatusCode);

        var pagedBanks = (await teacher.Client.GetFromJsonAsync<PagedResult<QuestionBankResponse>>(
            $"/api/question-banks/paged?courseId={courseId}&keyword=Đã Sửa"))!;
        Assert.Equal(bank.Id, Assert.Single(pagedBanks.Items).Id);

        // Single create + bulk create + paged filter
        var singleQ = (await (await teacher.Client.PostAsJsonAsync("/api/question-banks/questions", new QuestionRequest
        {
            BankId = bank.Id,
            Type = "FILL_IN",
            Content = "1 + 1 = ?",
            Difficulty = "EASY",
            Answer = "2"
        })).Content.ReadFromJsonAsync<QuestionResponse>())!;

        var bulkQs = (await (await teacher.Client.PostAsJsonAsync("/api/question-banks/questions/bulk", new BulkQuestionCreateRequest
        {
            BankId = bank.Id,
            Questions =
            [
                new QuestionItemDto { Type = "FILL_IN", Content = "Từ khóa đặc biệt 1", Difficulty = "HARD", Answer = "10" },
                new QuestionItemDto { Type = "TRUE_FALSE", Content = "2 > 1?", Difficulty = "MEDIUM", Answer = "Đúng" }
            ]
        })).Content.ReadFromJsonAsync<List<QuestionResponse>>())!;

        var filteredQs = (await teacher.Client.GetFromJsonAsync<PagedResult<QuestionResponse>>(
            $"/api/question-banks/{bank.Id}/questions/paged?difficulty=HARD&type=FILL_IN&keyword=đặc biệt"))!;
        Assert.Equal(bulkQs[0].Id, Assert.Single(filteredQs.Items).Id);

        // Attach singleQ to an assignment, then bulk-delete singleQ + bulkQs[0] -> detaches QuestionAssignment cleanly
        var asgn = (await (await teacher.Client.PostAsJsonAsync("/api/assignments", new CreateAssignmentRequest
        {
            LessonId = lessonId,
            Title = "Bài kiểm tra detach",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            Deadline = DateTime.UtcNow.AddHours(2),
            MaxAttempt = 1,
            AssignAllStudentsInClass = false,
            Questions =
            [
                new QuestionAssignmentItemDto { QuestionId = singleQ.Id, OrderIndex = 1, PointValue = 5f },
                new QuestionAssignmentItemDto { QuestionId = bulkQs[1].Id, OrderIndex = 2, PointValue = 5f }
            ]
        })).Content.ReadFromJsonAsync<AssignmentDetailResponse>())!;

        var bulkDelResp = await teacher.Client.PostAsJsonAsync("/api/question-banks/questions/bulk-delete", new BulkQuestionDeleteRequest
        {
            Ids = [singleQ.Id, bulkQs[0].Id]
        });
        Assert.Equal(HttpStatusCode.OK, bulkDelResp.StatusCode);
        var bulkDel = (await bulkDelResp.Content.ReadFromJsonAsync<BulkQuestionDeleteResponse>())!;
        Assert.Equal(2, bulkDel.DeletedCount);
        Assert.Equal(1, bulkDel.RemovedAssignmentCount);

        var updatedAsgn = (await teacher.Client.GetFromJsonAsync<AssignmentDetailResponse>($"/api/assignments/{asgn.Id}"))!;
        Assert.Single(updatedAsgn.Questions);

        // Delete remaining question & bank
        Assert.Equal(HttpStatusCode.OK, (await teacher.Client.DeleteAsync($"/api/question-banks/questions/{bulkQs[1].Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await teacher.Client.DeleteAsync($"/api/question-banks/{bank.Id}")).StatusCode);
    }

    [Fact]
    public async Task Assignments_ValidationCrudAssignUnassign_PracticeGuards_And_TeacherGrading_Work()
    {
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent1 = await AuthTestHelper.RegisterParentAsync(Factory);
        var parent2 = await AuthTestHelper.RegisterParentAsync(Factory);

        var s1 = (await (await parent1.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé A" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;
        var s2 = (await (await parent2.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé B" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;
        var sOut = (await (await parent1.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Ngoài Lớp" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        int courseId, lessonId;
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var course = new Course { Code = "ASG2-C", Name = "Toán ASG2", MinAge = 6, MaxAge = 12, Status = "ACTIVE", CreatedAt = DateTime.UtcNow };
            db.Courses.Add(course);
            await db.SaveChangesAsync();
            courseId = course.Id;

            var level = new CourseLevel { CourseId = course.Id, Code = "ASG2-L", Name = "L1", Fee = 1000000m, TotalSessions = 1, OrderIndex = 1, Status = "ACTIVE" };
            db.CourseLevels.Add(level);
            await db.SaveChangesAsync();

            var cls = new Classes { CourseLevelId = level.Id, TeacherId = teacher.ProfileId, Name = "Lớp ASG2", MaxCapacity = 10, StartDate = DateTime.UtcNow.Date, Status = "OPEN" };
            db.Classes.Add(cls);
            await db.SaveChangesAsync();

            db.ClassStudents.AddRange(
                new ClassStudent { ClassId = cls.Id, StudentId = s1.Id, Status = "ACTIVE" },
                new ClassStudent { ClassId = cls.Id, StudentId = s2.Id, Status = "ACTIVE" });

            var lesson = new Lesson { ClassId = cls.Id, Title = "Buổi ASG2", LessonDate = DateTime.UtcNow.Date, Status = "SCHEDULED" };
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync();
            lessonId = lesson.Id;
        }

        var bank = (await (await teacher.Client.PostAsJsonAsync("/api/question-banks", new QuestionBankRequest
        {
            CourseId = courseId,
            Name = "Bank ASG2"
        })).Content.ReadFromJsonAsync<QuestionBankResponse>())!;

        var q1 = (await (await teacher.Client.PostAsJsonAsync("/api/question-banks/questions", new QuestionRequest
        {
            BankId = bank.Id,
            Type = "FILL_IN",
            Content = "3 + 4 = ?",
            Difficulty = "EASY",
            Answer = "7"
        })).Content.ReadFromJsonAsync<QuestionResponse>())!;

        var now = DateTime.UtcNow;

        // TC-ASGN-003: MaxAttempt != 1 and Deadline <= StartTime -> 400
        Assert.Equal(HttpStatusCode.BadRequest, (await teacher.Client.PostAsJsonAsync("/api/assignments", new CreateAssignmentRequest
        {
            LessonId = lessonId,
            Title = "MaxAttempt=2",
            StartTime = now.AddMinutes(-5),
            Deadline = now.AddHours(2),
            MaxAttempt = 2,
            Questions = [new QuestionAssignmentItemDto { QuestionId = q1.Id, OrderIndex = 1, PointValue = 10f }]
        })).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await teacher.Client.PostAsJsonAsync("/api/assignments", new CreateAssignmentRequest
        {
            LessonId = lessonId,
            Title = "Bad Deadline",
            StartTime = now.AddHours(2),
            Deadline = now.AddHours(1),
            MaxAttempt = 1,
            Questions = [new QuestionAssignmentItemDto { QuestionId = q1.Id, OrderIndex = 1, PointValue = 10f }]
        })).StatusCode);

        // Future assignment (not opened yet) -> practice detail returns 400
        var futureAsgn = (await (await teacher.Client.PostAsJsonAsync("/api/assignments", new CreateAssignmentRequest
        {
            LessonId = lessonId,
            Title = "Chưa mở",
            StartTime = now.AddHours(2),
            Deadline = now.AddHours(4),
            MaxAttempt = 1,
            AssignAllStudentsInClass = true,
            Questions = [new QuestionAssignmentItemDto { QuestionId = q1.Id, OrderIndex = 1, PointValue = 10f }]
        })).Content.ReadFromJsonAsync<AssignmentDetailResponse>())!;

        Assert.Equal(HttpStatusCode.BadRequest,
            (await parent1.Client.GetAsync($"/api/assignments/{futureAsgn.Id}/student/{s1.Id}/practice")).StatusCode);

        // Update future assignment to open, then delete it cleanly
        Assert.Equal(HttpStatusCode.OK, (await teacher.Client.PutAsJsonAsync($"/api/assignments/{futureAsgn.Id}", new UpdateAssignmentRequest
        {
            Title = "Đã cập nhật",
            StartTime = now.AddMinutes(-5),
            Deadline = now.AddHours(4),
            TimeLimitMinutes = 25,
            MaxAttempt = 1,
            Status = "OPEN"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await teacher.Client.DeleteAsync($"/api/assignments/{futureAsgn.Id}")).StatusCode);

        // Create assignment specifically for s1 only
        var asgn = (await (await teacher.Client.PostAsJsonAsync("/api/assignments", new CreateAssignmentRequest
        {
            LessonId = lessonId,
            Title = "Giao riêng và chấm điểm",
            StartTime = now.AddMinutes(-5),
            Deadline = now.AddHours(3),
            MaxAttempt = 1,
            AssignAllStudentsInClass = false,
            SpecificStudentIds = [s1.Id],
            Questions = [new QuestionAssignmentItemDto { QuestionId = q1.Id, OrderIndex = 1, PointValue = 10f }]
        })).Content.ReadFromJsonAsync<AssignmentDetailResponse>())!;

        // SC-ASGN-03: Assign student outside class -> 400; assign s2 -> OK
        Assert.Equal(HttpStatusCode.BadRequest, (await teacher.Client.PostAsJsonAsync(
            $"/api/assignments/{asgn.Id}/students",
            new AssignStudentsRequest { StudentIds = [sOut.Id] })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await teacher.Client.PostAsJsonAsync(
            $"/api/assignments/{asgn.Id}/students",
            new AssignStudentsRequest { StudentIds = [s2.Id] })).StatusCode);

        // Unassign s2 while ASSIGNED -> 200 OK
        Assert.Equal(HttpStatusCode.OK,
            (await teacher.Client.DeleteAsync($"/api/assignments/{asgn.Id}/students/{s2.Id}")).StatusCode);

        // Practice list shows assignment available for s1
        var practiceList = (await parent1.Client.GetFromJsonAsync<List<StudentPracticeAssignmentDto>>(
            $"/api/assignments/student/{s1.Id}/practice"))!;
        Assert.Contains(practiceList, p => p.AssignmentId == asgn.Id && p.IsAvailable);

        // Submit with wrong QuestionAssignmentId / missing answer -> 400
        var practiceDetail = (await parent1.Client.GetFromJsonAsync<StudentPracticeAssignmentDetailDto>(
            $"/api/assignments/{asgn.Id}/student/{s1.Id}/practice"))!;
        Assert.Equal(HttpStatusCode.BadRequest, (await parent1.Client.PostAsJsonAsync(
            $"/api/assignments/{asgn.Id}/student/{s1.Id}/practice/submit",
            new SubmitStudentPracticeRequest
            {
                Answers = [new SubmitStudentPracticeAnswerDto { QuestionAssignmentId = 999999, Answer = "7" }]
            })).StatusCode);

        // Valid submit s1 -> SUBMITTED; unassigning SUBMITTED s1 -> 400; fetching practice again -> 400
        Assert.Equal(HttpStatusCode.OK, (await parent1.Client.PostAsJsonAsync(
            $"/api/assignments/{asgn.Id}/student/{s1.Id}/practice/submit",
            new SubmitStudentPracticeRequest
            {
                Answers = [new SubmitStudentPracticeAnswerDto { QuestionAssignmentId = practiceDetail.Questions[0].QuestionAssignmentId, Answer = "7" }]
            })).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await teacher.Client.DeleteAsync($"/api/assignments/{asgn.Id}/students/{s1.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await parent1.Client.GetAsync($"/api/assignments/{asgn.Id}/student/{s1.Id}/practice")).StatusCode);

        // TC-ASGN-007: Teacher single grade & batch grade -> GRADED + ACADEMIC notification to parent1
        Assert.Equal(HttpStatusCode.OK, (await teacher.Client.PutAsJsonAsync(
            $"/api/assignments/{asgn.Id}/grade/{s1.Id}",
            new GradeStudentRequest { Score = 9.5f, TeacherFeedback = "Rất tốt", Status = "GRADED" })).StatusCode);

        await ApiAssertHelper.EventuallyAsync(async () =>
        {
            var p1Inbox = await parent1.Client.GetFromJsonAsync<NotificationListResponse>("/api/notifications");
            return p1Inbox is not null && p1Inbox.Items.Any(n => n.Type == "ACADEMIC");
        });

        Assert.Equal(HttpStatusCode.OK, (await teacher.Client.PutAsJsonAsync(
            $"/api/assignments/{asgn.Id}/batch-grade",
            new BatchGradeRequest
            {
                Grades = [new BatchGradeItemRequest { StudentId = s1.Id, Score = 10f, TeacherFeedback = "Hoàn hảo", Status = "GRADED" }]
            })).StatusCode);
    }
}
