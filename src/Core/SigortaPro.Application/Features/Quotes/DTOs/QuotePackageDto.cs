using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.DTOs;

// Karşılaştırmadaki tek bir teminat paketi alternatifi (henüz oluşturulmamış, önizleme).
public sealed record QuotePackageDto(
    CoveragePackage CoveragePackage,
    RiskScore RiskScore,
    decimal TotalPremium,
    IReadOnlyList<QuoteCoverageDto> Coverages,
    IReadOnlyList<PricingBreakdownItem> PremiumBreakdown);
