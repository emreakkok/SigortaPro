using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Common.Pricing;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Infrastructure.Services.Pricing;

// ADR-008: Kural tabanlı mock fiyatlama motoru. Saf (deterministik, yan etkisiz) bir fonksiyondur;
// baz prim × risk çarpanları hesabı yapar ve prim dökümü + risk skoru üretir. Kural değerleri
// PRICING.md ile birebir eşleşir. Girdiler önceden hesaplanmış primitiflerdir (yaş vb.), böylece motor
// Quote akışından, domain entity'lerinden ve sistem saatinden bağımsızdır.
public sealed class PricingEngine : IPricingEngine
{
    public PricingResult CalculatePremium(PricingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request switch
        {
            VehiclePricingRequest vehicle => CalculateVehicle(vehicle),
            PropertyPricingRequest property => CalculateProperty(property),
            HealthPricingRequest health => CalculateHealth(health),
            _ => throw new ArgumentException(
                $"Desteklenmeyen fiyatlama isteği tipi: {request.GetType().Name}", nameof(request)),
        };
    }

    private static PricingResult CalculateVehicle(VehiclePricingRequest request)
    {
        EnsureBranch(request.Branch, InsuranceBranch.Kasko, InsuranceBranch.Trafik);

        var breakdown = new List<PricingBreakdownItem>
        {
            DriverAgeFactor(request.DriverAge),
            VehicleAgeFactor(request.VehicleAge),
            EnginePowerFactor(request.EnginePowerHp),
            CityRiskFactor(request.City),
            NoClaimFactor(request.NoClaimTier),
        };

        return BuildResult(request.Branch, breakdown);
    }

    private static PricingResult CalculateProperty(PropertyPricingRequest request)
    {
        EnsureBranch(request.Branch, InsuranceBranch.Konut, InsuranceBranch.Dask);

        var breakdown = new List<PricingBreakdownItem>
        {
            BuildingAgeFactor(request.BuildingAge),
            SquareMetersFactor(request.SquareMeters),
            EarthquakeZoneFactor(request.EarthquakeZone),
        };

        return BuildResult(request.Branch, breakdown);
    }

    private static PricingResult CalculateHealth(HealthPricingRequest request)
    {
        var breakdown = new List<PricingBreakdownItem>
        {
            HealthAgeFactor(request.Age),
            SmokerFactor(request.IsSmoker),
        };

        return BuildResult(InsuranceBranch.Saglik, breakdown);
    }

