using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Pricing;

// Fiyatlama motorunun girdisi. Motor saf (deterministik) bir fonksiyon olduğundan istek, önceden
// hesaplanmış primitif değerler taşır (örn. sürücü yaşı, araç yaşı) — motor DateTime.Now veya domain
// entity'lerine bağımlı değildir; bu sayede Quote akışından (Task 9) bağımsız ve tam test edilebilirdir (ADR-008).
public abstract record PricingRequest(InsuranceBranch Branch);

// Kasko / Trafik risk objesi (araç) girdisi.
public sealed record VehiclePricingRequest(
    InsuranceBranch Branch,
    int DriverAge,
    int VehicleAge,
    int EnginePowerHp,
    string City,
    int NoClaimTier) : PricingRequest(Branch);

// Konut / DASK risk objesi (bina) girdisi.
public sealed record PropertyPricingRequest(
    InsuranceBranch Branch,
    int BuildingAge,
    int SquareMeters,
    int EarthquakeZone) : PricingRequest(Branch);

// Sağlık risk objesi (kişi) girdisi. Branş sabit olarak Saglik'tir.
public sealed record HealthPricingRequest(
    int Age,
    bool IsSmoker) : PricingRequest(InsuranceBranch.Saglik);
