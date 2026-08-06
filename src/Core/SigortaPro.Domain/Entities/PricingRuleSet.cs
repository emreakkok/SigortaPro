using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

// Bir tarife versiyonunun BAZ PRİM DIŞINDAKİ tüm çarpanları:
// ticari kaldıraçlar (paket çarpanları, il risk katsayıları, yenileme indirimi) + aktüeryal faktör grupları
// (sürücü/araç/konut/sağlık). Versiyonla birlikte DEĞİŞMEZDİR (yalnızca taslak düzenlenirken kurulur); teklif
// bu versiyonu sabitlediğinden (PricingVersionId + PricingSnapshot) geçmiş primler asla değişmez.
//
// DEĞER NESNESİDİR (owned) — EF'te tek bir JSON kolonuna serileştirilir (ayrı tablo/entity açılmaz).
//
// Bantlı faktörler SIRALI çarpan listeleridir; band sınırları/etiketleri MOTORDA sabittir (rating yapısı),
// yalnızca çarpan DEĞERLERİ versiyonlanır. Her liste NULLABLE'dır: null ise (bu faktör grubu eklenmeden önce
// oluşmuş versiyonlar) motor yerleşik baseline değerini kullanır → eski teklifler bit-aynı yeniden hesaplanır.
public sealed record PricingRuleSet(
    // ── Ticari kaldıraçlar ──
    IReadOnlyDictionary<CoveragePackage, decimal> PackagePremiumFactors,
    IReadOnlyDictionary<string, decimal> CityRiskCoefficients,
    decimal DefaultCityRiskCoefficient,
    decimal RenewalDiscountFactor,
    // ── Sürücü faktörleri ── [0]=<25, [1]=25–65, [2]=>65
    IReadOnlyList<decimal>? DriverAgeFactors = null,
    // ── Araç faktörleri ──
    IReadOnlyList<decimal>? VehicleAgeFactors = null,      // [0]=0–3, [1]=4–10, [2]=>10
    IReadOnlyList<decimal>? EnginePowerFactors = null,     // [0]=≤100, [1]=≤160, [2]=≤240, [3]=>240
    IReadOnlyList<decimal>? VehicleUsageFactors = null,    // index = (int)VehicleUsage
    IReadOnlyList<decimal>? BonusMalusFactors = null,      // index = step − BonusMalus MinStep
    // ── Konut faktörleri ──
    IReadOnlyList<decimal>? BuildingAgeFactors = null,     // [0]=≤5, [1]=≤20, [2]=≤40, [3]=>40
    IReadOnlyList<decimal>? SquareMetersFactors = null,    // [0]=≤75, [1]=≤120, [2]=≤200, [3]=>200
    IReadOnlyList<decimal>? EarthquakeZoneFactors = null,  // [0..4]=zone1..5, [5]=bilinmeyen
    // ── Sağlık faktörleri ──
    IReadOnlyList<decimal>? HealthAgeFactors = null,       // [0]=≤17, [1]=≤30, [2]=≤45, [3]=≤60, [4]=>60
    decimal? SmokerSurcharge = null)
{
    // Paket prim çarpanı; tanımlı değilse null (çağıran yerleşik baseline'a düşer).
    public decimal? PackagePremiumFactorFor(CoveragePackage package) =>
        PackagePremiumFactors.TryGetValue(package, out var factor) ? factor : null;

    // İl risk katsayısı; il tabloda yoksa bu versiyonun varsayılan katsayısı döner.
    public decimal CityCoefficientFor(string? city)
    {
        if (!string.IsNullOrWhiteSpace(city))
        {
            foreach (var entry in CityRiskCoefficients)
            {
                if (string.Equals(entry.Key, city.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Value;
                }
            }
        }

        return DefaultCityRiskCoefficient;
    }

    // Bantlı bir faktörün belirli band çarpanını döner; bu versiyonda tanımlı değilse (null/eksik indeks) null →
    // çağıran (motor) yerleşik baseline değerini kullanır.
    public static decimal? BandFactor(IReadOnlyList<decimal>? factors, int index) =>
        factors is not null && index >= 0 && index < factors.Count ? factors[index] : null;
}