    private static PricingResult BuildResult(InsuranceBranch branch, IReadOnlyList<PricingBreakdownItem> breakdown)
    {
        var basePremium = PricingRuleTables.BasePremiums[branch];
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

    // --- Kasko / Trafik faktörleri ---

    private static PricingBreakdownItem DriverAgeFactor(int driverAge) => driverAge switch
    {
        < 25 => new("Sürücü Yaşı", 1.30m, "Genç sürücü ek primi (25 yaş altı)."),
        > 65 => new("Sürücü Yaşı", 1.15m, "İleri yaş ek primi (65 yaş üstü)."),
        _ => new("Sürücü Yaşı", 1.00m, "Standart yaş grubu (25-65)."),
    };

    private static PricingBreakdownItem VehicleAgeFactor(int vehicleAge) => vehicleAge switch
    {
        <= 3 => new("Araç Yaşı", 1.15m, "Yeni araç (0-3 yaş) — yüksek onarım/değer maliyeti."),
        <= 10 => new("Araç Yaşı", 1.00m, "Orta yaş araç (4-10 yaş)."),
        _ => new("Araç Yaşı", 0.85m, "Eski araç (10 yaş üstü) — düşük araç değeri."),
    };

    private static PricingBreakdownItem EnginePowerFactor(int enginePowerHp) => enginePowerHp switch
    {
        <= 100 => new("Motor Gücü", 1.00m, "Düşük motor gücü (≤100 HP)."),
        <= 160 => new("Motor Gücü", 1.10m, "Orta motor gücü (101-160 HP)."),
        <= 240 => new("Motor Gücü", 1.25m, "Yüksek motor gücü (161-240 HP)."),
        _ => new("Motor Gücü", 1.45m, "Çok yüksek motor gücü (240 HP üstü)."),
    };

    private static PricingBreakdownItem CityRiskFactor(string? city)
    {
        var coefficient = PricingRuleTables.DefaultCityRiskCoefficient;
        var description = "Standart il risk katsayısı.";

        if (!string.IsNullOrWhiteSpace(city)
            && PricingRuleTables.CityRiskCoefficients.TryGetValue(city.Trim(), out var cityCoefficient))
        {
            coefficient = cityCoefficient;
            description = $"{city.Trim()} ili risk katsayısı.";
        }

        return new PricingBreakdownItem("İl Risk Katsayısı", coefficient, description);
    }

    private static PricingBreakdownItem NoClaimFactor(int noClaimTier)
    {
        var effectiveTier = Math.Clamp(noClaimTier, 0, PricingRuleTables.MaxNoClaimTier);
        var multiplier = 1.00m - (effectiveTier * PricingRuleTables.NoClaimDiscountPerTier);

        var description = effectiveTier == 0
            ? "Hasarsızlık indirimi yok (0. basamak)."
            : $"Hasarsızlık indirimi ({effectiveTier}. basamak, %{effectiveTier * 5}).";

        return new PricingBreakdownItem("Hasarsızlık İndirimi", multiplier, description);
    }

    // --- Konut / DASK faktörleri ---

    private static PricingBreakdownItem BuildingAgeFactor(int buildingAge) => buildingAge switch
    {
        <= 5 => new("Bina Yaşı", 0.95m, "Yeni bina (0-5 yaş)."),
        <= 20 => new("Bina Yaşı", 1.00m, "Orta yaş bina (6-20 yaş)."),
        <= 40 => new("Bina Yaşı", 1.10m, "Eski bina (21-40 yaş)."),
        _ => new("Bina Yaşı", 1.25m, "Çok eski bina (40 yaş üstü)."),
    };

    private static PricingBreakdownItem SquareMetersFactor(int squareMeters) => squareMeters switch
    {
        <= 75 => new("Metrekare", 0.90m, "Küçük konut (≤75 m²)."),
        <= 120 => new("Metrekare", 1.00m, "Orta konut (76-120 m²)."),
        <= 200 => new("Metrekare", 1.15m, "Büyük konut (121-200 m²)."),
        _ => new("Metrekare", 1.30m, "Çok büyük konut (200 m² üstü)."),
    };

    private static PricingBreakdownItem EarthquakeZoneFactor(int earthquakeZone) => earthquakeZone switch
    {
        1 => new("Deprem Bölgesi", 1.50m, "1. derece deprem bölgesi (en yüksek risk)."),
        2 => new("Deprem Bölgesi", 1.30m, "2. derece deprem bölgesi."),
        3 => new("Deprem Bölgesi", 1.15m, "3. derece deprem bölgesi."),
        4 => new("Deprem Bölgesi", 1.05m, "4. derece deprem bölgesi."),
        5 => new("Deprem Bölgesi", 1.00m, "5. derece deprem bölgesi (en düşük risk)."),
        _ => new("Deprem Bölgesi", 1.15m, "Bilinmeyen deprem bölgesi — orta risk varsayıldı."),
    };

    // --- Sağlık faktörleri ---

    private static PricingBreakdownItem HealthAgeFactor(int age) => age switch
    {
        <= 17 => new("Yaş Bandı", 0.80m, "Çocuk/genç yaş bandı (0-17)."),
        <= 30 => new("Yaş Bandı", 1.00m, "Genç yetişkin yaş bandı (18-30)."),
        <= 45 => new("Yaş Bandı", 1.15m, "Orta yaş bandı (31-45)."),
        <= 60 => new("Yaş Bandı", 1.40m, "İleri orta yaş bandı (46-60)."),
        _ => new("Yaş Bandı", 1.80m, "İleri yaş bandı (60 üstü)."),
    };

    private static PricingBreakdownItem SmokerFactor(bool isSmoker) => isSmoker
        ? new("Sigara Kullanımı", 1.25m, "Sigara kullanımı beyanı — ek prim.")
        : new("Sigara Kullanımı", 1.00m, "Sigara kullanmıyor.");

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
