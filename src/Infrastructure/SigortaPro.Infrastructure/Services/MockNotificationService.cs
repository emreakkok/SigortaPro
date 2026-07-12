using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Notifications;

namespace SigortaPro.Infrastructure.Services;

// Mock bildirim servisi (ADR-007 deseninde): gerçek e-posta/SMS yerine yapılandırılmış log yazar (simülasyon).
// MVP'de gerçek entegrasyon yoktur (PROJECT_CONTEXT §9); arayüz ileride gerçek sağlayıcıya geçişe hazırdır.
public sealed class MockNotificationService : INotificationService
{
    private readonly ILogger<MockNotificationService> _logger;

    public MockNotificationService(ILogger<MockNotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyRenewalOfferedAsync(RenewalOfferedNotification notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[BİLDİRİM/mock] Yenileme teklifi hazır. Müşteri: {CustomerId}, Poliçe: {PolicyNumber}, Branş: {Branch}, " +
            "Prim: {OfferedPremium}, Son geçerlilik: {ValidUntil}",
            notification.CustomerId, notification.PolicyNumber, notification.Branch,
            notification.OfferedPremium, notification.ValidUntil);

        return Task.CompletedTask;
    }
}
