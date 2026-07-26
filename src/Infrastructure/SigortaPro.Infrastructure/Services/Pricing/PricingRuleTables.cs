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

    // Kullanım amacı katsayıları (ADR-057). Hususi referans seviyedir (1.00); ticari ve taksi kullanım
    // daha yüksek yıllık kilometre/kaza sıklığı taşıdığından ek prim uygular.
    // NOT: Bu değerler MVP SİMÜLASYONUDUR — gerçek aktüeryal tarife verisi değildir (PRICING.md).
    public static readonly IReadOnlyDictionary<VehicleUsage, decimal> VehicleUsageCoefficients =
        new Dictionary<VehicleUsage, decimal>
        {
            [VehicleUsage.Hususi] = 1.00m,
            [VehicleUsage.Ticari] = 1.30m,
            [VehicleUsage.Taksi] = 1.60m,
        };

    // ADR-059: Bonus-Malus basamak çarpanları (−3 … +6). Negatif basamak MALUS (ek prim), pozitif
    // basamak BONUS (indirim), 0 nötrdür (yeni müşteri / geçmişi bilinmeyen).
    // Malus tavanı (1.60), emekliye ayrılan ClaimHistoryFactor'ın tavanıyla BİREBİR aynıdır → ekonomik etki kontrollü.
    // NOT: Değerler MVP SİMÜLASYONUDUR — gerçek aktüeryal tarife verisi değildir (PRICING.md).
    public static readonly IReadOnlyDictionary<int, decimal> BonusMalusCoefficients =
        new Dictionary<int, decimal>
        {
            [-3] = 1.60m,
            [-2] = 1.40m,
            [-1] = 1.20m,
            [0] = 1.00m,
            [1] = 0.95m,
            [2] = 0.90m,
            [3] = 0.85m,
            [4] = 0.80m,
            [5] = 0.75m,
            [6] = 0.70m,
        };

    // Risk skoru eşikleri: toplam çarpan (Total/Base) bu eşiklere göre sınıflandırılır.
    public const decimal MediumRiskThreshold = 1.10m;
    public const decimal HighRiskThreshold = 1.50m;
}
