
using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface ILeaveRequestService
    {
        Task<LeaveRequestResponse> CreateLeaveRequestAsync(int accountId, LeaveRequestCreateRequest request);
        Task<bool> CancelLeaveRequestAsync(int accountId, int requestId);
        Task<IEnumerable<LeaveRequestResponse>> GetPendingRequestsAsync(int teacherAccountId);
        Task<bool> ProcessLeaveRequestAsync(int teacherAccountId, int requestId, bool isApproved);
        Task<IEnumerable<LeaveRequestResponse>> GetRequestsByStudentIdAsync(int accountId, int studentId);
    }
}