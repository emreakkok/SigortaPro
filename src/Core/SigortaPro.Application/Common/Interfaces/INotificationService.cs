using SigortaPro.Application.Common.Notifications;

namespace SigortaPro.Application.Common.Interfaces;

// Mock bildirim servisi (ARCHITECTURE_RULES.md §6.1). Arayüz Application'da, implementasyon Infrastructure'da
// (log/e-posta simülasyonu). MVP'de gerçek e-posta/SMS entegrasyonu yoktur (PROJECT_CONTEXT §9).
public interface INotificationService
{
    Task NotifyRenewalOfferedAsync(RenewalOfferedNotification notification, CancellationToken cancellationToken = default);
}
