using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Dashboard.ReadModels;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Interfaces;

// Admin dashboard & raporlama için özel salt okunur repository (ADR-005 §4.2, ADR-026). Tüm metotlar
// birden çok aggregate üzerinde SQL tarafı projeksiyon/agregasyon (COUNT/SUM/GROUP BY) yapar; hiçbiri durum
// değiştirmez. Türetilmiş oranlar (yenileme oranı, hasar/prim oranı) handler'da hesaplanır (Application).
public interface IDashboardRepository
{
    // Üretilen toplam prim (tüm poliçelerin primi toplamı).
    Task<decimal> GetTotalPremiumProductionAsync(CancellationToken cancellationToken = default);

    // Aktif poliçe sayısı.
    Task<int> GetActivePolicyCountAsync(CancellationToken cancellationToken = default);

    // Bekleyen teklif sayısı (Priced veya Approved — henüz satın alınmamış/reddedilmemiş/süresi dolmamış).
    Task<int> GetPendingQuoteCountAsync(CancellationToken cancellationToken = default);

    // Bekleyen hasar sayısı (Submitted veya UnderReview).
    Task<int> GetPendingClaimCountAsync(CancellationToken cancellationToken = default);

    // Ödenen hasarların onay tutarı toplamı (hasar/prim oranı için).
    Task<decimal> GetTotalPaidClaimAmountAsync(CancellationToken cancellationToken = default);

    // Sunulan toplam yenileme teklifi sayısı (yenileme oranı paydası).
    Task<int> GetRenewalOfferedCountAsync(CancellationToken cancellationToken = default);

    // Onaylanan yenileme teklifi sayısı (yenileme oranı payı).
    Task<int> GetAcceptedRenewalCountAsync(CancellationToken cancellationToken = default);

    // Aylık satış trendi: verilen tarihten itibaren poliçeler oluşturulma yıl/ayına göre gruplanır.
    Task<IReadOnlyList<MonthlySalesAggregate>> GetMonthlySalesTrendAsync(
        DateTime fromInclusive, CancellationToken cancellationToken = default);

    // Branş bazlı dağılım: poliçeler teklif branşına göre gruplanır.
    Task<IReadOnlyList<BranchDistributionAggregate>> GetBranchDistributionAsync(
        CancellationToken cancellationToken = default);

    // Tarih aralıklı poliçe raporu (başlangıç tarihine göre); müşteri ve teklif ile birlikte, sayfalanmış.
    Task<PagedResult<Policy>> GetPoliciesByDateRangeAsync(
        DateTime fromInclusive, DateTime toInclusive, PaginationParams paging, CancellationToken cancellationToken = default);

    // Tarih aralıklı ödeme raporu (işlem tarihine göre); müşteri ile birlikte, sayfalanmış.
    Task<PagedResult<Payment>> GetPaymentsByDateRangeAsync(
        DateTime fromInclusive, DateTime toInclusive, PaginationParams paging, CancellationToken cancellationToken = default);

    // En riskli müşteri segmentleri: hasar sayısı (ardından hasar tutarı) azalan; ilk N kayıt.
    Task<IReadOnlyList<CustomerRiskAggregate>> GetRiskiestCustomerSegmentsAsync(
        int topCount, CancellationToken cancellationToken = default);
}
