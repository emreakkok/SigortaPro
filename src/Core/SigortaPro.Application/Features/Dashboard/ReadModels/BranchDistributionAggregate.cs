using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Dashboard.ReadModels;

// SQL tarafında (GROUP BY teklif branşı) hesaplanan branş bazlı poliçe/prim dağılımı. API DTO'su değil;
// IDashboardRepository'nin döndürdüğü salt okunur sorgu sonucudur (handler bunu DTO'ya eşler — ADR-026).
public sealed record BranchDistributionAggregate(
    InsuranceBranch Branch,
    int PolicyCount,
    decimal PremiumTotal);
