using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.DTOs;

// Teklif detayı: durum, prim ve prim dökümü (risk faktörleri + teminat paketi) ile birlikte.
// Prim dökümü ve risk skoru, saklanan seçim + veriden deterministik olarak yeniden hesaplanır (ADR-021).
public sealed record QuoteDto(
    Guid Id,
    Guid CustomerId,
    InsuranceBranch Branch,
    string ProductName,
    QuoteStatus Status,
    CoveragePackage CoveragePackage,
    RiskScore RiskScore,
    decimal BasePremium,
    decimal TotalPremium,
    DateTime? ValidUntil,
    DateTime CreatedAt,
    QuoteRiskObjectDto RiskObject,
    IReadOnlyList<QuoteCoverageDto> Coverages,
    IReadOnlyList<PricingBreakdownItem> PremiumBreakdown);
