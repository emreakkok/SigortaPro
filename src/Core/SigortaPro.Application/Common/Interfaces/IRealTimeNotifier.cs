using SigortaPro.Application.Common.Notifications;

namespace SigortaPro.Application.Common.Interfaces;

// Gerçek zamanlı bildirim yayın soyutlaması (— arayüz Application'da, implementasyon
// Infrastructure'da). MVP implementasyonu SignalR'dır; sözleşme taşıyıcıdan
// bağımsız tutulur — ileride RabbitMQ/Azure Service Bus gibi altyapılara geçiş yalnızca yeni bir
// implementasyondur. Yayın hataları iş akışını ASLA bozmaz (implementasyon loglayıp yutar — bilinçli).
public interface IRealTimeNotifier
{
    // Acente personeline (Admin + Personel — "staff" grubu) yayın yapar.
    Task NotifyStaffAsync(RealTimeNotification notification, CancellationToken cancellationToken = default);

    // Belirli bir kullanıcıya ("user:{userId}" grubu) yayın yapar. Müşteri bildirimleri için
    // altyapı hazırdır; MVP'de aktif tüketici yalnızca staff akışıdır.
    Task NotifyUserAsync(Guid userId, RealTimeNotification notification, CancellationToken cancellationToken = default);
}
