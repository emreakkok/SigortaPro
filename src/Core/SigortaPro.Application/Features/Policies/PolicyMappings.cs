using SigortaPro.Application.Features.Policies.DTOs;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Policies;

// Entity → DTO manuel eşlemeleri (AutoMapper kullanılmaz).
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
        // düzeltmesi: Sağlıkta "başkası adına" poliçede risk objesi/Sigortalı bilgisi teklifin
        // InsuredPerson'ından gelir — poliçe detayı da (teklif detayı gibi) doğru Sigortalı'yı gösterir.
        QuoteMappings.BuildRiskObject(policy.Quote!.Vehicle, policy.Quote!.Property, policy.Quote!.InsuredPerson),
        pricing.Coverages,
        // Additive: Sigorta Ettiren (müşteri) kimliği — ad + telefon; CustomerId stabil kimlik. Customer poliçe
        // detayında zaten yüklüdür (fiyat yeniden hesabı için). Sağlıkta Sigortalı ayrı olarak InsuredPerson'da.
        policy.CustomerId,
        QuoteMappings.FullName(policy.Customer),
        policy.Customer?.PhoneNumber,
        QuoteMappings.ToInsuredPersonDto(policy.Quote!.InsuredPerson));
}
