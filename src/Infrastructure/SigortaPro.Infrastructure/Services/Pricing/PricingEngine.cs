using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Infrastructure.Services.Pricing;

// ADR-008: Kural tabanlı mock fiyatlama motoru. Saf (deterministik, yan etkisiz) bir fonksiyondur;
// baz prim × risk çarpanları hesabı yapar ve prim dökümü + risk skoru üretir. Kural değerleri
// PRICING.md ile birebir eşleşir. Girdiler önceden hesaplanmış primitiflerdir (yaş vb.), böylece motor
// Quote akışından, domain entity'lerinden ve sistem saatinden bağımsızdır.
public sealed class PricingEngine : IPricingEngine
{
    public PricingResult CalculatePremium(PricingRequest request, PricingRateSet? rates = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request switch
        {
            VehiclePricingRequest vehicle => CalculateVehicle(vehicle, rates),
            PropertyPricingRequest property => CalculateProperty(property, rates),
            HealthPricingRequest health => CalculateHealth(health, rates),
            _ => throw new ArgumentException(
                $"Desteklenmeyen fiyatlama isteği tipi: {request.GetType().Name}", nameof(request)),
        };
    }

    private static PricingResult CalculateVehicle(VehiclePricingRequest request, PricingRateSet? rates)
    {
        EnsureBranch(request.Branch, InsuranceBranch.Kasko, InsuranceBranch.Trafik);

        // Tüm bant çarpanları tarifenin kural setinden (versiyonlu) okunur; kural seti bu faktörü içermiyorsa
        // yerleşik baseline kullanılır → geçmiş teklifler bit-aynı yeniden hesaplanır.
        var ruleSet = rates?.RuleSet;
        var breakdown = new List<PricingBreakdownItem>
        {
            DriverAgeFactor(request.DriverAge, ruleSet),
            VehicleAgeFactor(request.VehicleAge, ruleSet),
            EnginePowerFactor(request.EnginePowerHp, ruleSet),
            CityRiskFactor(request.City, ruleSet),
        };

        // ADR-059: Bonus-Malus yalnızca basamak nötr DEĞİLSE prim dökümüne girer. Basamağı 0 olan
        // (yeni müşteri veya bu sistem öncesi oluşmuş) kayıtlarda kalem hiç üretilmez → eski tekliflerin
        // dökümü birebir korunur ve kullanıcıya etkisiz bir kalem gösterilmez.
        if (request.NoClaimTier != 0)
        {
            breakdown.Add(BonusMalusFactor(request.NoClaimTier, ruleSet));
        }

        // ADR-057: Kullanım amacı yalnızca BEYAN edildiyse fiyatlanır ve dökümde görünür; beyanı olmayan
        // (eski) kayıtlarda faktör hiç üretilmez → geçmiş fiyatlar/dökümler değişmez.
        if (request.UsagePurpose is not null)
        {
            breakdown.Add(VehicleUsageFactor(request.UsagePurpose.Value, ruleSet));
        }

        return BuildResult(request.Branch, breakdown, rates);
    }

    private static PricingResult CalculateProperty(PropertyPricingRequest request, PricingRateSet? rates)
    {
        EnsureBranch(request.Branch, InsuranceBranch.Konut, InsuranceBranch.Dask);

        var ruleSet = rates?.RuleSet;
        var breakdown = new List<PricingBreakdownItem>
        {
            BuildingAgeFactor(request.BuildingAge, ruleSet),
            SquareMetersFactor(request.SquareMeters, ruleSet),
            EarthquakeZoneFactor(request.EarthquakeZone, ruleSet),
        };

        return BuildResult(request.Branch, breakdown, rates);
    }

    private static PricingResult CalculateHealth(HealthPricingRequest request, PricingRateSet? rates)
    {
        var ruleSet = rates?.RuleSet;
        var breakdown = new List<PricingBreakdownItem>
        {
            HealthAgeFactor(request.Age, ruleSet),
            SmokerFactor(request.IsSmoker, ruleSet),
        };

        return BuildResult(InsuranceBranch.Saglik, breakdown, rates);
    }

    private static PricingResult BuildResult(
        InsuranceBranch branch, IReadOnlyList<PricingBreakdownItem> breakdown, PricingRateSet? rates)
    {
        // ADR-048: Baz prim verilen tarifeden okunur; tarife yoksa/branşı içermiyorsa yerleşik baseline
        // kullanılır → tarife yönetimi eklenmeden önce oluşmuş kayıtlar birebir aynı sonucu üretir.
        var basePremium = rates?.BasePremiumFor(branch) ?? PricingRuleTables.BasePremiums[branch];
        var aggregateMultiplier = breakdown.Aggregate(1m, (accumulator, item) => accumulator * item.Multiplier);
        var totalPremium = Math.Round(basePremium * aggregateMultiplier, 2, MidpointRounding.AwayFromZero);
        var riskScore = DetermineRiskScore(aggregateMultiplier);

        return new PricingResult(branch, basePremium, totalPremium, riskScore, breakdown);
    }

