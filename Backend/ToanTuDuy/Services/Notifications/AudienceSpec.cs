using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ToanTuDuy.Services.Notifications
{
    public static class AudienceKinds
    {
        public const string AllAdmins = "ALL_ADMINS";
        public const string ClassTeacher = "CLASS_TEACHER";
        public const string ClassParents = "CLASS_PARENTS";
        public const string StudentParent = "STUDENT_PARENT";
        public const string TeacherProfile = "TEACHER_PROFILE";
        public const string AdminProfile = "ADMIN_PROFILE";
        public const string Accounts = "ACCOUNTS";
    }

    public sealed class AudienceTarget
    {
        public string Kind { get; set; } = AudienceKinds.Accounts;
        public int? Id { get; set; }
        public List<int>? AccountIds { get; set; }
    }

    public sealed class AudienceSpec
    {
        public List<AudienceTarget> Targets { get; set; } = new();

        public bool IsEmpty => Targets.Count == 0;

        public static AudienceSpec AllAdmins() => new()
        {
            Targets = { new AudienceTarget { Kind = AudienceKinds.AllAdmins } },
        };

        public static AudienceSpec ClassTeacher(int classId) => new()
        {
            Targets = { new AudienceTarget { Kind = AudienceKinds.ClassTeacher, Id = classId } },
        };

        public static AudienceSpec ClassParents(int classId) => new()
        {
            Targets = { new AudienceTarget { Kind = AudienceKinds.ClassParents, Id = classId } },
        };

        public static AudienceSpec StudentParent(int studentId) => new()
        {
            Targets = { new AudienceTarget { Kind = AudienceKinds.StudentParent, Id = studentId } },
        };

        public static AudienceSpec Teacher(int teacherId) => new()
        {
            Targets = { new AudienceTarget { Kind = AudienceKinds.TeacherProfile, Id = teacherId } },
        };

        public static AudienceSpec Admin(int adminId) => new()
        {
            Targets = { new AudienceTarget { Kind = AudienceKinds.AdminProfile, Id = adminId } },
        };

        public static AudienceSpec Account(int accountId) => new()
        {
            Targets = { new AudienceTarget { Kind = AudienceKinds.Accounts, AccountIds = new List<int> { accountId } } },
        };

        public static AudienceSpec Accounts(params int[] accountIds) => new()
        {
            Targets = { new AudienceTarget { Kind = AudienceKinds.Accounts, AccountIds = accountIds.Distinct().ToList() } },
        };

        public static AudienceSpec Union(params AudienceSpec[] specs) => new()
        {
            Targets = specs.SelectMany(spec => spec.Targets).ToList(),
        };
    }

    public sealed record NotificationMessage(
        string Type,
        string Title,
        string? Body = null,
        string? RefId = null,
        int? CreatedByAccountId = null);

    public static class AudienceSpecJson
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    }
}
