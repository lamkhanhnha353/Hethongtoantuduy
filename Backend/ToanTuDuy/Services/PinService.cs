using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using ToanTuDuy.Data;
using ToanTuDuy.DTOs.Profile;
using ToanTuDuy.Exceptions;

namespace ToanTuDuy.Services
{
    public class PinService : IPinService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<Models.ParentProfile> _pinHasher;

        public PinService(ApplicationDbContext context, IPasswordHasher<Models.ParentProfile> pinHasher)
        {
            _context = context;
            _pinHasher = pinHasher;
        }

        public async Task SetAsync(int accountId, SetPinRequest request)
        {
            var parent = await _context.ParentProfiles
                .FirstOrDefaultAsync(p => p.AccountId == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");

            // Đã có PIN thì bắt buộc xác thực PIN hiện tại trước khi đổi.
            if (!string.IsNullOrEmpty(parent.Pin))
            {
                if (string.IsNullOrEmpty(request.CurrentPin))
                {
                    throw ApiException.BadRequest("Vui lòng nhập PIN hiện tại để đổi PIN.");
                }

                var check = _pinHasher.VerifyHashedPassword(parent, parent.Pin!, request.CurrentPin);
                if (check == PasswordVerificationResult.Failed)
                {
                    throw ApiException.BadRequest("PIN hiện tại không đúng.");
                }
            }

            parent.Pin = _pinHasher.HashPassword(parent, request.NewPin);
            await _context.SaveChangesAsync();
        }

        public async Task<PinVerificationResponse> VerifyAsync(int accountId, string pin)
        {
            var parent = await _context.ParentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.AccountId == accountId)
                ?? throw ApiException.NotFound("Không tìm thấy hồ sơ phụ huynh.");

            if (string.IsNullOrEmpty(parent.Pin))
            {
                throw ApiException.BadRequest("Chưa thiết lập PIN.");
            }

            var check = _pinHasher.VerifyHashedPassword(parent, parent.Pin, pin);

            return new PinVerificationResponse
            {
                Valid = check != PasswordVerificationResult.Failed,
            };
        }
    }
}
