namespace SigortaPro.Application.Features.Dashboard.ReadModels;

// Bir tarih aralığındaki operasyon sayaçları (her biri SQL tarafında COUNT/SUM ile hesaplanır).
// PremiumProduction = o aralıkta ÜRETİLEN poliçelerin (Policy.CreatedAt) brüt prim toplamıdır.
// API DTO'su değil; IDashboardRepository'nin döndürdüğü salt okunur sorgu sonucudur.
public sealed record PeriodStatsAggregate(
    int NewCustomers,
    int NewQuotes,
    int NewPolicies,
    int NewClaims,
    decimal PremiumProduction);