    private static RiskScore DetermineRiskScore(decimal aggregateMultiplier) => aggregateMultiplier switch
    {
        < PricingRuleTables.MediumRiskThreshold => RiskScore.Low,
        < PricingRuleTables.HighRiskThreshold => RiskScore.Medium,
        _ => RiskScore.High,
    };

    // --- Kasko / Trafik faktörleri (band sınırları sabit; çarpan değerleri versiyonlu — ruleSet ?? baseline) ---

    private static PricingBreakdownItem DriverAgeFactor(int driverAge, PricingRuleSet? ruleSet)
    {
        var (index, description) = driverAge switch
        {
            < 25 => (0, "Genç sürücü ek primi (25 yaş altı)."),
            > 65 => (2, "İleri yaş ek primi (65 yaş üstü)."),
            _ => (1, "Standart yaş grubu (25-65)."),
        };
        var multiplier = PricingRuleSet.BandFactor(ruleSet?.DriverAgeFactors, index)
            ?? PricingRuleTables.DriverAgeBaseline[index];
        return new PricingBreakdownItem("Sürücü Yaşı", multiplier, description);
    }

    private static PricingBreakdownItem VehicleAgeFactor(int vehicleAge, PricingRuleSet? ruleSet)
    {
        var (index, description) = vehicleAge switch
        {
            <= 3 => (0, "Yeni araç (0-3 yaş) — yüksek onarım/değer maliyeti."),
            <= 10 => (1, "Orta yaş araç (4-10 yaş)."),
            _ => (2, "Eski araç (10 yaş üstü) — düşük araç değeri."),
        };
        var multiplier = PricingRuleSet.BandFactor(ruleSet?.VehicleAgeFactors, index)
            ?? PricingRuleTables.VehicleAgeBaseline[index];
        return new PricingBreakdownItem("Araç Yaşı", multiplier, description);
    }

    private static PricingBreakdownItem EnginePowerFactor(int enginePowerHp, PricingRuleSet? ruleSet)
    {
        var (index, description) = enginePowerHp switch
        {
            <= 100 => (0, "Düşük motor gücü (≤100 HP)."),
            <= 160 => (1, "Orta motor gücü (101-160 HP)."),
            <= 240 => (2, "Yüksek motor gücü (161-240 HP)."),
            _ => (3, "Çok yüksek motor gücü (240 HP üstü)."),
        };
        var multiplier = PricingRuleSet.BandFactor(ruleSet?.EnginePowerFactors, index)
            ?? PricingRuleTables.EnginePowerBaseline[index];
        return new PricingBreakdownItem("Motor Gücü", multiplier, description);
    }

    // Versiyonlanmış il risk katsayısı. ruleSet verilmişse (yeni tarife) katsayı ORADAN okunur; verilmemişse
    // (kural seti eklenmeden önceki versiyonlar veya baz-prim-only tarife) yerleşik baseline tablosu kullanılır
    // → eski teklifler bit-aynı yeniden hesaplanır.
    private static PricingBreakdownItem CityRiskFactor(string? city, PricingRuleSet? ruleSet)
    {
        var trimmed = city?.Trim();

        if (ruleSet is not null)
        {
            var known = !string.IsNullOrWhiteSpace(trimmed)
                && ruleSet.CityRiskCoefficients.Keys.Any(key => string.Equals(key, trimmed, StringComparison.OrdinalIgnoreCase));
            var coefficient = ruleSet.CityCoefficientFor(trimmed);
            var description = known ? $"{trimmed} ili risk katsayısı." : "Standart il risk katsayısı.";
            return new PricingBreakdownItem("İl Risk Katsayısı", coefficient, description);
        }

        var baselineCoefficient = PricingRuleTables.DefaultCityRiskCoefficient;
        var baselineDescription = "Standart il risk katsayısı.";

        if (!string.IsNullOrWhiteSpace(trimmed)
            && PricingRuleTables.CityRiskCoefficients.TryGetValue(trimmed, out var cityCoefficient))
        {
            baselineCoefficient = cityCoefficient;
            baselineDescription = $"{trimmed} ili risk katsayısı.";
        }

        return new PricingBreakdownItem("İl Risk Katsayısı", baselineCoefficient, baselineDescription);
    }

    private static PricingBreakdownItem VehicleUsageFactor(VehicleUsage usage, PricingRuleSet? ruleSet)
    {
        var index = (int)usage;
        var multiplier = PricingRuleSet.BandFactor(ruleSet?.VehicleUsageFactors, index)
            ?? PricingRuleTables.VehicleUsageBaseline[index];
        var description = usage switch
        {
            VehicleUsage.Hususi => "Hususi (kişisel) kullanım — referans seviye.",
            VehicleUsage.Ticari => "Ticari kullanım — daha yüksek yıllık kilometre ve kaza sıklığı.",
            VehicleUsage.Taksi => "Taksi/yolcu taşımacılığı — en yüksek maruziyet.",
            _ => "Kullanım amacı.",
        };

        return new PricingBreakdownItem("Kullanım Amacı", multiplier, description);
    }

