using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Entities;

namespace SigortaPro.Application.Features.Pricing;

// ADR-048: IPricingRateResolver implementasyonu (Application servisi — iş kuralı içermez, yalnızca
// "hangi tarife" sorusunu cevaplar). Versiyon bulunamazsa baseline'a düşer; böylece tarife tablosu
// boş olsa dahi (ör. temiz kurulum) fiyatlama çalışmaya devam eder.
public sealed class PricingRateResolver : IPricingRateResolver
{
    private readonly IPricingVersionRepository _pricingVersionRepository;

    public PricingRateResolver(IPricingVersionRepository pricingVersionRepository)
    {
        _pricingVersionRepository = pricingVersionRepository;
    }

    public async Task<EffectivePricing> ResolveEffectiveAsync(
        DateTime asOf, CancellationToken cancellationToken = default)
    {
        // Yürürlükteki tarife artık ZAMAN yerine YAŞAM DÖNGÜSÜ ile belirlenir: tek AKTİF versiyon. Böylece
        // admin "Aktifleştir" dediği anda yeni teklifler yeni tarifeyi kullanır (asOf yalnızca imzada kalır).
        var version = await _pricingVersionRepository.GetActiveAsync(cancellationToken);
        return version is null ? EffectivePricing.Baseline : new EffectivePricing(version.Id, ToRateSet(version));
    }

    public async Task<PricingRateSet?> ResolveForQuoteAsync(
        Guid? pricingVersionId, CancellationToken cancellationToken = default)
    {
        if (pricingVersionId is null)
        {
            // Tarife yönetimi öncesi oluşturulmuş teklif → yerleşik baseline (bit-aynı sonuç).
            return null;
        }

        var version = await _pricingVersionRepository.GetWithRatesByIdAsync(pricingVersionId.Value, cancellationToken);
        return version is null ? null : ToRateSet(version);
    }

    public static PricingRateSet ToRateSet(PricingVersion version) =>
        new(
            version.Rates.ToDictionary(rate => rate.Branch, rate => rate.BasePremium),
            version.RuleSet);
}
