using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Notifications;

// Yenileme teklifi üretildiğinde müşteriye gönderilecek (mock) bildirim içeriği. Hassas veri taşımaz.
public sealed record RenewalOfferedNotification(
    Guid CustomerId,
    string PolicyNumber,
    InsuranceBranch Branch,
    decimal OfferedPremium,
    DateTime ValidUntil);
