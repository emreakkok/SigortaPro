using SigortaPro.Application.Features.Policies.DTOs;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Policies;

// Entity → DTO manuel eşlemeleri (AutoMapper kullanılmaz — CODING_STANDARDS.md §4.2).
// Branş/ürün/risk objesi/teminatlar poliçenin kaynaklandığı tekliften türetilir; teklif detayının
// gösterim yardımcıları (QuoteMappings.BuildRiskObject) yeniden kullanılır (DRY).
internal static class PolicyMappings
{
    public static PolicyListItemDto ToListItem(Policy policy) => new(
        policy.Id,
        policy.PolicyNumber,
        policy.Quote!.Branch,
        policy.Quote!.InsuranceProduct?.Name ?? string.Empty,
        policy.Status,
        policy.StartDate,
        policy.EndDate,
        policy.TotalPremium);

    public static PolicyDetailDto ToDetailDto(Policy policy, QuotePricingOutcome pricing) => new(
        policy.Id,
        policy.PolicyNumber,
        policy.Quote!.Branch,
        policy.Quote!.InsuranceProduct!.Name,
        policy.Status,
        policy.Quote!.CoveragePackage,
        policy.StartDate,
        policy.EndDate,
        policy.TotalPremium,
        policy.QuoteId,
        QuoteMappings.BuildRiskObject(policy.Quote!.Vehicle, policy.Quote!.Property),
        pricing.Coverages);
}
