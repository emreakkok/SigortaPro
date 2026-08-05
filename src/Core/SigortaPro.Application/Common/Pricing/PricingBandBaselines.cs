namespace SigortaPro.Application.Common.Pricing;

// Yerleşik (kod-sabit) bantlı faktör baseline'ları — motorun kullandığı SIRALI çarpan listeleriyle birebir.
// Yeni taslak versiyon bu değerlerle seed edilir ve bir versiyonun ilgili faktör grubu boşsa DTO bunları
// gösterir → admin her zaman GERÇEK sayıları görür, motorla asla sapmaz (baseline tek kaynaktan okunur).
public sealed record PricingBandBaselines(
    IReadOnlyList<decimal> DriverAge,
    IReadOnlyList<decimal> VehicleAge,
    IReadOnlyList<decimal> EnginePower,
    IReadOnlyList<decimal> VehicleUsage,
    IReadOnlyList<decimal> BonusMalus,
    IReadOnlyList<decimal> BuildingAge,
    IReadOnlyList<decimal> SquareMeters,
    IReadOnlyList<decimal> EarthquakeZone,
    IReadOnlyList<decimal> HealthAge,
    decimal SmokerSurcharge);
