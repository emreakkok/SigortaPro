using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Dashboard.DTOs;

// ADR-052: Operasyon dashboard'ının tek veri kaynağı. Tüm bloklar SEÇİLEN TARİH ARALIĞINA göre hesaplanır;
// karşılaştırma, hemen öncesindeki EŞİT UZUNLUKTAKİ dönemle yapılır. Oranlar 0..1 ondalıktır (frontend biçimler).
// Bir oran güvenilir hesaplanamıyorsa (payda 0) **null** döner — "%0" veya "+%100" gibi yanıltıcı değer üretilmez.
//
// Finansal görünürlük ayrımı (P1 kararı D1 — agregat finans yalnızca Admin): agregat finansal alanlar
// (prim üretimi, ciro/portföy primi, kârlılık, ödenen/tahmini hasar tutarı, branş prim toplamı, başarısız
// ödeme) nullable'dır ve Personel çağrılarında handler tarafından **null** maskelenir → veri backend
// response'unda gönderilmez (yalnızca frontend gizleme yeterli değildir). Operasyonel alanlar (adetler,
// huni, hasar durum kırılımı, yenileme oranı, portföy adetleri) her iki rolde de doludur.
public sealed record DashboardSummaryDto(
    // Seçilen aralık ve serinin kova genişliği (frontend eksen biçimlendirmesi için).
    DateTime From,
    DateTime To,
    PremiumGranularityDto Granularity,
    DashboardPeriodStatsDto Current,
    DashboardPeriodStatsDto Previous,
    DashboardDeltaDto Deltas,
    OperationalAlertsDto Alerts,
    PortfolioDto Portfolio,
    QuoteFunnelDto Funnel,
    IReadOnlyList<PremiumSeriesPointDto> PremiumSeries,
    IReadOnlyList<BranchPerformanceDto> BranchPerformance,
    ClaimOperationDto Claims,
    // Dönemsel yenileme oranı: aralıkta SUNULAN yenilemelerin kabul oranı. Sunulan yoksa null.
    decimal? RenewalRate);

// Serinin kova genişliği (JSON'da sayısal enum — mevcut konvansiyon).
public enum PremiumGranularityDto
{
    Hourly,
    Daily,
    Monthly
}

// Bir dönemin operasyon sayaçları. PremiumProduction = dönemde ÜRETİLEN poliçelerin brüt primi (FİNANSAL —
// Personel'e null maskelenir); sayaçlar operasyoneldir ve her rolde doludur.
public sealed record DashboardPeriodStatsDto(
    int NewCustomers,
    int NewQuotes,
    int NewPolicies,
    int NewClaims,
    decimal? PremiumProduction);

// Önceki eşit uzunluktaki döneme göre oransal değişim (0.18 = +%18). Önceki dönem 0 ise **null**
// (tanımsız) — sıfırdan artışı "+%100" gibi göstermek yanıltıcı olurdu.
public sealed record DashboardDeltaDto(
    decimal? PremiumProduction,
    decimal? NewPolicies,
    decimal? NewQuotes,
    decimal? NewCustomers);

// Aksiyon merkezi: admin'in dokunması gereken açık işler. Her satır tıklanabilir bir hedefe karşılık gelir.
public sealed record OperationalAlertsDto(
    // Fiyatlanmış/onaylanmış, henüz satın alınmamış teklifler (dönemden bağımsız — açık iş yükü).
    int PendingQuotes,
    // Bildirilmiş/incelemedeki hasarlar (açık iş yükü).
    int PendingClaims,
    // Önümüzdeki N gün içinde bitecek aktif poliçeler (yenileme fırsatı).
    int UpcomingRenewals,
    int UpcomingRenewalWindowDays,
    // Seçilen aralıkta başarısız olan ödemeler (tahsilat sorunu — FİNANSAL; Personel'e null maskelenir).
    int? FailedPayments);

// Portföyün anlık (dönemden bağımsız) durumu. LossRatio kümülatiftir — sigortacılıkta anlamlı olan budur.
public sealed record PortfolioDto(
    int ActivePolicyCount,
    int TotalCustomerCount,
    // FİNANSAL (Personel'e null maskelenir): portföy prim toplamı, ödenen hasar tutarı, kârlılık oranı.
    decimal? LifetimePremiumProduction,
    decimal? PaidClaimAmount,
    // Ödenen hasar / üretilen prim. Prim 0 ise null (ayrıca Personel'e maskelenir).
    decimal? LossRatio);

// Satış hunisi: aralıkta OLUŞTURULAN tekliflerin kohortu. Onaylanan = onaylanmış + satın alınmış
// (satın alma onaydan geçer), böylece huni monoton azalır. ConversionRate = satın alınan / oluşturulan.
public sealed record QuoteFunnelDto(
    int Created,
    int Approved,
    int Purchased,
    int Expired,
    int Rejected,
    decimal? ConversionRate);

public sealed record PremiumSeriesPointDto(
    DateTime BucketStart,
    int PolicyCount,
    // FİNANSAL (Personel'e null maskelenir); poliçe adedi operasyoneldir ve her rolde doludur.
    decimal? PremiumTotal);

// Branş performansı — teklif kohortu üzerinden (tek kaynak): oluşturulan teklif, poliçeleşen, prim, dönüşüm.
public sealed record BranchPerformanceDto(
    InsuranceBranch Branch,
    int QuoteCount,
    int PurchasedCount,
    // FİNANSAL (Personel'e null maskelenir); adet ve dönüşüm operasyoneldir ve her rolde doludur.
    decimal? PremiumTotal,
    decimal? ConversionRate);

// Hasar operasyonu: aralıkta bildirilen hasarların durum kırılımı + tutarlar.
public sealed record ClaimOperationDto(
    int Submitted,
    int UnderReview,
    int Approved,
    int Rejected,
    int Paid,
    // Ödenmiş hasarların onay tutarı (güvenilir: yalnızca Paid kayıtlar) — FİNANSAL; Personel'e null maskelenir.
    decimal? PaidAmount,
    // Bildirilen hasarların tahmini tutar toplamı (beyan) — FİNANSAL; Personel'e null maskelenir.
    decimal? EstimatedAmount);
