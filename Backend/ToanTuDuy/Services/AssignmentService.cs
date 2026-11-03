using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class AssignmentService : IAssignmentService
    {
        private readonly ApplicationDbContext _context;

        public AssignmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AssignmentDetailResponse> CreateAssignmentAsync(CreateAssignmentRequest request)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Class)
                .FirstOrDefaultAsync(l => l.Id == request.LessonId);

            if (lesson == null)
            {
                throw ApiException.NotFound($"Không tìm thấy buổi học với ID {request.LessonId}.");
            }

            if (request.Deadline <= request.StartTime)
            {
                throw ApiException.BadRequest("Hạn nộp bài (Deadline) phải lớn hơn thời gian bắt đầu (StartTime).");
            }

            // Tạo Assignment
            var assignment = new Assignment
            {
                LessonId = request.LessonId,
                StageId = request.StageId,
                Title = request.Title.Trim(),
                StartTime = request.StartTime,
                Deadline = request.Deadline,
                TimeLimitMinutes = request.TimeLimitMinutes,
                MaxAttempt = request.MaxAttempt,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "OPEN" : request.Status.Trim().ToUpper()
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Đính kèm các câu hỏi từ ngân hàng câu hỏi
            if (request.Questions != null && request.Questions.Any())
            {
                var inputQuestionIds = request.Questions.Select(q => q.QuestionId).Distinct().ToList();
                var validQuestions = await _context.Questions
                    .Where(q => inputQuestionIds.Contains(q.Id))
                    .Select(q => q.Id)
                    .ToListAsync();

                var invalidQuestionIds = inputQuestionIds.Except(validQuestions).ToList();
                if (invalidQuestionIds.Any())
                {
                    throw ApiException.BadRequest($"Các câu hỏi có ID [{string.Join(", ", invalidQuestionIds)}] không tồn tại.");
                }

                int order = 1;
                foreach (var q in request.Questions)
                {
                    var questionAssignment = new QuestionAssignment
                    {
                        AssignmentId = assignment.Id,
                        QuestionId = q.QuestionId,
                        OrderIndex = q.OrderIndex > 0 ? q.OrderIndex : order++,
                        PointValue = q.PointValue > 0 ? q.PointValue : 1.0f
                    };
                    _context.QuestionAssignments.Add(questionAssignment);
                }
            }

            // Phân phối học sinh (Giao cả lớp hoặc Giao riêng lẻ)
            var activeClassStudents = await _context.ClassStudents
                .Where(cs => cs.ClassId == lesson.ClassId && cs.Status == "ACTIVE")
                .Select(cs => cs.StudentId)
                .ToListAsync();

            var studentsToAssign = new List<int>();

            if (request.AssignAllStudentsInClass)
            {
                // Giao cho toàn bộ học sinh đang học trong lớp
                studentsToAssign = activeClassStudents;
            }
            else if (request.SpecificStudentIds != null && request.SpecificStudentIds.Any())
            {
                // Giao riêng lẻ cho danh sách học sinh được chỉ định
                var distinctSpecificIds = request.SpecificStudentIds.Distinct().ToList();
                var invalidStudentIds = distinctSpecificIds.Except(activeClassStudents).ToList();
                if (invalidStudentIds.Any())
                {
                    throw ApiException.BadRequest($"Học sinh ID [{string.Join(", ", invalidStudentIds)}] không thuộc lớp học này.");
                }

                studentsToAssign = distinctSpecificIds;
            }

            foreach (var studentId in studentsToAssign)
            {
                var studentAssignment = new StudentAssignment
                {
                    AssignmentId = assignment.Id,
                    StudentId = studentId,
                    Status = "ASSIGNED"
                };
                _context.StudentAssignments.Add(studentAssignment);
            }

            await _context.SaveChangesAsync();

            var result = await GetAssignmentByIdAsync(assignment.Id);
            return result!;
        }

        public async Task<AssignmentDetailResponse> CloneAssignmentToLessonAsync(CloneAssignmentRequest request)
        {
            var source = await _context.Assignments
                .Include(a => a.QuestionAssignments)
                .FirstOrDefaultAsync(a => a.Id == request.SourceAssignmentId);

            if (source == null)
            {
                throw ApiException.NotFound($"Không tìm thấy bài kiểm tra nguồn với ID {request.SourceAssignmentId}.");
            }

            var targetLesson = await _context.Lessons
                .Include(l => l.Class)
                .FirstOrDefaultAsync(l => l.Id == request.TargetLessonId);

            if (targetLesson == null)
            {
                throw ApiException.NotFound($"Không tìm thấy buổi học đích với ID {request.TargetLessonId}.");
            }

            var startTime = request.StartTime ?? DateTime.UtcNow;
            var deadline = request.Deadline ?? startTime.AddDays(7);

            if (deadline <= startTime)
            {
                throw ApiException.BadRequest("Hạn nộp bài phải sau thời gian bắt đầu.");
            }

            // Nhân bản bài kiểm tra vào buổi học mới
            var newAssignment = new Assignment
            {
                LessonId = request.TargetLessonId,
                StageId = source.StageId,
                Title = !string.IsNullOrWhiteSpace(request.NewTitle) ? request.NewTitle.Trim() : $"{source.Title} (Bản sao)",
                StartTime = startTime,
                Deadline = deadline,
                TimeLimitMinutes = source.TimeLimitMinutes,
                MaxAttempt = source.MaxAttempt,
                Status = "OPEN"
            };

            _context.Assignments.Add(newAssignment);
            await _context.SaveChangesAsync();

            // Sao chép toàn bộ câu hỏi và cơ cấu điểm
            foreach (var qa in source.QuestionAssignments)
            {
                _context.QuestionAssignments.Add(new QuestionAssignment
                {
                    AssignmentId = newAssignment.Id,
                    QuestionId = qa.QuestionId,
                    OrderIndex = qa.OrderIndex,
                    PointValue = qa.PointValue
                });
            }

            // Phân phối học sinh cho lớp của buổi học đích
            var targetClassStudents = await _context.ClassStudents
                .Where(cs => cs.ClassId == targetLesson.ClassId && cs.Status == "ACTIVE")
                .Select(cs => cs.StudentId)
                .ToListAsync();

            var studentsToAssign = new List<int>();

            if (request.AssignAllStudentsInClass)
            {
                studentsToAssign = targetClassStudents;
            }
            else if (request.SpecificStudentIds != null && request.SpecificStudentIds.Any())
            {
                var distinctSpecificIds = request.SpecificStudentIds.Distinct().ToList();
                var invalid = distinctSpecificIds.Except(targetClassStudents).ToList();
                if (invalid.Any())
                {
                    throw ApiException.BadRequest($"Học sinh ID [{string.Join(", ", invalid)}] không thuộc lớp của buổi học đích.");
                }
                studentsToAssign = distinctSpecificIds;
            }

            foreach (var sId in studentsToAssign)
            {
                _context.StudentAssignments.Add(new StudentAssignment
                {
                    AssignmentId = newAssignment.Id,
                    StudentId = sId,
                    Status = "ASSIGNED"
                });
            }

            await _context.SaveChangesAsync();

            var result = await GetAssignmentByIdAsync(newAssignment.Id);
            return result!;
        }

        public async Task<IEnumerable<AssignmentResponse>> GetAssignmentsByLessonIdAsync(int lessonId)
        {
            var lessonExists = await _context.Lessons.AnyAsync(l => l.Id == lessonId);
            if (!lessonExists)
            {
                throw ApiException.NotFound($"Không tìm thấy buổi học với ID {lessonId}.");
            }

            return await _context.Assignments
                .Include(a => a.Lesson)
                .ThenInclude(l => l.Class)
                .Include(a => a.QuestionAssignments)
                .Include(a => a.StudentAssignments)
                .Where(a => a.LessonId == lessonId)
                .OrderByDescending(a => a.StartTime)
                .Select(a => new AssignmentResponse
                {
                    Id = a.Id,
                    LessonId = a.LessonId,
                    LessonTitle = a.Lesson.Title,
                    LessonDate = a.Lesson.LessonDate,
                    ClassId = a.Lesson.ClassId,
                    ClassName = a.Lesson.Class.Name,
                    StageId = a.StageId,
                    Title = a.Title,
                    StartTime = a.StartTime,
                    Deadline = a.Deadline,
                    TimeLimitMinutes = a.TimeLimitMinutes,
                    MaxAttempt = a.MaxAttempt,
                    Status = a.Status,
                    TotalQuestions = a.QuestionAssignments.Count,
                    TotalPoints = a.QuestionAssignments.Sum(q => q.PointValue),
                    AssignedStudentsCount = a.StudentAssignments.Count,
                    CompletedStudentsCount = a.StudentAssignments.Count(sa => sa.Status == "GRADED" || sa.Status == "SUBMITTED")
                })
                .ToListAsync();
        }

        public async Task<AssignmentDetailResponse?> GetAssignmentByIdAsync(int id)
        {
            var a = await _context.Assignments
                .Include(a => a.Lesson)
                .ThenInclude(l => l.Class)
                .Include(a => a.QuestionAssignments)
                .ThenInclude(qa => qa.Question)
                .Include(a => a.StudentAssignments)
                .ThenInclude(sa => sa.Student)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (a == null) return null;

            return new AssignmentDetailResponse
            {
                Id = a.Id,
                LessonId = a.LessonId,
                LessonTitle = a.Lesson?.Title,
                LessonDate = a.Lesson?.LessonDate ?? DateTime.MinValue,
                ClassId = a.Lesson?.ClassId ?? 0,
                ClassName = a.Lesson?.Class?.Name ?? string.Empty,
                StageId = a.StageId,
                Title = a.Title,
                StartTime = a.StartTime,
                Deadline = a.Deadline,
                TimeLimitMinutes = a.TimeLimitMinutes,
                MaxAttempt = a.MaxAttempt,
                Status = a.Status,
                TotalQuestions = a.QuestionAssignments.Count,
                TotalPoints = a.QuestionAssignments.Sum(q => q.PointValue),
                AssignedStudentsCount = a.StudentAssignments.Count,
                CompletedStudentsCount = a.StudentAssignments.Count(sa => sa.Status == "GRADED" || sa.Status == "SUBMITTED"),
                Questions = a.QuestionAssignments
                    .OrderBy(qa => qa.OrderIndex)
                    .Select(qa => new AssignmentQuestionDetailDto
                    {
                        QuestionAssignmentId = qa.Id,
                        QuestionId = qa.QuestionId,
                        OrderIndex = qa.OrderIndex,
                        PointValue = qa.PointValue,
                        Type = qa.Question.Type,
                        Content = qa.Question.Content,
                        ImageUrl = qa.Question.ImageUrl,
                        Difficulty = qa.Question.Difficulty,
                        Answer = qa.Question.Answer
                    }).ToList(),
                AssignedStudents = a.StudentAssignments
                    .OrderBy(sa => sa.Student.FullName)
                    .Select(sa => new StudentAssignmentResponse
                    {
                        Id = sa.Id,
                        AssignmentId = sa.AssignmentId,
                        StudentId = sa.StudentId,
                        StudentName = sa.Student.FullName,
                        Avatar = sa.Student.Avatar,
                        Score = sa.Score,
                        Status = sa.Status,
                        TeacherFeedback = sa.TeacherFeedback,
                        CompletedAt = sa.CompletedAt
                    }).ToList()
            };
        }

        public async Task<bool> UpdateAssignmentAsync(int id, UpdateAssignmentRequest request)
        {
            var assignment = await _context.Assignments
                .Include(a => a.QuestionAssignments)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return false;

            if (request.Deadline <= request.StartTime)
            {
                throw ApiException.BadRequest("Hạn nộp bài phải sau thời gian bắt đầu.");
            }

            assignment.StageId = request.StageId;
            assignment.Title = request.Title.Trim();
            assignment.StartTime = request.StartTime;
            assignment.Deadline = request.Deadline;
            assignment.TimeLimitMinutes = request.TimeLimitMinutes;
            assignment.MaxAttempt = request.MaxAttempt;
            assignment.Status = request.Status.Trim().ToUpper();

            // Nếu có cập nhật lại danh sách câu hỏi
            if (request.Questions != null)
            {
                var inputQIds = request.Questions.Select(q => q.QuestionId).Distinct().ToList();
                var validQuestions = await _context.Questions
                    .Where(q => inputQIds.Contains(q.Id))
                    .Select(q => q.Id)
                    .ToListAsync();

                var invalid = inputQIds.Except(validQuestions).ToList();
                if (invalid.Any())
                {
                    throw ApiException.BadRequest($"Các câu hỏi có ID [{string.Join(", ", invalid)}] không tồn tại.");
                }

                _context.QuestionAssignments.RemoveRange(assignment.QuestionAssignments);

                int order = 1;
                foreach (var q in request.Questions)
                {
                    _context.QuestionAssignments.Add(new QuestionAssignment
                    {
                        AssignmentId = assignment.Id,
                        QuestionId = q.QuestionId,
                        OrderIndex = q.OrderIndex > 0 ? q.OrderIndex : order++,
                        PointValue = q.PointValue > 0 ? q.PointValue : 1.0f
                    });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAssignmentAsync(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.QuestionAssignments)
                .Include(a => a.StudentAssignments)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return false;

            // Xóa các liên kết trước
            _context.QuestionAssignments.RemoveRange(assignment.QuestionAssignments);
            _context.StudentAssignments.RemoveRange(assignment.StudentAssignments);
            _context.Assignments.Remove(assignment);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AssignmentDetailResponse> AssignStudentsAsync(int assignmentId, List<int> studentIds)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Lesson)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null)
            {
                throw ApiException.NotFound($"Không tìm thấy bài kiểm tra với ID {assignmentId}.");
            }

            var activeClassStudents = await _context.ClassStudents
                .Where(cs => cs.ClassId == assignment.Lesson.ClassId && cs.Status == "ACTIVE")
                .Select(cs => cs.StudentId)
                .ToListAsync();

            var distinctIds = studentIds.Distinct().ToList();
            var invalid = distinctIds.Except(activeClassStudents).ToList();
            if (invalid.Any())
            {
                throw ApiException.BadRequest($"Học sinh ID [{string.Join(", ", invalid)}] không thuộc lớp học này.");
            }

            var alreadyAssignedIds = await _context.StudentAssignments
                .Where(sa => sa.AssignmentId == assignmentId)
                .Select(sa => sa.StudentId)
                .ToListAsync();

            var newStudentIds = distinctIds.Except(alreadyAssignedIds).ToList();

            foreach (var sId in newStudentIds)
            {
                _context.StudentAssignments.Add(new StudentAssignment
                {
                    AssignmentId = assignmentId,
                    StudentId = sId,
                    Status = "ASSIGNED"
                });
            }

            await _context.SaveChangesAsync();

            var result = await GetAssignmentByIdAsync(assignmentId);
            return result!;
        }

        public async Task<bool> UnassignStudentAsync(int assignmentId, int studentId)
        {
            var sa = await _context.StudentAssignments
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);

            if (sa == null) return false;

            if (sa.Status == "SUBMITTED" || sa.Status == "GRADED")
            {
                throw ApiException.BadRequest("Không thể hủy giao bài vì học sinh đã nộp bài hoặc đã được chấm điểm.");
            }

            _context.StudentAssignments.Remove(sa);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<AssignmentResponse>> GetAssignmentsByStudentAsync(int studentId, int? lessonId = null)
        {
            var query = _context.StudentAssignments
                .Include(sa => sa.Assignment)
                .ThenInclude(a => a.Lesson)
                .ThenInclude(l => l.Class)
                .Where(sa => sa.StudentId == studentId);

            if (lessonId.HasValue)
            {
                query = query.Where(sa => sa.Assignment.LessonId == lessonId.Value);
            }

            return await query
                .OrderByDescending(sa => sa.Assignment.StartTime)
                .Select(sa => new AssignmentResponse
                {
                    Id = sa.Assignment.Id,
                    LessonId = sa.Assignment.LessonId,
                    LessonTitle = sa.Assignment.Lesson.Title,
                    LessonDate = sa.Assignment.Lesson.LessonDate,
                    ClassId = sa.Assignment.Lesson.ClassId,
                    ClassName = sa.Assignment.Lesson.Class.Name,
                    StageId = sa.Assignment.StageId,
                    Title = sa.Assignment.Title,
                    StartTime = sa.Assignment.StartTime,
                    Deadline = sa.Assignment.Deadline,
                    TimeLimitMinutes = sa.Assignment.TimeLimitMinutes,
                    MaxAttempt = sa.Assignment.MaxAttempt,
                    Status = sa.Assignment.Status,
                    TotalQuestions = sa.Assignment.QuestionAssignments.Count,
                    TotalPoints = sa.Assignment.QuestionAssignments.Sum(qa => qa.PointValue),
                    AssignedStudentsCount = sa.Assignment.StudentAssignments.Count,
                    CompletedStudentsCount = sa.Assignment.StudentAssignments.Count(s => s.Status == "GRADED" || s.Status == "SUBMITTED")
                })
                .ToListAsync();
        }

        public async Task<StudentAssignmentResponse?> GradeStudentAsync(int assignmentId, int studentId, GradeStudentRequest request)
        {
            var sa = await _context.StudentAssignments
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);

            if (sa == null) return null;

            sa.Score = request.Score;
            sa.TeacherFeedback = request.TeacherFeedback;
            sa.Status = string.IsNullOrWhiteSpace(request.Status) ? "GRADED" : request.Status.Trim().ToUpper();
            if (sa.CompletedAt == null)
            {
                sa.CompletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return new StudentAssignmentResponse
            {
                Id = sa.Id,
                AssignmentId = sa.AssignmentId,
                StudentId = sa.StudentId,
                StudentName = sa.Student?.FullName ?? string.Empty,
                Avatar = sa.Student?.Avatar,
                Score = sa.Score,
                Status = sa.Status,
                TeacherFeedback = sa.TeacherFeedback,
                CompletedAt = sa.CompletedAt
            };
        }

        public async Task<bool> BatchGradeAsync(int assignmentId, List<BatchGradeItemRequest> grades)
        {
            if (grades == null || !grades.Any()) return true;

            var studentIds = grades.Select(g => g.StudentId).ToList();
            var list = await _context.StudentAssignments
                .Where(s => s.AssignmentId == assignmentId && studentIds.Contains(s.StudentId))
                .ToListAsync();

            foreach (var sa in list)
            {
                var gradeInfo = grades.FirstOrDefault(g => g.StudentId == sa.StudentId);
                if (gradeInfo != null)
                {
                    if (gradeInfo.Score.HasValue)
                    {
                        sa.Score = gradeInfo.Score.Value;
                    }
                    if (gradeInfo.TeacherFeedback != null)
                    {
                        sa.TeacherFeedback = gradeInfo.TeacherFeedback;
                    }
                    if (!string.IsNullOrWhiteSpace(gradeInfo.Status))
                    {
                        sa.Status = gradeInfo.Status.Trim().ToUpper();
                    }
                    else if (gradeInfo.Score.HasValue)
                    {
                        sa.Status = "GRADED";
                    }
                    if (sa.CompletedAt == null && sa.Score.HasValue)
                    {
                        sa.CompletedAt = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<StudentSubmissionDetailResponse?> GetStudentSubmissionAsync(int assignmentId, int studentId)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Lesson)
                .ThenInclude(l => l.Class)
                .Include(a => a.QuestionAssignments)
                .ThenInclude(qa => qa.Question)
                .Include(a => a.StudentAssignments)
                .ThenInclude(sa => sa.Student)
                .FirstOrDefaultAsync(a => a.Id == assignmentId);

            if (assignment == null) return null;

            var sa = assignment.StudentAssignments.FirstOrDefault(s => s.StudentId == studentId);
            if (sa == null) return null;

            return new StudentSubmissionDetailResponse
            {
                AssignmentId = assignment.Id,
                AssignmentTitle = assignment.Title,
                LessonId = assignment.LessonId,
                LessonTitle = assignment.Lesson?.Title,
                ClassId = assignment.Lesson?.ClassId ?? 0,
                ClassName = assignment.Lesson?.Class?.Name ?? string.Empty,
                TimeLimitMinutes = assignment.TimeLimitMinutes,
                TotalPoints = assignment.QuestionAssignments.Sum(q => q.PointValue),
                StartTime = assignment.StartTime,
                Deadline = assignment.Deadline,
                StudentId = sa.StudentId,
                StudentName = sa.Student?.FullName ?? string.Empty,
                StudentAvatar = sa.Student?.Avatar,
                Score = sa.Score,
                Status = sa.Status,
                TeacherFeedback = sa.TeacherFeedback,
                CompletedAt = sa.CompletedAt,
                Questions = assignment.QuestionAssignments
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new AssignmentQuestionDetailDto
                    {
                        QuestionAssignmentId = q.Id,
                        QuestionId = q.QuestionId,
                        OrderIndex = q.OrderIndex,
                        PointValue = q.PointValue,
                        Type = q.Question?.Type ?? string.Empty,
                        Content = q.Question?.Content ?? string.Empty,
                        ImageUrl = q.Question?.ImageUrl,
                        Difficulty = q.Question?.Difficulty ?? string.Empty,
                        Answer = q.Question?.Answer ?? string.Empty
                    }).ToList()
            };
        }
    }
}
