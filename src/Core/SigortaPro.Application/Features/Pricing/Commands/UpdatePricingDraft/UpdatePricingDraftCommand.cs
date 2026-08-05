using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Pricing.Commands.UpdatePricingDraft;

// ADR-048: TASLAK tarife versiyonunu düzenler (ad + geçerlilik tarihleri + baz primler + TÜM çarpan grupları).
// Yalnızca taslak düzenlenebilir; aktif/arşiv versiyon asla değişmez (geçmiş primler korunur). Düzenleme CANLI
// fiyatları etkilemez — değişiklik ancak "Aktifleştir" ile yürürlüğe girer.
//
// Bantlı faktörler SIRALI çarpan listeleridir (PricingRuleSet indeks sözleşmesiyle birebir):
//   DriverAge[3] · VehicleAge[3] · EnginePower[4] · VehicleUsage[3] · BonusMalus[10] · BuildingAge[4] ·
//   SquareMeters[4] · EarthquakeZone[6] · HealthAge[5].
public sealed record UpdatePricingDraftCommand(
    Guid VersionId,
    string Name,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string? Note,
    IReadOnlyList<BranchRateInput> Rates,
    // Ticari Ayarlar
    IReadOnlyList<PackageFactorInput> PackagePremiumFactors,
    IReadOnlyList<CityCoefficientInput> CityRiskCoefficients,
    decimal DefaultCityRiskCoefficient,
    decimal RenewalDiscountFactor,
    // Sürücü Faktörleri
    IReadOnlyList<decimal> DriverAgeFactors,
    // Araç Faktörleri
    IReadOnlyList<decimal> VehicleAgeFactors,
    IReadOnlyList<decimal> EnginePowerFactors,
    IReadOnlyList<decimal> VehicleUsageFactors,
    IReadOnlyList<decimal> BonusMalusFactors,
    // Konut Faktörleri
    IReadOnlyList<decimal> BuildingAgeFactors,
    IReadOnlyList<decimal> SquareMetersFactors,
    IReadOnlyList<decimal> EarthquakeZoneFactors,
    // Sağlık Faktörleri
    IReadOnlyList<decimal> HealthAgeFactors,
    decimal SmokerSurcharge) : ICommand<PricingVersionDto>;

public sealed record BranchRateInput(InsuranceBranch Branch, decimal BasePremium);

public sealed record PackageFactorInput(CoveragePackage Package, decimal PremiumFactor);

public sealed record CityCoefficientInput(string City, decimal Coefficient);
