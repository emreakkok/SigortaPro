namespace SigortaPro.Application.Features.Dashboard.ReadModels;

// SQL tarafında (GROUP BY yıl/ay) hesaplanan aylık satış toplamı. API DTO'su değil; yalnızca
// IDashboardRepository'nin döndürdüğü salt okunur sorgu sonucudur (handler bunu DTO'ya eşler — ADR-026).
public sealed record MonthlySalesAggregate(
    int Year,
    int Month,
    int PolicyCount,
    decimal PremiumTotal);
