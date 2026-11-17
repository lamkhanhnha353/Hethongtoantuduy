using System.Collections.Generic;
using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface IEnrollmentService
    {
        Task<IEnumerable<EnrollmentRegistrationResponse>> GetCourseLevelRegistrationsAsync(
            int accountId,
            int courseLevelId);

        Task<int> RegisterClassAsync(int accountId, EnrollmentRequest request);
    }
}
