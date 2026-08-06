using NSubstitute;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Tests.Common;

// Fiyatlandırma tarifesi çözümleyicisi için ortak test sahteleri.
// Varsayılan davranış "baseline" tarifedir → tarife yönetimi eklenmeden önceki fiyatlar birebir korunur,
// böylece mevcut fiyatlama testleri beklenen değerlerini değiştirmeden geçmeye devam eder.
public static class PricingTestDoubles
{
    /// <summary>Yerleşik baseline tarifeyi döndüren çözümleyici (versiyon yok → motor sabit tabloyu kullanır).</summary>
    public static IPricingRateResolver BaselineResolver()
    {
        var resolver = Substitute.For<IPricingRateResolver>();
        resolver.ResolveEffectiveAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(EffectivePricing.Baseline);
        resolver.ResolveForQuoteAsync(Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns((PricingRateSet?)null);
        return resolver;
    }

    /// <summary>
    /// GERÇEK girdi kurucusu (sahte değil) — önizleme ile teklif oluşturmanın aynı girdiyi
    /// üretmesi testlerde de fiilen çalışır. Deprem bölgesi sağlayıcısı istenirse özelleştirilebilir.
    /// </summary>
    /// <param name="policyRepository">Verilmezse sahte döner → 0 hasarsız dönem (nötr basamak).</param>
    /// <param name="claimRepository">Verilmezse sahte döner → 0 hasar (nötr basamak).</param>
    public static IQuotePricingInputBuilder InputBuilder(
        IEarthquakeZoneProvider? zoneProvider = null,
        IPolicyRepository? policyRepository = null,
        IClaimRepository? claimRepository = null) =>
        new QuotePricingInputBuilder(
            zoneProvider ?? Substitute.For<IEarthquakeZoneProvider>(),
            policyRepository ?? Substitute.For<IPolicyRepository>(),
            claimRepository ?? Substitute.For<IClaimRepository>());

    /// <summary>Belirli bir branşta özel baz prim uygulayan tarife (fiyat değişikliği senaryoları için).</summary>
    public static PricingRateSet RateSet(InsuranceBranch branch, decimal basePremium) =>
        new(new Dictionary<InsuranceBranch, decimal> { [branch] = basePremium });
}
