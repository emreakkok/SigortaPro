using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.DTOs;

// Aynı risk objesi için üretilen teminat seviyeli paket alternatifleri (2-3 adet).
public sealed record QuoteComparisonDto(
    InsuranceBranch Branch,
    string ProductName,
    QuoteRiskObjectDto RiskObject,
    IReadOnlyList<QuotePackageDto> Packages);
