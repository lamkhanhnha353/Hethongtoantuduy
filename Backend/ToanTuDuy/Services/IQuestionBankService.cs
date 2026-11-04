using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface IQuestionBankService
    {
        // Quản lý Ngân hàng câu hỏi
        Task<IEnumerable<QuestionBankResponse>> GetAllBanksAsync(int? courseId = null, string? keyword = null);
        Task<QuestionBankDetailResponse?> GetBankByIdAsync(int id);
        Task<QuestionBankResponse> CreateBankAsync(QuestionBankRequest request);
        Task<bool> UpdateBankAsync(int id, QuestionBankRequest request);
        Task<bool> DeleteBankAsync(int id);

        // Quản lý Câu hỏi trong Ngân hàng
        Task<IEnumerable<QuestionResponse>> GetQuestionsByBankAsync(int bankId, string? difficulty = null, string? type = null);
        Task<QuestionResponse?> GetQuestionByIdAsync(int id);
        Task<QuestionResponse> CreateQuestionAsync(QuestionRequest request);
        Task<IEnumerable<QuestionResponse>> BulkCreateQuestionsAsync(BulkQuestionCreateRequest request);
        Task<bool> UpdateQuestionAsync(int id, QuestionRequest request);
        Task<bool> DeleteQuestionAsync(int id);
    }
}
