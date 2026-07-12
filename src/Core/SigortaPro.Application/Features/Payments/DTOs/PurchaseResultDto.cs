using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Payments.DTOs;

// Teklif satın alma sonucu: ödeme özeti + oluşturulan aktif poliçe. Kart yalnızca maskeli döner.
public sealed record PurchaseResultDto(
    Guid PaymentId,
    PaymentStatus PaymentStatus,
    string MaskedCardNumber,
    decimal Amount,
    int InstallmentCount,
    string? ProviderReferenceCode,
    PolicySummaryDto Policy);
