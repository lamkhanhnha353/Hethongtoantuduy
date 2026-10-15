using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs.Student;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;
using ToanTuDuy.Services.Storage;

namespace ToanTuDuy.Services
{
    public class StudentService : IStudentService
    {
        private const string AvatarFolder = "avatars/students";

        private readonly ApplicationDbContext _context;
        private readonly IImageStorage _imageStorage;
        private readonly ILogger<StudentService> _logger;

        public StudentService(
            ApplicationDbContext context,
            IImageStorage imageStorage,
            ILogger<StudentService> logger)
        {
            _context = context;
            _imageStorage = imageStorage;
            _logger = logger;
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
            };

            _context.StudentProfiles.Add(student);
            await _context.SaveChangesAsync();

            return Map(student);
        }

        public async Task<StudentResponse> UpdateAsync(int accountId, int studentId, UpdateStudentRequest request)
        {
            var student = await FindAsync(accountId, studentId);

            student.FullName = request.FullName;
            await _context.SaveChangesAsync();

            return Map(student);
        }

        public async Task<StudentResponse> UpdateAvatarAsync(int accountId, int studentId, IFormFile? file, CancellationToken cancellationToken = default)
        {
            ImageUploadRules.EnsureValid(file);

            var student = await FindAsync(accountId, studentId);

            await using var stream = file!.OpenReadStream();
            var url = await _imageStorage.UploadAsync(stream, file.FileName, file.ContentType, AvatarFolder, cancellationToken);

            await SafeDeleteAsync(student.Avatar, cancellationToken);

            student.Avatar = url;
            await _context.SaveChangesAsync(cancellationToken);

            return Map(student);
        }

        public async Task<StudentResponse> RemoveAvatarAsync(int accountId, int studentId, CancellationToken cancellationToken = default)
        {
            var student = await FindAsync(accountId, studentId);

            if (!string.IsNullOrWhiteSpace(student.Avatar))
            {
                await SafeDeleteAsync(student.Avatar, cancellationToken);
                student.Avatar = null;
                await _context.SaveChangesAsync(cancellationToken);
            }

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

            await SafeDeleteAsync(student.Avatar, CancellationToken.None);

            _context.StudentProfiles.Remove(student);
            await _context.SaveChangesAsync();
        }

        private async Task SafeDeleteAsync(string? url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                await _imageStorage.DeleteAsync(url, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Xóa avatar học sinh thất bại: {Url}", url);
            }
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