    // ADR-059: Hasar geçmişinin tek çarpanı. Negatif basamak ek prim (malus), pozitif basamak indirim (bonus).
    private static PricingBreakdownItem BonusMalusFactor(int step, PricingRuleSet? ruleSet)
    {
        var effectiveStep = Math.Clamp(step, BonusMalusScale.MinStep, BonusMalusScale.MaxStep);
        var index = effectiveStep - BonusMalusScale.MinStep;
        var multiplier = PricingRuleSet.BandFactor(ruleSet?.BonusMalusFactors, index)
            ?? PricingRuleTables.BonusMalusBaseline[index];

        var description = effectiveStep < 0
            ? $"Hasar geçmişi ek primi ({effectiveStep}. basamak)."
            : $"Hasarsızlık indirimi ({effectiveStep}. basamak).";

        return new PricingBreakdownItem("Hasarsızlık Basamağı", multiplier, description);
    }

    // --- Konut / DASK faktörleri ---

    private static PricingBreakdownItem BuildingAgeFactor(int buildingAge, PricingRuleSet? ruleSet)
    {
        var (index, description) = buildingAge switch
        {
            <= 5 => (0, "Yeni bina (0-5 yaş)."),
            <= 20 => (1, "Orta yaş bina (6-20 yaş)."),
            <= 40 => (2, "Eski bina (21-40 yaş)."),
            _ => (3, "Çok eski bina (40 yaş üstü)."),
        };
        var multiplier = PricingRuleSet.BandFactor(ruleSet?.BuildingAgeFactors, index)
            ?? PricingRuleTables.BuildingAgeBaseline[index];
        return new PricingBreakdownItem("Bina Yaşı", multiplier, description);
    }

    private static PricingBreakdownItem SquareMetersFactor(int squareMeters, PricingRuleSet? ruleSet)
    {
        var (index, description) = squareMeters switch
        {
            <= 75 => (0, "Küçük konut (≤75 m²)."),
            <= 120 => (1, "Orta konut (76-120 m²)."),
            <= 200 => (2, "Büyük konut (121-200 m²)."),
            _ => (3, "Çok büyük konut (200 m² üstü)."),
        };
        var multiplier = PricingRuleSet.BandFactor(ruleSet?.SquareMetersFactors, index)
            ?? PricingRuleTables.SquareMetersBaseline[index];
        return new PricingBreakdownItem("Metrekare", multiplier, description);
    }

    private static PricingBreakdownItem EarthquakeZoneFactor(int earthquakeZone, PricingRuleSet? ruleSet)
    {
        var (index, description) = earthquakeZone switch
        {
            1 => (0, "1. derece deprem bölgesi (en yüksek risk)."),
            2 => (1, "2. derece deprem bölgesi."),
            3 => (2, "3. derece deprem bölgesi."),
            4 => (3, "4. derece deprem bölgesi."),
            5 => (4, "5. derece deprem bölgesi (en düşük risk)."),
            _ => (5, "Bilinmeyen deprem bölgesi — orta risk varsayıldı."),
        };
        var multiplier = PricingRuleSet.BandFactor(ruleSet?.EarthquakeZoneFactors, index)
            ?? PricingRuleTables.EarthquakeZoneBaseline[index];
        return new PricingBreakdownItem("Deprem Bölgesi", multiplier, description);
    }

    // --- Sağlık faktörleri ---

    private static PricingBreakdownItem HealthAgeFactor(int age, PricingRuleSet? ruleSet)
    {
        var (index, description) = age switch
        {
            <= 17 => (0, "Çocuk/genç yaş bandı (0-17)."),
            <= 30 => (1, "Genç yetişkin yaş bandı (18-30)."),
            <= 45 => (2, "Orta yaş bandı (31-45)."),
            <= 60 => (3, "İleri orta yaş bandı (46-60)."),
            _ => (4, "İleri yaş bandı (60 üstü)."),
        };
        var multiplier = PricingRuleSet.BandFactor(ruleSet?.HealthAgeFactors, index)
            ?? PricingRuleTables.HealthAgeBaseline[index];
        return new PricingBreakdownItem("Yaş Bandı", multiplier, description);
    }

    private static PricingBreakdownItem SmokerFactor(bool isSmoker, PricingRuleSet? ruleSet)
    {
        if (!isSmoker)
        {
            return new PricingBreakdownItem("Sigara Kullanımı", 1.00m, "Sigara kullanmıyor.");
        }

        var multiplier = ruleSet?.SmokerSurcharge ?? PricingRuleTables.SmokerSurchargeBaseline;
        return new PricingBreakdownItem("Sigara Kullanımı", multiplier, "Sigara kullanımı beyanı — ek prim.");
    }

    private static void EnsureBranch(InsuranceBranch actual, params InsuranceBranch[] allowed)
    {
        if (!allowed.Contains(actual))
        {
            throw new ArgumentException(
                $"'{actual}' branşı bu risk objesi tipiyle fiyatlandırılamaz. Beklenen: {string.Join(", ", allowed)}.",
                nameof(actual));
        }
    }
}
