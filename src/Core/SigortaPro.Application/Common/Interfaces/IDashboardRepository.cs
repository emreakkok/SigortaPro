using SigortaPro.Application.Common.Models;
using SigortaPro.Application.Features.Dashboard;
using SigortaPro.Application.Features.Dashboard.ReadModels;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Common.Interfaces;

// Admin dashboard & raporlama için özel salt okunur repository. Tüm metotlar
// birden çok aggregate üzerinde SQL tarafı projeksiyon/agregasyon (COUNT/SUM/GROUP BY) yapar; hiçbiri durum
// değiştirmez ve hiçbiri tabloyu belleğe çekmez (N+1 yok). Türetilmiş oranlar (dönüşüm, yenileme, hasar/prim)
// handler'da hesaplanır (Application) — payda 0 ise null döner, uydurma oran üretilmez.
public interface IDashboardRepository
{
    // --- Portföy (dönemden bağımsız anlık durum) ---

    // Üretilen toplam prim (tüm poliçelerin primi toplamı) — kümülatif hasar/prim oranı için.
    Task<decimal> GetTotalPremiumProductionAsync(CancellationToken cancellationToken = default);

    // Aktif poliçe sayısı.
    Task<int> GetActivePolicyCountAsync(CancellationToken cancellationToken = default);

    // Toplam müşteri sayısı (sistemde "aktif/pasif müşteri" ayrımı YOKTUR — uydurulmaz).
    Task<int> GetTotalCustomerCountAsync(CancellationToken cancellationToken = default);

    // Ödenen hasarların onay tutarı toplamı (hasar/prim oranı için).
    Task<decimal> GetTotalPaidClaimAmountAsync(CancellationToken cancellationToken = default);

    // --- Aksiyon merkezi (açık iş yükü) ---

    // Bekleyen teklif sayısı (Priced veya Approved — henüz satın alınmamış/reddedilmemiş/süresi dolmamış).
    Task<int> GetPendingQuoteCountAsync(CancellationToken cancellationToken = default);

    // Bekleyen hasar sayısı (Submitted veya UnderReview).
    Task<int> GetPendingClaimCountAsync(CancellationToken cancellationToken = default);

    // Verilen an itibarıyla önümüzdeki N gün içinde bitecek AKTİF poliçe sayısı (yenileme fırsatı).
    Task<int> GetUpcomingRenewalCountAsync(
        DateTime asOf, int withinDays, CancellationToken cancellationToken = default);

    // Aralıkta başarısız olan ödeme sayısı (tahsilat sorunu — Payment.TransactionDate'e göre).
    Task<int> GetFailedPaymentCountAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default);

    // --- Dönemsel analiz (seçilen tarih aralığı) ---

    // Aralığın operasyon sayaçları: yeni müşteri/teklif/poliçe/hasar + üretilen prim (Policy.CreatedAt).
    Task<PeriodStatsAggregate> GetPeriodStatsAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default);

    // Aralıkta oluşturulan tekliflerin güncel durum kırılımı (satış hunisi + dönüşüm oranı kaynağı).
    Task<QuoteFunnelAggregate> GetQuoteFunnelAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default);

    // Aralıkta oluşturulan tekliflerin branş bazlı performansı (teklif / poliçeleşen / prim) — tek kohort.
    Task<IReadOnlyList<BranchPerformanceAggregate>> GetBranchPerformanceAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default);

    // Aralıkta bildirilen hasarların durum kırılımı (adet + tahmini/onaylanan tutar).
    Task<IReadOnlyList<ClaimStatusCountAggregate>> GetClaimStatusBreakdownAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default);

    // Prim üretimi zaman serisi (Policy.CreatedAt); kova genişliği çağıran tarafından verilir.
    Task<IReadOnlyList<PremiumSeriesAggregate>> GetPremiumSeriesAsync(
        DateTime fromInclusive,
        DateTime toInclusive,
        PremiumGranularity granularity,
        CancellationToken cancellationToken = default);

    // Aralıkta SUNULAN yenileme sayısı ve bunların kaç tanesinin kabul edildiği (dönemsel yenileme oranı).
    Task<(int Offered, int Accepted)> GetRenewalCountsAsync(
        DateTime fromInclusive, DateTime toInclusive, CancellationToken cancellationToken = default);

    // --- Raporlar ---

    // Tarih aralıklı poliçe raporu (başlangıç tarihine göre); müşteri ve teklif ile birlikte, sayfalanmış.
    // search: müşteri adı/soyadı/tam adı, telefon (format bağımsız) veya poliçe numarası.
    Task<PagedResult<Policy>> GetPoliciesByDateRangeAsync(
        DateTime fromInclusive, DateTime toInclusive, string? search, PaginationParams paging, CancellationToken cancellationToken = default);

    // Tarih aralıklı ödeme raporu (işlem tarihine göre); müşteri ile birlikte, sayfalanmış.
    Task<PagedResult<Payment>> GetPaymentsByDateRangeAsync(
        DateTime fromInclusive, DateTime toInclusive, PaginationParams paging, CancellationToken cancellationToken = default);

    // En riskli müşteri segmentleri: hasar sayısı (ardından hasar tutarı) azalan; ilk N kayıt.
    Task<IReadOnlyList<CustomerRiskAggregate>> GetRiskiestCustomerSegmentsAsync(
        int topCount, CancellationToken cancellationToken = default);
}
