using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ToanTuDuy.Data;

namespace ToanTuDuy.Services.Notifications
{
    public interface INotificationAudienceResolver
    {
        Task<HashSet<int>> ResolveAccountIdsAsync(AudienceSpec spec, CancellationToken cancellationToken = default);
    }

    public sealed class NotificationAudienceResolver : INotificationAudienceResolver
    {
        private readonly ApplicationDbContext _context;

        public NotificationAudienceResolver(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HashSet<int>> ResolveAccountIdsAsync(
            AudienceSpec spec,
            CancellationToken cancellationToken = default)
        {
            var accountIds = new HashSet<int>();

            foreach (var target in spec.Targets)
            {
                var resolved = await ResolveTargetAsync(target, cancellationToken);
                accountIds.UnionWith(resolved);
            }

            return accountIds;
        }

        private async Task<List<int>> ResolveTargetAsync(
            AudienceTarget target,
            CancellationToken cancellationToken)
        {
            switch (target.Kind)
            {
                case AudienceKinds.AllAdmins:
                    return await _context.AdminProfiles
                        .AsNoTracking()
                        .Select(admin => admin.AccountId)
                        .ToListAsync(cancellationToken);

                case AudienceKinds.ClassTeacher:
                    if (target.Id is not int classId) return new List<int>();
                    return await _context.Classes
                        .AsNoTracking()
                        .Where(classEntity => classEntity.Id == classId && classEntity.TeacherId.HasValue)
                        .Select(classEntity => classEntity.Teacher!.AccountId)
                        .ToListAsync(cancellationToken);

                case AudienceKinds.ClassParents:
                    if (target.Id is not int classParentsClassId) return new List<int>();
                    return await _context.ClassStudents
                        .AsNoTracking()
                        .Where(classStudent => classStudent.ClassId == classParentsClassId
                            && classStudent.Status == "ACTIVE")
                        .Select(classStudent => classStudent.Student.Parent.AccountId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                case AudienceKinds.StudentParent:
                    if (target.Id is not int studentId) return new List<int>();
                    return await _context.StudentProfiles
                        .AsNoTracking()
                        .Where(student => student.Id == studentId)
                        .Select(student => student.Parent.AccountId)
                        .ToListAsync(cancellationToken);

                case AudienceKinds.TeacherProfile:
                    if (target.Id is not int teacherId) return new List<int>();
                    return await _context.TeacherProfiles
                        .AsNoTracking()
                        .Where(teacher => teacher.Id == teacherId)
                        .Select(teacher => teacher.AccountId)
                        .ToListAsync(cancellationToken);

                case AudienceKinds.AdminProfile:
                    if (target.Id is not int adminId) return new List<int>();
                    return await _context.AdminProfiles
                        .AsNoTracking()
                        .Where(admin => admin.Id == adminId)
                        .Select(admin => admin.AccountId)
                        .ToListAsync(cancellationToken);

                case AudienceKinds.Accounts:
                    return target.AccountIds?.Where(id => id > 0).Distinct().ToList() ?? new List<int>();

                default:
                    return new List<int>();
            }
        }
    }
}
