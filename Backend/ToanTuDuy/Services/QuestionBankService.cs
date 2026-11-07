using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs;
using ToanTuDuy.Exceptions;
using ToanTuDuy.Models;

namespace ToanTuDuy.Services
{
    public class QuestionBankService : IQuestionBankService
    {
        private readonly ApplicationDbContext _context;

        public QuestionBankService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Quản lý Ngân hàng câu hỏi (QuestionBank)

        public async Task<IEnumerable<QuestionBankResponse>> GetAllBanksAsync(int? courseId = null, string? keyword = null)
        {
            var query = _context.QuestionBanks
                .Include(b => b.Course)
                .Include(b => b.Questions)
                .AsQueryable();

            if (courseId.HasValue)
            {
                query = query.Where(b => b.CourseId == courseId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(b => b.Name.ToLower().Contains(kw) || (b.Description != null && b.Description.ToLower().Contains(kw)));
            }

            return await query
                .OrderByDescending(b => b.Id)
                .Select(b => new QuestionBankResponse
                {
                    Id = b.Id,
                    CourseId = b.CourseId,
                    CourseName = b.Course != null ? b.Course.Name : string.Empty,
                    Name = b.Name,
                    Description = b.Description,
                    TotalQuestions = b.Questions.Count
                })
                .ToListAsync();
        }

        public async Task<QuestionBankDetailResponse?> GetBankByIdAsync(int id)
        {
            var bank = await _context.QuestionBanks
                .Include(b => b.Course)
                .Include(b => b.Questions)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bank == null) return null;

            return new QuestionBankDetailResponse
            {
                Id = bank.Id,
                CourseId = bank.CourseId,
                CourseName = bank.Course?.Name ?? string.Empty,
                Name = bank.Name,
                Description = bank.Description,
                TotalQuestions = bank.Questions.Count,
                Questions = bank.Questions.Select(q => new QuestionResponse
                {
                    Id = q.Id,
                    BankId = q.BankId,
                    BankName = bank.Name,
                    Type = q.Type,
                    Content = q.Content,
                    ImageUrl = q.ImageUrl,
                    Difficulty = q.Difficulty,
                    Answer = q.Answer
                }).ToList()
            };
        }

        public async Task<QuestionBankResponse> CreateBankAsync(QuestionBankRequest request)
        {
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId);
            if (!courseExists)
            {
                throw ApiException.NotFound($"Không tìm thấy khóa học với ID {request.CourseId}.");
            }

            var bank = new QuestionBank
            {
                CourseId = request.CourseId,
                Name = request.Name.Trim(),
                Description = request.Description?.Trim()
            };

            _context.QuestionBanks.Add(bank);
            await _context.SaveChangesAsync();

            var course = await _context.Courses.FindAsync(request.CourseId);

            return new QuestionBankResponse
            {
                Id = bank.Id,
                CourseId = bank.CourseId,
                CourseName = course?.Name ?? string.Empty,
                Name = bank.Name,
                Description = bank.Description,
                TotalQuestions = 0
            };
        }

        public async Task<bool> UpdateBankAsync(int id, QuestionBankRequest request)
        {
            var bank = await _context.QuestionBanks.FindAsync(id);
            if (bank == null) return false;

            var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId);
            if (!courseExists)
            {
                throw ApiException.NotFound($"Không tìm thấy khóa học với ID {request.CourseId}.");
            }

            bank.CourseId = request.CourseId;
            bank.Name = request.Name.Trim();
            bank.Description = request.Description?.Trim();

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteBankAsync(int id)
        {
            var bank = await _context.QuestionBanks
                .Include(b => b.Questions)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bank == null) return false;

            var questionIds = bank.Questions.Select(q => q.Id).ToList();

            // Gỡ các câu hỏi khỏi bài kiểm tra trước khi xóa để không vi phạm khóa ngoại
            // (QUESTION_ASSIGNMENT đang dùng DeleteBehavior.Restrict).
            var assignments = await _context.QuestionAssignments
                .Where(qa => questionIds.Contains(qa.QuestionId))
                .ToListAsync();
            if (assignments.Count > 0)
            {
                _context.QuestionAssignments.RemoveRange(assignments);
            }

            _context.Questions.RemoveRange(bank.Questions);
            _context.QuestionBanks.Remove(bank);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Quản lý Câu hỏi (Question)

        public async Task<IEnumerable<QuestionResponse>> GetQuestionsByBankAsync(int bankId, string? difficulty = null, string? type = null)
        {
            var bank = await _context.QuestionBanks.FindAsync(bankId);
            if (bank == null)
            {
                throw ApiException.NotFound($"Không tìm thấy ngân hàng câu hỏi với ID {bankId}.");
            }

            var query = _context.Questions
                .Where(q => q.BankId == bankId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                query = query.Where(q => q.Difficulty.ToUpper() == difficulty.Trim().ToUpper());
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(q => q.Type.ToUpper() == type.Trim().ToUpper());
            }

            return await query
                .OrderBy(q => q.Id)
                .Select(q => new QuestionResponse
                {
                    Id = q.Id,
                    BankId = q.BankId,
                    BankName = bank.Name,
                    Type = q.Type,
                    Content = q.Content,
                    ImageUrl = q.ImageUrl,
                    Difficulty = q.Difficulty,
                    Answer = q.Answer
                })
                .ToListAsync();
        }

