using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            if (request.Questions == null || request.Questions.Count == 0)
            {
                throw ApiException.BadRequest("Cần chọn ít nhất một câu hỏi trước khi giao bài.");
            }

            if (request.MaxAttempt.GetValueOrDefault(1) != 1)
            {
                throw ApiException.BadRequest("Hiện tại mỗi bài tập chỉ hỗ trợ một lần nộp.");
            }

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

            var requestedQuestions = request.Questions ?? new List<QuestionAssignmentItemDto>();
            var requestedQuestionIds = requestedQuestions
                .Select(question => question.QuestionId)
                .Distinct()
                .ToList();
            var bankQuestions = requestedQuestionIds.Count == 0
                ? new List<Question>()
                : await _context.Questions
                    .Where(question => requestedQuestionIds.Contains(question.Id))
                    .ToListAsync();
            var existingQuestionIds = bankQuestions.Select(question => question.Id).ToHashSet();
            var invalidQuestionIds = requestedQuestionIds
                .Where(questionId => !existingQuestionIds.Contains(questionId))
                .ToList();
            if (invalidQuestionIds.Count > 0)
            {
                throw ApiException.BadRequest(
                    $"Các câu hỏi có ID [{string.Join(", ", invalidQuestionIds)}] không tồn tại.");
            }

            var questionsById = bankQuestions.ToDictionary(question => question.Id);
            foreach (var requestedQuestion in requestedQuestions)
            {
                EnsureSupportedPracticeQuestion(questionsById[requestedQuestion.QuestionId]);
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
                MaxAttempt = 1,
                Status = string.IsNullOrWhiteSpace(request.Status) ? "OPEN" : request.Status.Trim().ToUpper()
            };

            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            // Đính kèm các câu hỏi từ ngân hàng câu hỏi
            if (requestedQuestions.Count > 0)
            {
                int order = 1;
                foreach (var q in requestedQuestions)
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
                    .ThenInclude(questionAssignment => questionAssignment.Question)
                .FirstOrDefaultAsync(a => a.Id == request.SourceAssignmentId);

            if (source == null)
            {
                throw ApiException.NotFound($"Không tìm thấy bài kiểm tra nguồn với ID {request.SourceAssignmentId}.");
            }

            foreach (var questionAssignment in source.QuestionAssignments)
            {
                EnsureSupportedPracticeQuestion(questionAssignment.Question);
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
                MaxAttempt = 1,
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
            if (request.MaxAttempt.GetValueOrDefault(1) != 1)
            {
                throw ApiException.BadRequest("Hiện tại mỗi bài tập chỉ hỗ trợ một lần nộp.");
            }

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
            assignment.MaxAttempt = 1;
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

        public async Task<IEnumerable<StudentPracticeAssignmentDto>> GetStudentPracticeAssignmentsAsync(
            int accountId,
            int studentId)
        {
            var isParentOfStudent = await _context.StudentProfiles
                .AnyAsync(student => student.Id == studentId && student.Parent!.AccountId == accountId);

            if (!isParentOfStudent)
            {
                throw ApiException.NotFound("Không tìm thấy học sinh.");
            }

            var now = DateTime.UtcNow;
            return await _context.StudentAssignments
                .AsNoTracking()
                .Include(studentAssignment => studentAssignment.Assignment)
                    .ThenInclude(assignment => assignment.Lesson)
                .Include(studentAssignment => studentAssignment.Assignment)
                    .ThenInclude(assignment => assignment.QuestionAssignments)
                .Where(studentAssignment => studentAssignment.StudentId == studentId)
                .OrderByDescending(studentAssignment => studentAssignment.Assignment.StartTime)
                .Select(studentAssignment => new StudentPracticeAssignmentDto
                {
                    AssignmentId = studentAssignment.AssignmentId,
                    Title = studentAssignment.Assignment.Title,
                    LessonTitle = studentAssignment.Assignment.Lesson.Title,
                    StartTime = studentAssignment.Assignment.StartTime,
                    Deadline = studentAssignment.Assignment.Deadline,
                    TimeLimitMinutes = studentAssignment.Assignment.TimeLimitMinutes,
                    TotalQuestions = studentAssignment.Assignment.QuestionAssignments.Count,
                    TotalPoints = studentAssignment.Assignment.QuestionAssignments.Sum(question => question.PointValue),
                    StudentStatus = studentAssignment.Status,
                    Score = studentAssignment.Score,
                    CompletedAt = studentAssignment.CompletedAt,
                    IsAvailable = studentAssignment.Assignment.Status == "OPEN"
                        && studentAssignment.Assignment.StartTime <= now
                        && studentAssignment.Assignment.Deadline >= now
                        && studentAssignment.Status != "GRADED"
                        && studentAssignment.Status != "SUBMITTED"
                })
                .ToListAsync();
        }

        public async Task<StudentPracticeAssignmentDetailDto?> GetStudentPracticeAssignmentDetailAsync(
            int accountId,
            int assignmentId,
            int studentId)
        {
            var studentAssignment = await FindStudentAssignmentForPracticeAsync(accountId, assignmentId, studentId);
            if (studentAssignment == null)
            {
                return null;
            }

            EnsureAssignmentIsOpen(studentAssignment.Assignment);
            if (studentAssignment.Status == "GRADED" || studentAssignment.Status == "SUBMITTED")
            {
                throw ApiException.BadRequest("Bài tập này đã được nộp.");
            }
            EnsureSupportedPracticeQuestions(studentAssignment.Assignment);
            return MapStudentPracticeAssignmentDetail(studentAssignment);
        }

        public async Task<StudentPracticeSubmissionResponseDto?> SubmitStudentPracticeAssignmentAsync(
            int accountId,
            int assignmentId,
            int studentId,
            SubmitStudentPracticeRequest request)
        {
            var studentAssignment = await FindStudentAssignmentForPracticeAsync(accountId, assignmentId, studentId);
            if (studentAssignment == null)
            {
                return null;
            }

            EnsureAssignmentIsOpen(studentAssignment.Assignment);
            if (studentAssignment.Status == "GRADED" || studentAssignment.Status == "SUBMITTED")
            {
                throw ApiException.BadRequest("Bài tập này đã được nộp.");
            }

            var now = DateTime.UtcNow;
            var questions = studentAssignment.Assignment.QuestionAssignments
                .OrderBy(question => question.OrderIndex)
                .ToList();
            EnsureSupportedPracticeQuestions(studentAssignment.Assignment);
            var answersByQuestionId = request.Answers
                .GroupBy(answer => answer.QuestionAssignmentId)
                .ToDictionary(group => group.Key, group => group.ToList());

            if (answersByQuestionId.Count != questions.Count
                || questions.Any(question => !answersByQuestionId.TryGetValue(question.Id, out var answers) || answers.Count != 1)
                || answersByQuestionId.Keys.Any(id => questions.All(question => question.Id != id)))
            {
                throw ApiException.BadRequest("Cần gửi đúng một câu trả lời cho mỗi câu hỏi trong bài tập.");
            }

            var score = 0f;
            var correctCount = 0;
            foreach (var questionAssignment in questions)
            {
                var answerText = answersByQuestionId[questionAssignment.Id][0].Answer.Trim();
                var (isCorrect, pointsAwarded) = GradeAnswer(questionAssignment.Question, answerText, questionAssignment.PointValue);
                if (isCorrect == true)
                {
                    correctCount += 1;
                    score += pointsAwarded;
                }
            }

            var updatedRows = await _context.StudentAssignments
                .Where(item => item.Id == studentAssignment.Id
                    && item.Status != "SUBMITTED"
                    && item.Status != "GRADED")
                .ExecuteUpdateAsync(updates => updates
                    .SetProperty(item => item.Score, score)
                    .SetProperty(item => item.Status, "SUBMITTED")
                    .SetProperty(item => item.CompletedAt, now));
            if (updatedRows == 0)
            {
                throw ApiException.BadRequest("Bài tập này đã được nộp.");
            }

            return new StudentPracticeSubmissionResponseDto
            {
                AssignmentId = assignmentId,
                Status = "SUBMITTED",
                Score = score,
                TotalPoints = questions.Sum(question => question.PointValue),
                CorrectCount = correctCount,
                TotalQuestions = questions.Count,
                SubmittedAt = now
            };
        }

        private async Task<StudentAssignment?> FindStudentAssignmentForPracticeAsync(
            int accountId,
            int assignmentId,
            int studentId)
        {
            var isParentOfStudent = await _context.StudentProfiles
                .AnyAsync(student => student.Id == studentId && student.Parent!.AccountId == accountId);
            if (!isParentOfStudent)
            {
                return null;
            }

            return await _context.StudentAssignments
                .Include(studentAssignment => studentAssignment.Assignment)
                    .ThenInclude(assignment => assignment.Lesson)
                .Include(studentAssignment => studentAssignment.Assignment)
                    .ThenInclude(assignment => assignment.QuestionAssignments)
                        .ThenInclude(questionAssignment => questionAssignment.Question)
                .FirstOrDefaultAsync(studentAssignment =>
                    studentAssignment.StudentId == studentId
                    && studentAssignment.AssignmentId == assignmentId);
        }

        private static void EnsureAssignmentIsOpen(Assignment assignment)
        {
            var now = DateTime.UtcNow;
            if (!string.Equals(assignment.Status, "OPEN", StringComparison.OrdinalIgnoreCase))
            {
                throw ApiException.BadRequest("Bài tập hiện không mở.");
            }
            if (now < assignment.StartTime)
            {
                throw ApiException.BadRequest("Bài tập chưa đến thời gian bắt đầu.");
            }
            if (now > assignment.Deadline)
            {
                throw ApiException.BadRequest("Bài tập đã quá hạn nộp.");
            }
        }

        private static void EnsureSupportedPracticeQuestions(Assignment assignment)
        {
            foreach (var questionAssignment in assignment.QuestionAssignments)
            {
                EnsureSupportedPracticeQuestion(questionAssignment.Question);
            }
        }

        private static void EnsureSupportedPracticeQuestion(Question question)
        {
            var type = question.Type.Trim().ToUpperInvariant();
            if (type != "MULTIPLE_CHOICE" && type != "TRUE_FALSE" && type != "FILL_IN" && type != "TIMED_SEQUENCE")
            {
                throw ApiException.BadRequest(
                    $"Loại câu hỏi '{question.Type}' chưa được hỗ trợ khi giao bài trên app. "
                    + "Các loại hiện hỗ trợ: MULTIPLE_CHOICE, TRUE_FALSE, FILL_IN, TIMED_SEQUENCE.");
            }

            if (type == "MULTIPLE_CHOICE" && ParseMultipleChoice(question.Content).Options.Count == 0)
            {
                throw ApiException.BadRequest("Câu trắc nghiệm cần có các lựa chọn theo định dạng A., B., C., D.");
            }
            if (type == "TIMED_SEQUENCE" && ParseTimedSequence(question.Content).Count < 2)
            {
                throw ApiException.BadRequest("Câu tính nhẩm theo nhịp cần có định dạng: Tính nhẩm theo nhịp: 1, +2, -3.");
            }
        }

        private static StudentPracticeAssignmentDetailDto MapStudentPracticeAssignmentDetail(
            StudentAssignment studentAssignment)
        {
            var assignment = studentAssignment.Assignment;
            var questions = assignment.QuestionAssignments
                .OrderBy(question => question.OrderIndex)
                .Select(questionAssignment =>
                {
                    var question = questionAssignment.Question;
                    var (content, options) = string.Equals(
                        question.Type,
                        "MULTIPLE_CHOICE",
                        StringComparison.OrdinalIgnoreCase)
                        ? ParseMultipleChoice(question.Content)
                        : (question.Content, new List<StudentPracticeQuestionOptionDto>());

                    return new StudentPracticeQuestionDto
                    {
                        QuestionAssignmentId = questionAssignment.Id,
                        OrderIndex = questionAssignment.OrderIndex,
                        PointValue = questionAssignment.PointValue,
                        Type = question.Type,
                        Content = content,
                        ImageUrl = question.ImageUrl,
                        Difficulty = question.Difficulty,
                        Options = options,
                        SequenceTerms = string.Equals(
                            question.Type,
                            "TIMED_SEQUENCE",
                            StringComparison.OrdinalIgnoreCase)
                            ? ParseTimedSequence(question.Content)
                            : new List<StudentPracticeSequenceTermDto>()
                    };
                })
                .ToList();

            return new StudentPracticeAssignmentDetailDto
            {
                AssignmentId = assignment.Id,
                Title = assignment.Title,
                LessonTitle = assignment.Lesson?.Title,
                Deadline = assignment.Deadline,
                TimeLimitMinutes = assignment.TimeLimitMinutes,
                TotalPoints = questions.Sum(question => question.PointValue),
                Questions = questions
            };
        }

        private static (bool? IsCorrect, float PointsAwarded) GradeAnswer(
            Question question,
            string studentAnswer,
            float pointValue)
        {
            var answer = question.Answer.Trim();
            switch (question.Type.ToUpperInvariant())
            {
                case "MULTIPLE_CHOICE":
                {
                    var correctOption = Regex.Match(answer, @"^\s*([A-D])(?:[\.:\)\s-]|$)", RegexOptions.IgnoreCase);
                    var selectedOption = studentAnswer.Trim();
                    var isCorrect = correctOption.Success
                        && string.Equals(correctOption.Groups[1].Value, selectedOption, StringComparison.OrdinalIgnoreCase);
                    return (isCorrect, isCorrect ? pointValue : 0f);
                }
                case "TRUE_FALSE":
                {
                    var correctValue = Regex.Match(answer, @"^\s*(Đúng|Sai)\b", RegexOptions.IgnoreCase);
                    var isCorrect = correctValue.Success
                        && string.Equals(
                            NormalizeAnswer(correctValue.Groups[1].Value),
                            NormalizeAnswer(studentAnswer),
                            StringComparison.OrdinalIgnoreCase);
                    return (isCorrect, isCorrect ? pointValue : 0f);
                }
                case "FILL_IN":
                case "TIMED_SEQUENCE":
                {
                    var correctValue = answer.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)[0].Trim();
                    var isCorrect = NormalizeAnswer(correctValue)
                        .Equals(NormalizeAnswer(studentAnswer), StringComparison.OrdinalIgnoreCase);
                    return (isCorrect, isCorrect ? pointValue : 0f);
                }
                default:
                    return (null, 0f);
            }
        }

        private static List<StudentPracticeSequenceTermDto> ParseTimedSequence(string content)
        {
            var sequenceMatch = Regex.Match(
                content,
                @"tính\s+nhẩm\s+theo\s+nhịp\s*:\s*(?<sequence>\d+\s*(?:[,;]\s*[+\-−]\s*\d+)+)",
                RegexOptions.IgnoreCase);
            if (!sequenceMatch.Success)
            {
                return new List<StudentPracticeSequenceTermDto>();
            }

            var tokens = Regex.Matches(sequenceMatch.Groups["sequence"].Value, @"(?<operator>[+\-−]?)\s*(?<value>\d+)");
            if (tokens.Count < 2 || tokens[0].Groups["operator"].Value.Length > 0)
            {
                return new List<StudentPracticeSequenceTermDto>();
            }

            var terms = new List<StudentPracticeSequenceTermDto>();
            foreach (Match token in tokens)
            {
                if (!int.TryParse(token.Groups["value"].Value, out var value))
                {
                    return new List<StudentPracticeSequenceTermDto>();
                }

                var operation = token.Groups["operator"].Value;
                terms.Add(new StudentPracticeSequenceTermDto
                {
                    Operator = operation.Length == 0 ? null : operation == "−" ? "-" : operation,
                    Value = value
                });
            }

            return terms;
        }

        private static string NormalizeAnswer(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormKC).Trim();
            return Regex.Replace(normalized, @"\s+", " ");
        }

        private static (string Content, List<StudentPracticeQuestionOptionDto> Options) ParseMultipleChoice(string content)
        {
            var pattern = @"(?:^|\n|\s)(?<key>[A-D])[\.:\)]\s*(?<value>[\s\S]*?)(?=(?:\s+[A-D][\.:\)]\s*)|$)";
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);
            if (matches.Count < 4)
            {
                return (content, new List<StudentPracticeQuestionOptionDto>());
            }

            var options = matches
                .Cast<Match>()
                .Take(4)
                .Select(match => new StudentPracticeQuestionOptionDto
                {
                    Key = match.Groups["key"].Value.ToUpperInvariant(),
                    Text = match.Groups["value"].Value.Trim().TrimEnd(';')
                })
                .ToList();

            var questionText = content.Substring(0, matches[0].Index).Trim();
            return (questionText, options);
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
