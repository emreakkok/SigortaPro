using SigortaPro.Application.Common.Notifications;

namespace SigortaPro.Application.Common.Interfaces;

// Mock bildirim servisi. Arayüz Application'da, implementasyon Infrastructure'da
// (log/e-posta simülasyonu). MVP'de gerçek e-posta/SMS entegrasyonu yoktur.
public interface INotificationService
{
    Task NotifyRenewalOfferedAsync(RenewalOfferedNotification notification, CancellationToken cancellationToken = default);
}
