using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Renewals.DTOs;

// Müşteriye sunulan yenileme teklifi görünümü: yenilenen poliçe + yeni dönem teklifi bilgileri.
public sealed record RenewalDto(
    Guid Id,
    Guid PolicyId,
    string PolicyNumber,
    Guid NewQuoteId,
    InsuranceBranch Branch,
    QuoteStatus NewQuoteStatus,
    decimal OfferedPremium,
    DateTime? ValidUntil,
    DateTime OfferedAt,
    bool IsAccepted,
    DateTime? AcceptedAt);
