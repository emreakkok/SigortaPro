using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Policies.DTOs;

// Poliçe detayı: poliçe künyesi + risk objesi + teminat tablosu. Teminatlar, poliçenin kaynaklandığı
// teklifin saklanan seçiminden (CoveragePackage) deterministik yeniden hesaplanır (ADR-021 ile tutarlı).
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
    IReadOnlyList<QuoteCoverageDto> Coverages);
