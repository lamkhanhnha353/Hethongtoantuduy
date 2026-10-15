using Microsoft.AspNetCore.Mvc;

namespace ToanTuDuy.Filters
{
    /// <summary>
    /// Đánh dấu một action/controller là "thao tác đặc biệt" cần xác thực PIN của phụ huynh.
    /// Client gửi kèm PIN trong header <c>X-Pin</c> ngay trong chính request đó,
    /// nhờ vậy không cần gọi <c>POST /api/profile/pin/verify</c> trước (tránh 2 request).
    ///
    /// Ví dụ:
    /// <code>
    /// [HttpDelete("{id:int}")]
    /// [RequirePin]
    /// public async Task&lt;IActionResult&gt; Delete(int id) { ... }
    /// </code>
    /// </summary>
    public sealed class RequirePinAttribute : TypeFilterAttribute
    {
        public RequirePinAttribute() : base(typeof(PinFilter))
        {
        }
    }
}
