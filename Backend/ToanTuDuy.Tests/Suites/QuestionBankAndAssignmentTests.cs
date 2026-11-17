using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
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
}
