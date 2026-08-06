using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Interfaces;

// Sigorta ürünü sorguları. Teklif oluşturma/karşılaştırma, branşa göre
// aktif ürünü teminatlarıyla birlikte çözümler.
public interface IInsuranceProductRepository : IReadRepository<InsuranceProduct>
{
    // Verilen branştaki aktif ürünü teminatlarıyla birlikte getirir; yoksa null.
    Task<InsuranceProduct?> GetActiveByBranchAsync(InsuranceBranch branch, CancellationToken cancellationToken = default);
}
