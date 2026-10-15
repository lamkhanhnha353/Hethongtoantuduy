using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs.Student;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;

        public StudentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<StudentResponse>> GetByParentAsync(int accountId) =>
            await _context.StudentProfiles
                .AsNoTracking()
                .Where(s => s.ParentAccountId == accountId)
                .OrderBy(s => s.Id)
                .Select(s => Map(s))
                .ToListAsync();

        public async Task<StudentResponse> GetByIdAsync(int accountId, int studentId)
        {
            var student = await FindAsync(accountId, studentId);
            return Map(student);
        }

        public async Task<StudentResponse> CreateAsync(int accountId, CreateStudentRequest request)
        {
            var student = new StudentProfile
            {
                ParentAccountId = accountId,
                FullName = request.FullName,
                Avatar = request.Avatar,
            };

            _context.StudentProfiles.Add(student);
            await _context.SaveChangesAsync();

            return Map(student);
        }

        public async Task<StudentResponse> UpdateAsync(int accountId, int studentId, UpdateStudentRequest request)
        {
            var student = await FindAsync(accountId, studentId);

            student.FullName = request.FullName;
            student.Avatar = request.Avatar;
            await _context.SaveChangesAsync();

            return Map(student);
        }

        public async Task DeleteAsync(int accountId, int studentId)
        {
            var student = await FindAsync(accountId, studentId);

            if (await HasRelatedDataAsync(studentId))
            {
                throw ApiException.Conflict(
                    "Không thể xóa học sinh đang có dữ liệu liên quan (lớp, bài tập, điểm danh, hóa đơn...).");
            }

            _context.StudentProfiles.Remove(student);
            await _context.SaveChangesAsync();
        }

        private async Task<StudentProfile> FindAsync(int accountId, int studentId) =>
            await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.Id == studentId && s.ParentAccountId == accountId)
            ?? throw ApiException.NotFound("Không tìm thấy học sinh.");

        private async Task<bool> HasRelatedDataAsync(int studentId) =>
            await _context.ClassStudents.AnyAsync(x => x.StudentId == studentId)
            || await _context.Invoices.AnyAsync(x => x.StudentId == studentId)
            || await _context.StudentAssignments.AnyAsync(x => x.StudentId == studentId)
            || await _context.Attendances.AnyAsync(x => x.StudentId == studentId)
            || await _context.LeaveRequests.AnyAsync(x => x.StudentId == studentId)
            || await _context.StudentBadges.AnyAsync(x => x.StudentId == studentId);

        private static StudentResponse Map(StudentProfile student) => new()
        {
            Id = student.Id,
            FullName = student.FullName,
            Avatar = student.Avatar,
            TotalPoints = student.TotalPoints,
            Stars = student.Stars,
            StreakDays = student.StreakDays,
        };
    }
}
