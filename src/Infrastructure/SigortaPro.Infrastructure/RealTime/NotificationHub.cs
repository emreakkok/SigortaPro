using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SigortaPro.Application.Common.Authorization;

namespace SigortaPro.Infrastructure.RealTime;

// ADR-041: Gerçek zamanlı bildirim hub'ı. Sunucu → istemci tek yönlü yayın kanalıdır; istemciden
// çağrılabilir bir metot yüzeyi yoktur (iş komutları her zaman HTTP/CQRS üzerinden yürür).
// Bağlanan kullanıcı rolüne göre gruplanır: Admin/Personel → "staff"; her kullanıcı ayrıca
// "user:{userId}" grubuna alınır (müşteri bildirimleri için hazır altyapı — MVP'de tüketici staff'tır).
[Authorize]
public sealed class NotificationHub : Hub
{
    public const string StaffGroup = "staff";

    public static string UserGroup(string userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;

        // ADR-060: staff kümesi (Admin ∪ Personel) tek kaynaktan (Roles.StaffRoles) türetilir.
        if (user is not null && Roles.StaffRoles.Any(user.IsInRole))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, StaffGroup);
        }

        // JWT 'sub' claim'i (MapInboundClaims=false) NameIdentifier olarak eşlidir.
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        await base.OnConnectedAsync();
    }
}
