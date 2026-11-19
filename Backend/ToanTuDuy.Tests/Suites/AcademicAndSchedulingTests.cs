using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.DTOs.Notification;
using ToanTuDuy.DTOs.Student;
using ToanTuDuy.Models;
using ToanTuDuy.Tests.Infrastructure;

namespace ToanTuDuy.Tests.Suites;

[Collection(BackendApiTestCollection.Name)]
public sealed class AcademicAndSchedulingTests(DatabaseFixture fixture) : IAsyncLifetime
{
    private TestWebApplicationFactory Factory => fixture.Factory;

    public Task InitializeAsync() => fixture.ResetDatabaseAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CoursesAndLevels_EnforceAdminRbac_And_DeleteConstraints()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);

        var courseReq = new CourseRequest
        {
            Code = "SRB-01",
            Name = "Soroban Cơ Bản",
            MinAge = 5,
            MaxAge = 12,
            Status = "ACTIVE"
        };

        Assert.Equal(HttpStatusCode.Forbidden, (await parent.Client.PostAsJsonAsync("/api/courses", courseReq)).StatusCode);

        var createCourse = await admin.Client.PostAsJsonAsync("/api/courses", courseReq);
        Assert.Equal(HttpStatusCode.Created, createCourse.StatusCode);
        var course = (await createCourse.Content.ReadFromJsonAsync<CourseResponse>())!;

        var createLevel = await admin.Client.PostAsJsonAsync("/api/courselevels", new CourseLevelRequest
        {
            CourseId = course.Id,
            Code = "SRB-L1",
            Name = "Cấp độ 1",
            Fee = 1200000m,
            TotalSessions = 4,
            OrderIndex = 1,
            Status = "ACTIVE",
            Syllabus = new[] { new { title = "Làm quen bàn tính" }, new { title = "Cộng trừ cơ bản" } }
        });
        Assert.Equal(HttpStatusCode.Created, createLevel.StatusCode);

        // Cannot delete Course while it still has CourseLevels
        var delCourse = await admin.Client.DeleteAsync($"/api/courses/{course.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, delCourse.StatusCode);
    }

    [Fact]
    public async Task ClassSchedules_EnforceRbacInProduction_DetectConflicts_AndAutoGenerateLessons()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Minh" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        var course = (await (await admin.Client.PostAsJsonAsync("/api/courses", new CourseRequest
        {
            Code = "FGM-01",
            Name = "Finger Math",
            MinAge = 4,
            MaxAge = 8,
            Status = "ACTIVE"
        })).Content.ReadFromJsonAsync<CourseResponse>())!;

        var level = (await (await admin.Client.PostAsJsonAsync("/api/courselevels", new CourseLevelRequest
        {
            CourseId = course.Id,
            Code = "FGM-L1",
            Name = "Finger Math Level 1",
            Fee = 1500000m,
            TotalSessions = 4,
            OrderIndex = 1,
            Status = "ACTIVE",
            Syllabus = new[] { new { title = "Chủ đề 1" }, new { title = "Chủ đề 2" } }
        })).Content.ReadFromJsonAsync<CourseLevelResponse>())!;

        var startDate = DateTime.UtcNow.Date.AddDays(1);
        var class1 = (await (await admin.Client.PostAsJsonAsync("/api/classes", new ClassRequest
        {
            CourseLevelId = level.Id,
            TeacherId = teacher.ProfileId,
            Name = "Lớp FGM-A",
            MaxCapacity = 5,
            StartDate = startDate,
            Status = "OPEN"
        })).Content.ReadFromJsonAsync<ClassResponse>())!;

        var class2 = (await (await admin.Client.PostAsJsonAsync("/api/classes", new ClassRequest
        {
            CourseLevelId = level.Id,
            TeacherId = teacher.ProfileId,
            Name = "Lớp FGM-B",
            MaxCapacity = 5,
            StartDate = startDate,
            Status = "OPEN"
        })).Content.ReadFromJsonAsync<ClassResponse>())!;

        // Non-Admin blocked by AdminOrDevelopmentTesting in Production factory
        var scheduleReq = new ClassScheduleRequest
        {
            ClassId = class1.Id,
            DayOfWeek = (int)startDate.DayOfWeek,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Room = "P101"
        };
        Assert.Equal(HttpStatusCode.Forbidden, (await teacher.Client.PostAsJsonAsync("/api/classschedules", scheduleReq)).StatusCode);

        // Admin creates schedule -> auto generates 4 lessons and updates EndDate
        var createSched = await admin.Client.PostAsJsonAsync("/api/classschedules", scheduleReq);
        Assert.Equal(HttpStatusCode.Created, createSched.StatusCode);

        var lessons = (await admin.Client.GetFromJsonAsync<List<LessonResponse>>($"/api/lessons/class/{class1.Id}"))!;
        Assert.Equal(4, lessons.Count);
        Assert.All(lessons, l => Assert.Equal("SCHEDULED", l.Status));

        var updatedClass1 = (await admin.Client.GetFromJsonAsync<ClassResponse>($"/api/classes/{class1.Id}"))!;
        Assert.Equal(lessons.Max(l => l.LessonDate.Date), updatedClass1.EndDate!.Value.Date);

        // Teacher schedule conflict on Class 2 -> 409 Conflict
        var teacherConflict = await admin.Client.PostAsJsonAsync("/api/classschedules", new ClassScheduleRequest
        {
            ClassId = class2.Id,
            DayOfWeek = (int)startDate.DayOfWeek,
            StartTime = new TimeSpan(9, 30, 0),
            EndTime = new TimeSpan(11, 0, 0),
            Room = "P202"
        });
        Assert.Equal(HttpStatusCode.Conflict, teacherConflict.StatusCode);

        // Enroll student & check roadmap (1st IN_PROGRESS, 2..4 LOCKED)
        var addStudent = await admin.Client.PostAsJsonAsync($"/api/classes/{class1.Id}/students", new AddClassStudentRequest
        {
            StudentId = student.Id
        });
        Assert.Equal(HttpStatusCode.OK, addStudent.StatusCode);

        var roadmaps = (await parent.Client.GetFromJsonAsync<List<StudentRoadmapResponse>>($"/api/lessons/student/{student.Id}/roadmap"))!;
        var roadmap = Assert.Single(roadmaps);
        Assert.Equal(4, roadmap.TotalSessions);
        Assert.Equal("IN_PROGRESS", roadmap.Lessons[0].Status);
        Assert.Equal("Chủ đề 1", roadmap.Lessons[0].Title);
        Assert.Equal("LOCKED", roadmap.Lessons[1].Status);
        Assert.Equal("Chủ đề 2", roadmap.Lessons[1].Title);
        Assert.Equal("LOCKED", roadmap.Lessons[2].Status);
    }

    [Fact]
    public async Task CoursesLevelsAndClasses_ValidateDuplicatesDeleteGuardsCapacityAndPagedFilters()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher1 = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var teacher2 = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var s1 = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "HS 1" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;
        var s2 = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "HS 2" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        // Upload course media image via FakeCourseMediaStorage
        using var mediaForm = new MultipartFormDataContent();
        var imgBytes = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47, 1, 2 });
        imgBytes.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        mediaForm.Add(imgBytes, "File", "cover.png");
        mediaForm.Add(new StringContent("image"), "Type");
        var mediaResp = await admin.Client.PostAsync("/api/courses/media", mediaForm);
        Assert.Equal(HttpStatusCode.OK, mediaResp.StatusCode);

        // TC-ACAD-002: Duplicate Course Code -> 400
        var cReq = new CourseRequest
        {
            Code = "DUP-C",
            Name = "Toán Tư Duy Gốc",
            MinAge = 6,
            MaxAge = 12,
            Status = "ACTIVE"
        };
        var cCreate = await admin.Client.PostAsJsonAsync("/api/courses", cReq);
        Assert.Equal(HttpStatusCode.Created, cCreate.StatusCode);
        var course = (await cCreate.Content.ReadFromJsonAsync<CourseResponse>())!;
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.Client.PostAsJsonAsync("/api/courses", cReq)).StatusCode);

        // Invalid CourseId on CourseLevel -> 400
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.Client.PostAsJsonAsync("/api/courselevels", new CourseLevelRequest
        {
            CourseId = 999999,
            Code = "BAD-L",
            Name = "Invalid",
            Fee = 1000000m,
            TotalSessions = 4,
            OrderIndex = 1,
            Status = "ACTIVE"
        })).StatusCode);

        var level = (await (await admin.Client.PostAsJsonAsync("/api/courselevels", new CourseLevelRequest
        {
            CourseId = course.Id,
            Code = "DUP-L1",
            Name = "Level 1",
            Fee = 1000000m,
            TotalSessions = 4,
            OrderIndex = 1,
            Status = "ACTIVE"
        })).Content.ReadFromJsonAsync<CourseLevelResponse>())!;

        // Create Class with MaxCapacity=1
        var cls = (await (await admin.Client.PostAsJsonAsync("/api/classes", new ClassRequest
        {
            CourseLevelId = level.Id,
            TeacherId = teacher1.ProfileId,
            Name = "Lớp Capacity 1",
            MaxCapacity = 1,
            StartDate = DateTime.UtcNow.Date.AddDays(2),
            Status = "OPEN"
        })).Content.ReadFromJsonAsync<ClassResponse>())!;

        // Cannot delete CourseLevel while it still has Classes -> 400
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.Client.DeleteAsync($"/api/courselevels/{level.Id}")).StatusCode);

        // TC-ACAD-006: Add s1 -> OK; add duplicate s1 -> 400; add s2 exceeding MaxCapacity=1 -> 400
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.PostAsJsonAsync(
            $"/api/classes/{cls.Id}/students", new AddClassStudentRequest { StudentId = s1.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.Client.PostAsJsonAsync(
            $"/api/classes/{cls.Id}/students", new AddClassStudentRequest { StudentId = s1.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.Client.PostAsJsonAsync(
            $"/api/classes/{cls.Id}/students", new AddClassStudentRequest { StudentId = s2.Id })).StatusCode);

        // Capacity filter = full finds this class; cannot delete class with student -> 400
        var fullPaged = (await admin.Client.GetFromJsonAsync<PagedResult<ClassResponse>>(
            "/api/classes/paged?capacity=full"))!;
        Assert.Contains(fullPaged.Items, x => x.Id == cls.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.Client.DeleteAsync($"/api/classes/{cls.Id}")).StatusCode);

        // Reassign TeacherId triggers notification + email to new teacher
        var updateCls = await admin.Client.PutAsJsonAsync($"/api/classes/{cls.Id}", new ClassRequest
        {
            CourseLevelId = level.Id,
            TeacherId = teacher2.ProfileId,
            Name = "Lớp Đã Đổi GV",
            MaxCapacity = 1,
            StartDate = cls.StartDate,
            Status = "OPEN"
        });
        Assert.Equal(HttpStatusCode.OK, updateCls.StatusCode);

        await ApiAssertHelper.EventuallyAsync(() =>
            Task.FromResult(Factory.FakeEmail.SentMessages.Any(m => m.ToEmail == teacher2.Email)));

        // Remove student -> now class can be deleted -> level -> course
        Assert.Equal(HttpStatusCode.OK,
            (await admin.Client.DeleteAsync($"/api/classes/{cls.Id}/students/{s1.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.DeleteAsync($"/api/classes/{cls.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.DeleteAsync($"/api/courselevels/{level.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.DeleteAsync($"/api/courses/{course.Id}")).StatusCode);
    }

    [Fact]
    public async Task ClassSchedules_SameClassAndRoomConflicts_And_LeaveRequestUpdateGuard_And_SpecialLessons_Work()
    {
        var admin = await AuthTestHelper.CreateAdminAsync(Factory);
        var teacher1 = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var teacher2 = await AuthTestHelper.RegisterTeacherAsync(Factory, markEmailConfirmed: true);
        var parent = await AuthTestHelper.RegisterParentAsync(Factory);
        var student = (await (await parent.Client.PostAsJsonAsync("/api/students", new CreateStudentRequest { FullName = "Bé Lịch" }))
            .Content.ReadFromJsonAsync<StudentResponse>())!;

        var course = (await (await admin.Client.PostAsJsonAsync("/api/courses", new CourseRequest
        {
            Code = "SCH-C",
            Name = "Toán Lịch",
            MinAge = 5,
            MaxAge = 12,
            Status = "ACTIVE"
        })).Content.ReadFromJsonAsync<CourseResponse>())!;

        var level = (await (await admin.Client.PostAsJsonAsync("/api/courselevels", new CourseLevelRequest
        {
            CourseId = course.Id,
            Code = "SCH-L",
            Name = "L1",
            Fee = 1000000m,
            TotalSessions = 2,
            OrderIndex = 1,
            Status = "ACTIVE"
        })).Content.ReadFromJsonAsync<CourseLevelResponse>())!;

        var futureDate = DateTime.UtcNow.AddDays(5).Date;
        var c1 = (await (await admin.Client.PostAsJsonAsync("/api/classes", new ClassRequest
        {
            CourseLevelId = level.Id,
            TeacherId = teacher1.ProfileId,
            Name = "Lớp Lịch 1",
            MaxCapacity = 5,
            StartDate = futureDate,
            Status = "OPEN"
        })).Content.ReadFromJsonAsync<ClassResponse>())!;

        var c2 = (await (await admin.Client.PostAsJsonAsync("/api/classes", new ClassRequest
        {
            CourseLevelId = level.Id,
            TeacherId = teacher2.ProfileId,
            Name = "Lớp Lịch 2",
            MaxCapacity = 5,
            StartDate = futureDate,
            Status = "OPEN"
        })).Content.ReadFromJsonAsync<ClassResponse>())!;

        var s1Resp = await admin.Client.PostAsJsonAsync("/api/classschedules", new ClassScheduleRequest
        {
            ClassId = c1.Id,
            DayOfWeek = (int)futureDate.DayOfWeek,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 30, 0),
            Room = "P101"
        });
        Assert.Equal(HttpStatusCode.Created, s1Resp.StatusCode);
        var sched1 = (await s1Resp.Content.ReadFromJsonAsync<ClassScheduleResponse>())!;

        // Same class overlapping time -> 409
        Assert.Equal(HttpStatusCode.Conflict, (await admin.Client.PostAsJsonAsync("/api/classschedules", new ClassScheduleRequest
        {
            ClassId = c1.Id,
            DayOfWeek = (int)futureDate.DayOfWeek,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 30, 0),
            Room = "P102"
        })).StatusCode);

        // Room conflict on Class 2 (different teacher, same room P101 & overlapping time) -> 409
        Assert.Equal(HttpStatusCode.Conflict, (await admin.Client.PostAsJsonAsync("/api/classschedules", new ClassScheduleRequest
        {
            ClassId = c2.Id,
            DayOfWeek = (int)futureDate.DayOfWeek,
            StartTime = new TimeSpan(9, 30, 0),
            EndTime = new TimeSpan(11, 0, 0),
            Room = "P101"
        })).StatusCode);

        // Enroll student & create LeaveRequest on 1st generated future lesson -> updating schedule returns 409 (TC-ACAD-010)
        Assert.Equal(HttpStatusCode.OK, (await admin.Client.PostAsJsonAsync(
            $"/api/classes/{c1.Id}/students", new AddClassStudentRequest { StudentId = student.Id })).StatusCode);

        var c1Lessons = (await admin.Client.GetFromJsonAsync<List<LessonResponse>>($"/api/lessons/class/{c1.Id}"))!;
        var leaveResp = await parent.Client.PostAsJsonAsync("/api/leaverequests", new LeaveRequestCreateRequest
        {
            StudentId = student.Id,
            LessonId = c1Lessons[0].Id,
            Reason = "Nghỉ phép thử chặn đổi lịch"
        });
        Assert.Equal(HttpStatusCode.OK, leaveResp.StatusCode);

        var blockedUpdate = await admin.Client.PutAsJsonAsync($"/api/classschedules/{sched1.Id}", new ClassScheduleRequest
        {
            ClassId = c1.Id,
            DayOfWeek = (int)futureDate.DayOfWeek,
            StartTime = new TimeSpan(14, 0, 0),
            EndTime = new TimeSpan(15, 30, 0),
            Room = "P101"
        });
        Assert.Equal(HttpStatusCode.Conflict, blockedUpdate.StatusCode);

        // TC-ACAD-011: Lessons CRUD & Special lesson notification
        var badLessonTime = await admin.Client.PostAsJsonAsync("/api/lessons", new LessonRequest
        {
            ClassId = c1.Id,
            LessonDate = futureDate.AddDays(14),
            StartTime = futureDate.AddDays(14).AddHours(10),
            EndTime = futureDate.AddDays(14).AddHours(9),
            Status = "SCHEDULED"
        });
        Assert.Equal(HttpStatusCode.BadRequest, badLessonTime.StatusCode);

        Factory.FakeEmail.Reset();
        var specialLessonResp = await admin.Client.PostAsJsonAsync("/api/lessons", new LessonRequest
        {
            ClassId = c1.Id,
            LessonDate = futureDate.AddDays(14),
            StartTime = futureDate.AddDays(14).AddHours(14),
            EndTime = futureDate.AddDays(14).AddHours(15.5),
            Status = "SCHEDULED",
            IsImportan = true
        });
        Assert.Equal(HttpStatusCode.Created, specialLessonResp.StatusCode);
        var specialLesson = (await specialLessonResp.Content.ReadFromJsonAsync<LessonResponse>())!;

        await ApiAssertHelper.EventuallyAsync(async () =>
        {
            var tInbox = await teacher1.Client.GetFromJsonAsync<NotificationListResponse>("/api/notifications");
            return tInbox is not null && tInbox.Items.Any(n => n.Type == "SPECIAL");
        });

        // Lesson with Attendance cannot be deleted -> 400
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Attendances.Add(new Attendance
            {
                LessonId = specialLesson.Id,
                StudentId = student.Id,
                Status = "PRESENT"
            });
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.Client.DeleteAsync($"/api/lessons/{specialLesson.Id}")).StatusCode);

        // Parent timeline endpoint & isolation
        var timeline = (await parent.Client.GetFromJsonAsync<List<StudentLessonResponse>>($"/api/lessons/student/{student.Id}"))!;
        Assert.True(timeline.Count >= 3);
    }
}
