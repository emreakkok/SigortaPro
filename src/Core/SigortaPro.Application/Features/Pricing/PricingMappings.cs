using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Application.Features.Quotes;
using SigortaPro.Domain.Entities;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Pricing;

// Tarife versiyonu → DTO eşlemeleri (AutoMapper kullanılmaz). Bir versiyonun kural setinde bir faktör grubu
// yoksa (o grup eklenmeden önce oluşmuş versiyonlar) yerleşik baseline değerleri gösterilir → admin her zaman
// GERÇEK sayıları görür, motorla asla sapmaz (baseline tek kaynaktan okunur).
internal static class PricingMappings
{
    // TÜM çarpanlarıyla yerleşik baseline kural seti (ticari kaldıraçlar + tüm faktör grupları). Yeni taslak
    // (aktif versiyon yoksa) bununla seed edilir.
    public static PricingRuleSet BuildBaselineRuleSet(IPricingBaselineProvider baseline)
    {
        var bands = baseline.BandBaselines;
        return new PricingRuleSet(
            PackagePremiumFactors: CoveragePackageFactors.ComparablePackages
                .ToDictionary(package => package, CoveragePackageFactors.PremiumFactor),
            CityRiskCoefficients: baseline.BaselineCityRiskCoefficients
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase),
            DefaultCityRiskCoefficient: baseline.BaselineDefaultCityRiskCoefficient,
            RenewalDiscountFactor: 1.00m,
            DriverAgeFactors: bands.DriverAge,
            VehicleAgeFactors: bands.VehicleAge,
            EnginePowerFactors: bands.EnginePower,
            VehicleUsageFactors: bands.VehicleUsage,
            BonusMalusFactors: bands.BonusMalus,
            BuildingAgeFactors: bands.BuildingAge,
            SquareMetersFactors: bands.SquareMeters,
            EarthquakeZoneFactors: bands.EarthquakeZone,
            HealthAgeFactors: bands.HealthAge,
            SmokerSurcharge: bands.SmokerSurcharge);
    }

    // Bir kaynağı (eski/kısmi kural seti) baseline ile TAMAMLAR: boş kalan faktör grupları baseline değerini
    // alır. Yeni taslak, aktif versiyondan seed edilirken kullanılır → taslak her zaman tam bir set taşır.
    public static PricingRuleSet Complete(PricingRuleSet source, IPricingBaselineProvider baseline)
    {
        var bands = baseline.BandBaselines;
        return source with
        {
            DriverAgeFactors = source.DriverAgeFactors ?? bands.DriverAge,
            VehicleAgeFactors = source.VehicleAgeFactors ?? bands.VehicleAge,
            EnginePowerFactors = source.EnginePowerFactors ?? bands.EnginePower,
            VehicleUsageFactors = source.VehicleUsageFactors ?? bands.VehicleUsage,
            BonusMalusFactors = source.BonusMalusFactors ?? bands.BonusMalus,
            BuildingAgeFactors = source.BuildingAgeFactors ?? bands.BuildingAge,
            SquareMetersFactors = source.SquareMetersFactors ?? bands.SquareMeters,
            EarthquakeZoneFactors = source.EarthquakeZoneFactors ?? bands.EarthquakeZone,
            HealthAgeFactors = source.HealthAgeFactors ?? bands.HealthAge,
            SmokerSurcharge = source.SmokerSurcharge ?? bands.SmokerSurcharge,
        };
    }

    public static PricingRuleSetDto ToRuleSetDto(PricingRuleSet? ruleSet, IPricingBaselineProvider baseline)
    {
        var bands = baseline.BandBaselines;
        var effective = ruleSet ?? BuildBaselineRuleSet(baseline);

        return new PricingRuleSetDto(
            CoveragePackageFactors.ComparablePackages
                .Select(package => new PackageFactorDto(
                    package,
                    effective.PackagePremiumFactorFor(package) ?? CoveragePackageFactors.PremiumFactor(package)))
                .ToList(),
            effective.CityRiskCoefficients
                .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .Select(entry => new CityCoefficientDto(entry.Key, entry.Value))
                .ToList(),
            effective.DefaultCityRiskCoefficient,
            effective.RenewalDiscountFactor,
            // Bir faktör grubu bu versiyonda boşsa yerleşik baseline gösterilir (motor da onu kullanır).
            effective.DriverAgeFactors ?? bands.DriverAge,
            effective.VehicleAgeFactors ?? bands.VehicleAge,
            effective.EnginePowerFactors ?? bands.EnginePower,
            effective.VehicleUsageFactors ?? bands.VehicleUsage,
            effective.BonusMalusFactors ?? bands.BonusMalus,
            effective.BuildingAgeFactors ?? bands.BuildingAge,
            effective.SquareMetersFactors ?? bands.SquareMeters,
            effective.EarthquakeZoneFactors ?? bands.EarthquakeZone,
            effective.HealthAgeFactors ?? bands.HealthAge,
            effective.SmokerSurcharge ?? bands.SmokerSurcharge);
    }

    public static PricingVersionDto ToDto(
        PricingVersion version,
        IReadOnlyDictionary<InsuranceBranch, decimal> previousBasePremiums,
        IPricingBaselineProvider baseline) => new(
        version.Id,
        version.VersionNumber,
        version.Name,
        version.Status,
        version.EffectiveFrom,
        version.EffectiveTo,
        version.ActivatedAt,
        version.Note,
        version.CreatedByName,
        version.CreatedAt,
        IsCurrent: version.Status == PricingVersionStatus.Active,
        IsBaseline: false,
        Rates: version.Rates
            .OrderBy(rate => rate.Branch)
            .Select(rate => new PricingBranchRateDto(
                rate.Branch,
                rate.BasePremium,
                previousBasePremiums.TryGetValue(rate.Branch, out var previous) ? previous : null))
            .ToList(),
        RuleSet: ToRuleSetDto(version.RuleSet, baseline));
}
