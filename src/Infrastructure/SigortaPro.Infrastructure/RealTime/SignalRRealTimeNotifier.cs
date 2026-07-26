using Microsoft.AspNetCore.SignalR;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Notifications;

namespace SigortaPro.Infrastructure.RealTime;

// ADR-041: IRealTimeNotifier'ın SignalR implementasyonu. Application yalnızca soyut sözleşmeyi bilir;
// taşıyıcı (SignalR) burada kapsüllenir — ileride RabbitMQ/Azure Service Bus'a geçiş yalnızca yeni bir
// implementasyondur. İstemci tarafında dinlenen tek olay adı "notification"dır; yük, sözleşmeye ek olarak
// sunucu zaman damgası ve benzersiz kimlik taşır (bildirim merkezi listesi/rozeti için).
public sealed class SignalRRealTimeNotifier : IRealTimeNotifier
{
    public const string ClientEventName = "notification";

    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SignalRRealTimeNotifier(IHubContext<NotificationHub> hubContext, IDateTimeProvider dateTimeProvider)
    {
        _hubContext = hubContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task NotifyStaffAsync(RealTimeNotification notification, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(NotificationHub.StaffGroup)
            .SendAsync(ClientEventName, ToPayload(notification), cancellationToken);

    public Task NotifyUserAsync(Guid userId, RealTimeNotification notification, CancellationToken cancellationToken = default) =>
        _hubContext.Clients.Group(NotificationHub.UserGroup(userId.ToString()))
            .SendAsync(ClientEventName, ToPayload(notification), cancellationToken);

    // ADR-047 (additive): referenceCode toast'ta kısa operasyonel bağlam verir (ör. poliçe numarası);
    // detaylı anlatım Bildirim Merkezi'ndedir. Hassas veri taşınmaz.
    private object ToPayload(RealTimeNotification notification) => new
    {
        id = Guid.NewGuid(),
        type = notification.Type,
        severity = notification.Severity,
        title = notification.Title,
        message = notification.Message,
        referenceCode = notification.ReferenceCode,
        occurredAt = _dateTimeProvider.UtcNow,
    };
}
