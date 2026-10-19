using System.Threading.Tasks;
using ToanTuDuy.DTOs;

namespace ToanTuDuy.Services
{
    public interface IPasswordResetService
    {
        Task<ForgotPasswordResponse> ForgotPasswordAsync(string email);
    }
}
