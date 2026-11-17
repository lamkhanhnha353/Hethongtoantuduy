
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ToanTuDuy.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        // SignalR sẽ tự động map Connection ID với Account ID từ Token JWT.
    }
}