        public async Task<QuestionResponse?> GetQuestionByIdAsync(int id)
        {
            var q = await _context.Questions
                .Include(q => q.Bank)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (q == null) return null;

            return new QuestionResponse
            {
                Id = q.Id,
                BankId = q.BankId,
                BankName = q.Bank?.Name ?? string.Empty,
                Type = q.Type,
                Content = q.Content,
                ImageUrl = q.ImageUrl,
                Difficulty = q.Difficulty,
                Answer = q.Answer
            };
        }

        public async Task<QuestionResponse> CreateQuestionAsync(QuestionRequest request)
        {
            var bank = await _context.QuestionBanks.FindAsync(request.BankId);
            if (bank == null)
            {
                throw ApiException.NotFound($"Không tìm thấy ngân hàng câu hỏi với ID {request.BankId}.");
            }

            var question = new Question
            {
                BankId = request.BankId,
                Type = request.Type.Trim().ToUpper(),
                Content = request.Content.Trim(),
                ImageUrl = request.ImageUrl?.Trim(),
                Difficulty = request.Difficulty.Trim().ToUpper(),
                Answer = request.Answer.Trim()
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return new QuestionResponse
            {
                Id = question.Id,
                BankId = question.BankId,
                BankName = bank.Name,
                Type = question.Type,
                Content = question.Content,
                ImageUrl = question.ImageUrl,
                Difficulty = question.Difficulty,
                Answer = question.Answer
            };
        }

        public async Task<IEnumerable<QuestionResponse>> BulkCreateQuestionsAsync(BulkQuestionCreateRequest request)
        {
            var bank = await _context.QuestionBanks.FindAsync(request.BankId);
            if (bank == null)
            {
                throw ApiException.NotFound($"Không tìm thấy ngân hàng câu hỏi với ID {request.BankId}.");
            }

            if (request.Questions == null || !request.Questions.Any())
            {
                return Enumerable.Empty<QuestionResponse>();
            }

            var questionEntities = request.Questions.Select(q => new Question
            {
                BankId = request.BankId,
                Type = string.IsNullOrWhiteSpace(q.Type) ? "MULTIPLE_CHOICE" : q.Type.Trim().ToUpper(),
                Content = q.Content.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(q.ImageUrl) ? null : q.ImageUrl.Trim(),
                Difficulty = string.IsNullOrWhiteSpace(q.Difficulty) ? "EASY" : q.Difficulty.Trim().ToUpper(),
                Answer = q.Answer.Trim()
            }).ToList();

            await _context.Questions.AddRangeAsync(questionEntities);
            await _context.SaveChangesAsync();

            return questionEntities.Select(question => new QuestionResponse
            {
                Id = question.Id,
                BankId = question.BankId,
                BankName = bank.Name,
                Type = question.Type,
                Content = question.Content,
                ImageUrl = question.ImageUrl,
                Difficulty = question.Difficulty,
                Answer = question.Answer
            }).ToList();
        }

        public async Task<bool> UpdateQuestionAsync(int id, QuestionRequest request)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return false;

            var bankExists = await _context.QuestionBanks.AnyAsync(b => b.Id == request.BankId);
            if (!bankExists)
            {
                throw ApiException.NotFound($"Không tìm thấy ngân hàng câu hỏi với ID {request.BankId}.");
            }

            question.BankId = request.BankId;
            question.Type = request.Type.Trim().ToUpper();
            question.Content = request.Content.Trim();
            question.ImageUrl = request.ImageUrl?.Trim();
            question.Difficulty = request.Difficulty.Trim().ToUpper();
            question.Answer = request.Answer.Trim();

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteQuestionAsync(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return false;

            // Gỡ câu hỏi khỏi bài kiểm tra trước khi xóa để không vi phạm khóa ngoại.
            var assignments = await _context.QuestionAssignments
                .Where(qa => qa.QuestionId == id)
                .ToListAsync();
            if (assignments.Count > 0)
            {
                _context.QuestionAssignments.RemoveRange(assignments);
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BulkQuestionDeleteResponse> DeleteQuestionsAsync(IEnumerable<int> ids)
        {
            var distinctIds = (ids ?? Enumerable.Empty<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (distinctIds.Count == 0)
            {
                return new BulkQuestionDeleteResponse
                {
                    DeletedCount = 0,
                    RemovedAssignmentCount = 0,
                    Message = "Vui lòng chọn ít nhất một câu hỏi để xóa."
                };
            }

            var questions = await _context.Questions
                .Where(q => distinctIds.Contains(q.Id))
                .ToListAsync();

            if (questions.Count == 0)
            {
                return new BulkQuestionDeleteResponse
                {
                    DeletedCount = 0,
                    RemovedAssignmentCount = 0,
                    Message = "Không tìm thấy câu hỏi nào để xóa."
                };
            }

            var existingIds = questions.Select(q => q.Id).ToList();
            var assignments = await _context.QuestionAssignments
                .Where(qa => existingIds.Contains(qa.QuestionId))
                .ToListAsync();

            if (assignments.Count > 0)
            {
                _context.QuestionAssignments.RemoveRange(assignments);
            }

            _context.Questions.RemoveRange(questions);
            await _context.SaveChangesAsync();

            var message = assignments.Count > 0
                ? $"Đã xóa {questions.Count} câu hỏi và gỡ {assignments.Count} liên kết khỏi bài kiểm tra."
                : $"Đã xóa {questions.Count} câu hỏi.";

            return new BulkQuestionDeleteResponse
            {
                DeletedCount = questions.Count,
                RemovedAssignmentCount = assignments.Count,
                Message = message
            };
        }

        #endregion
    }
}
