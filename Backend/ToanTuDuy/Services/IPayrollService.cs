using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services;

public interface IPayrollService
{
    Task<IReadOnlyList<PayslipDto>> GetMonthlyPayrollsAsync(
        int month,
        int year,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PayslipDto>> GetMonthlyPayrollsPagedAsync(
        int month,
        int year,
        int page,
        int pageSize,
        string? keyword = null,
        string? emailStatus = null,
        CancellationToken cancellationToken = default);

    Task<PayslipDetailDto> GetTeacherPayslipDetailAsync(
        int teacherId,
        int month,
        int year,
        CancellationToken cancellationToken = default);

    Task<SalaryAdjustmentDto> AddSalaryAdjustmentAsync(
        CreateAdjustmentRequestDto request,
        CancellationToken cancellationToken = default);

    Task<TeacherBaseRateDto> UpdateTeacherBaseRateAsync(
        int teacherId,
        UpdateTeacherBaseRateRequestDto request,
        CancellationToken cancellationToken = default);

    Task<int?> GetTeacherIdForAccountAsync(
        int accountId,
        CancellationToken cancellationToken = default);

    Task DeleteSalaryAdjustmentAsync(
        int adjustmentId, 
        CancellationToken cancellationToken = default);
}
