using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Pricing;

// ADR-048: Fiyatlama motoruna dışarıdan verilen tarife (branş → baz prim + ticari kaldıraç seti). Bir
// `PricingVersion`'ın salt okunur görünümüdür; motor saf/deterministik kalmaya devam eder (girdi ne ise çıktı odur).
// `null` geçildiğinde motor yerleşik baseline tarifeyi kullanır → bu alan eklenmeden önceki davranış birebir
// korunur (eski teklifler bit-aynı yeniden hesaplanır). RuleSet (paket/şehir/yenileme katsayıları) opsiyoneldir:
// null ise (kural seti eklenmeden önce oluşmuş versiyonlar veya baz-prim-only test tarifeleri) motor yerleşik
// baseline katsayılarını kullanır.
public sealed record PricingRateSet(
    IReadOnlyDictionary<InsuranceBranch, decimal> BasePremiums,
    PricingRuleSet? RuleSet = null)
{
    /// <summary>Branşın baz primi; tarifede tanımlı değilse null (çağıran baseline'a düşer).</summary>
    public decimal? BasePremiumFor(InsuranceBranch branch) =>
        BasePremiums.TryGetValue(branch, out var basePremium) ? basePremium : null;
}
