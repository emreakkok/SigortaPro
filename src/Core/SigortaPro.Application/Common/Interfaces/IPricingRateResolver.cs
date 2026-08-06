using SigortaPro.Application.Common.Pricing;

namespace SigortaPro.Application.Common.Interfaces;

// Fiyatlamada kullanılacak tarifeyi çözen servis. İki farklı soru vardır ve karıştırılmamalıdır:
//  1) YENİ fiyatlama (teklif oluşturma / karşılaştırma önizlemesi) → o an YÜRÜRLÜKTEKİ tarife,
//  2) MEVCUT teklifin yeniden hesabı (detay, PDF, poliçe görünümü) → teklifin SABİTLEDİĞİ tarife.
// (2) sayesinde admin tarifeyi değiştirse bile geçmiş teklif/poliçe primleri değişmez.
public interface IPricingRateResolver
{
    // Yürürlükteki tarife: yeni fiyatlamalarda kullanılır. Versiyon yoksa (VersionId: null, Rates: null)
    // döner → motor yerleşik baseline'ı kullanır.
    Task<EffectivePricing> ResolveEffectiveAsync(DateTime asOf, CancellationToken cancellationToken = default);

    // Teklifin sabitlediği tarife. pricingVersionId null ise (tarife yönetimi öncesi kayıtlar) null döner
    // → yerleşik baseline ile birebir aynı sonuç üretilir.
    Task<PricingRateSet?> ResolveForQuoteAsync(Guid? pricingVersionId, CancellationToken cancellationToken = default);
}

// Yürürlükteki tarifenin kimliği + oranları. VersionId teklifte sabitlenir.
public sealed record EffectivePricing(Guid? VersionId, PricingRateSet? Rates)
{
    public static readonly EffectivePricing Baseline = new(null, null);
}
