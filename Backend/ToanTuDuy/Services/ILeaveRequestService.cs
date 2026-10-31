
using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface ILeaveRequestService
    {
        Task<LeaveRequestResponse> CreateLeaveRequestAsync(int accountId, LeaveRequestCreateRequest request);
        Task<bool> CancelLeaveRequestAsync(int accountId, int requestId);
        Task<IEnumerable<LeaveRequestResponse>> GetPendingRequestsAsync();
        Task<bool> ProcessLeaveRequestAsync(int requestId, bool isApproved); // Admin duyệt
        Task<IEnumerable<LeaveRequestResponse>> GetRequestsByStudentIdAsync(int accountId, int studentId);
    }
}