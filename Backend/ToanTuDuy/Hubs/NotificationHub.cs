using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ToanTuDuy.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
