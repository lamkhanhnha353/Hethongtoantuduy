using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface IQuestionBankService
    {
        // Quản lý Ngân hàng câu hỏi
        Task<IEnumerable<QuestionBankResponse>> GetAllBanksAsync(int? courseId = null, string? keyword = null);
        Task<PagedResult<QuestionBankResponse>> GetPagedBanksAsync(
            int page,
            int pageSize,
            int? courseId = null,
            string? keyword = null,
            CancellationToken cancellationToken = default);
        Task<QuestionBankDetailResponse?> GetBankByIdAsync(int id);
        Task<QuestionBankResponse> CreateBankAsync(QuestionBankRequest request);
        Task<bool> UpdateBankAsync(int id, QuestionBankRequest request);
        Task<bool> DeleteBankAsync(int id);

        // Quản lý Câu hỏi trong Ngân hàng
        Task<IEnumerable<QuestionResponse>> GetQuestionsByBankAsync(int bankId, string? difficulty = null, string? type = null);
        Task<PagedResult<QuestionResponse>> GetPagedQuestionsByBankAsync(
            int bankId,
            int page,
            int pageSize,
            string? difficulty = null,
            string? type = null,
            string? keyword = null,
            CancellationToken cancellationToken = default);
        Task<QuestionResponse?> GetQuestionByIdAsync(int id);
        Task<QuestionResponse> CreateQuestionAsync(QuestionRequest request);
        Task<IEnumerable<QuestionResponse>> BulkCreateQuestionsAsync(BulkQuestionCreateRequest request);
        Task<bool> UpdateQuestionAsync(int id, QuestionRequest request);
        Task<bool> DeleteQuestionAsync(int id);
        Task<BulkQuestionDeleteResponse> DeleteQuestionsAsync(IEnumerable<int> ids);
    }
}
