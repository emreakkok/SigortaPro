using SigortaPro.Domain.Enums;

namespace SigortaPro.Infrastructure.Services.Pricing;

// Fiyatlama kural sabitleri ve tabloları (ADR-008). Buradaki tüm değerler PRICING.md ile birebir
// eşleşir; kural değiştiğinde iki dosya birlikte güncellenir.
internal static class PricingRuleTables
{
    // Branş bazlı yıllık baz primler (TRY).
    public static readonly IReadOnlyDictionary<InsuranceBranch, decimal> BasePremiums =
        new Dictionary<InsuranceBranch, decimal>
        {
            [InsuranceBranch.Kasko] = 15000m,
            [InsuranceBranch.Trafik] = 6000m,
            [InsuranceBranch.Konut] = 3000m,
            [InsuranceBranch.Dask] = 1500m,
            [InsuranceBranch.Saglik] = 8000m,
        };

    // İl bazlı risk katsayıları; listede olmayan iller için varsayılan katsayı uygulanır.
    // Eşleşme kültür bağımsız ve büyük/küçük harf duyarsızdır (mock — serbest metin il alanı).
    public static readonly IReadOnlyDictionary<string, decimal> CityRiskCoefficients =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["İstanbul"] = 1.25m,
            ["İzmir"] = 1.20m,
            ["Ankara"] = 1.15m,
            ["Bursa"] = 1.10m,
            ["Antalya"] = 1.10m,
        };

    public const decimal DefaultCityRiskCoefficient = 1.00m;

    // Hasarsızlık basamağı: her basamak %5 indirim; en fazla 7 basamak (taban çarpan 0.65).
    public const decimal NoClaimDiscountPerTier = 0.05m;
    public const int MaxNoClaimTier = 7;

    // Risk skoru eşikleri: toplam çarpan (Total/Base) bu eşiklere göre sınıflandırılır.
    public const decimal MediumRiskThreshold = 1.10m;
    public const decimal HighRiskThreshold = 1.50m;
}
