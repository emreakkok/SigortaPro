using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.DTOs;

// Teklif listesi görünümü için özet DTO (prim dökümü içermez).
public sealed record QuoteSummaryDto(
    Guid Id,
    InsuranceBranch Branch,
    string ProductName,
    QuoteStatus Status,
    CoveragePackage CoveragePackage,
    decimal TotalPremium,
    DateTime? ValidUntil,
    DateTime CreatedAt);
