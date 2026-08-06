using SigortaPro.Domain.Enums;

namespace SigortaPro.Infrastructure.Services.Pricing;

// Fiyatlama kural sabitleri ve tabloları. Buradaki tüm değerler ile birebir
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

    // Kullanım amacı katsayıları. Hususi referans seviyedir (1.00); ticari ve taksi kullanım
    // daha yüksek yıllık kilometre/kaza sıklığı taşıdığından ek prim uygular.
    // NOT: Bu değerler MVP SİMÜLASYONUDUR — gerçek aktüeryal tarife verisi değildir.
    public static readonly IReadOnlyDictionary<VehicleUsage, decimal> VehicleUsageCoefficients =
        new Dictionary<VehicleUsage, decimal>
        {
            [VehicleUsage.Hususi] = 1.00m,
            [VehicleUsage.Ticari] = 1.30m,
            [VehicleUsage.Taksi] = 1.60m,
        };

    // Bonus-Malus basamak çarpanları (−3 … +6). Negatif basamak MALUS (ek prim), pozitif
    // basamak BONUS (indirim), 0 nötrdür (yeni müşteri / geçmişi bilinmeyen).
    // Malus tavanı (1.60), emekliye ayrılan ClaimHistoryFactor'ın tavanıyla BİREBİR aynıdır → ekonomik etki kontrollü.
    // NOT: Değerler MVP SİMÜLASYONUDUR — gerçek aktüeryal tarife verisi değildir.
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

    // ── Bantlı faktör baseline'ları ─────────────────────────────────────────────
    // Her faktör SIRALI bir çarpan listesidir; band sınırları/etiketleri motorda SABİTTİR (rating yapısı),
    // yalnızca bu çarpan DEĞERLERİ versiyonlanır. Değerler eskiden motor içinde satır-içiydi; buraya taşındı
    // (değer birebir aynı → determinizm korunur). Tarifenin kural seti bu faktörü içermiyorsa motor bunları kullanır.

    // Sürücü yaşı: [0]=<25 (genç), [1]=25–65 (standart), [2]=>65 (ileri yaş).
    public static readonly IReadOnlyList<decimal> DriverAgeBaseline = new[] { 1.30m, 1.00m, 1.15m };

    // Araç yaşı: [0]=0–3 (yeni), [1]=4–10 (orta), [2]=>10 (eski).
    public static readonly IReadOnlyList<decimal> VehicleAgeBaseline = new[] { 1.15m, 1.00m, 0.85m };

    // Motor gücü: [0]=≤100, [1]=101–160, [2]=161–240, [3]=>240 HP.
    public static readonly IReadOnlyList<decimal> EnginePowerBaseline = new[] { 1.00m, 1.10m, 1.25m, 1.45m };

    // Kullanım amacı: index = (int)VehicleUsage → [0]=Hususi, [1]=Ticari, [2]=Taksi.
    public static readonly IReadOnlyList<decimal> VehicleUsageBaseline = new[] { 1.00m, 1.30m, 1.60m };

    // Bonus-Malus: index = step − MinStep → [0]=−3 basamak … [9]=+6 basamak.
    public static readonly IReadOnlyList<decimal> BonusMalusBaseline =
        new[] { 1.60m, 1.40m, 1.20m, 1.00m, 0.95m, 0.90m, 0.85m, 0.80m, 0.75m, 0.70m };

    // Bina yaşı: [0]=0–5, [1]=6–20, [2]=21–40, [3]=>40.
    public static readonly IReadOnlyList<decimal> BuildingAgeBaseline = new[] { 0.95m, 1.00m, 1.10m, 1.25m };

    // Metrekare: [0]=≤75, [1]=76–120, [2]=121–200, [3]=>200 m².
    public static readonly IReadOnlyList<decimal> SquareMetersBaseline = new[] { 0.90m, 1.00m, 1.15m, 1.30m };

    // Deprem bölgesi: [0]=1. derece … [4]=5. derece, [5]=bilinmeyen.
    public static readonly IReadOnlyList<decimal> EarthquakeZoneBaseline =
        new[] { 1.50m, 1.30m, 1.15m, 1.05m, 1.00m, 1.15m };

    // Sağlık yaş bandı: [0]=0–17, [1]=18–30, [2]=31–45, [3]=46–60, [4]=>60.
    public static readonly IReadOnlyList<decimal> HealthAgeBaseline = new[] { 0.80m, 1.00m, 1.15m, 1.40m, 1.80m };

    // Sigara kullanım ek prim çarpanı (kullanmıyor = 1.00 sabittir).
    public const decimal SmokerSurchargeBaseline = 1.25m;

    // Risk skoru eşikleri: toplam çarpan (Total/Base) bu eşiklere göre sınıflandırılır.
    public const decimal MediumRiskThreshold = 1.10m;
    public const decimal HighRiskThreshold = 1.50m;
}
