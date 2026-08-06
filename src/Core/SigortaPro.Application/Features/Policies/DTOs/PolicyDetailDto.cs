using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Policies.DTOs;

// Poliçe detayı: poliçe künyesi + risk objesi + teminat tablosu. Teminatlar, poliçenin kaynaklandığı
// teklifin saklanan seçiminden (CoveragePackage) deterministik yeniden hesaplanır.
// Risk objesi ve teminat DTO'ları teklif detayıyla aynı şekildedir (aynı gösterim; DRY).
public sealed record PolicyDetailDto(
    Guid Id,
    string PolicyNumber,
    InsuranceBranch Branch,
    string ProductName,
    PolicyStatus Status,
    CoveragePackage CoveragePackage,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalPremium,
    Guid QuoteId,
    QuoteRiskObjectDto RiskObject,
    IReadOnlyList<QuoteCoverageDto> Coverages,
    // Müşteri (Sigorta Ettiren) kimliği (additive) — admin detayında ad + telefon özeti; CustomerId stabil kimlik.
    Guid CustomerId = default,
    string CustomerFullName = "",
    string? CustomerPhone = null,
    // (additive): Sağlıkta "başkası adına" poliçede Sigortalı özeti; Ettiren = müşteri. Değilse null.
    QuoteInsuredPersonDto? InsuredPerson = null);
