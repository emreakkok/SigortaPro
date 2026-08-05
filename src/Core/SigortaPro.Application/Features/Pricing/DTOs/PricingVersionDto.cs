using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Pricing.DTOs;

// ADR-048: Bir tarife versiyonunun admin görünümü. Aktif/arşiv versiyonlar değişmezdir; yalnızca taslak
// düzenlenir. "Geçmiş" bu kayıtların kendisidir (ayrı audit tablosu yoktur).
public sealed record PricingVersionDto(
    Guid Id,
    int VersionNumber,
    // Taslak adı (kullanıcı tanımlı, oluştururken zorunlu). Eski kayıtlarda null → panel "v{no}" gösterir.
    string? Name,
    // Yaşam döngüsü durumu (Draft/Active/Archived) — panel "Taslak / Aktif / Arşiv" rozetlerini bundan üretir.
    PricingVersionStatus Status,
    // Geçerlilik başlangıcı (admin tarafından girilir). EffectiveTo opsiyonel bitiş; ActivatedAt sistemsel aktifleşme anı.
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    DateTime? ActivatedAt,
    string? Note,
    string? CreatedByName,
    DateTime CreatedAt,
    // Şu an yürürlükte mi (Status == Active). Geriye dönük uyumluluk için ayrı alan olarak da verilir.
    bool IsCurrent,
    // ADR-049: Yayınlanmış bir versiyon değil, yerleşik (kod-sabit) baz tarife (v0). Id/EffectiveFrom anlamsızdır.
    bool IsBaseline,
    IReadOnlyList<PricingBranchRateDto> Rates,
    // Baz prim dışındaki TÜM çarpanlar (ticari + aktüeryal faktör grupları). Versiyonun kendi seti; boşsa baseline.
    PricingRuleSetDto RuleSet);

public sealed record PricingBranchRateDto(
    InsuranceBranch Branch,
    decimal BasePremium,
    // Bir önceki versiyona göre değişim (ilk versiyonda null) — admin etkiyi görsün diye.
    decimal? PreviousBasePremium);

// Versiyonun tüm çarpan seti — panelde gruplar halinde düzenlenir. Bantlı faktörler SIRALI çarpan
// listeleridir; band etiketleri/sıraları frontend'de sabittir (PricingRuleSet'teki indeks sözleşmesiyle birebir).
public sealed record PricingRuleSetDto(
    // Ticari Ayarlar
    IReadOnlyList<PackageFactorDto> PackagePremiumFactors,
    IReadOnlyList<CityCoefficientDto> CityRiskCoefficients,
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
    decimal SmokerSurcharge);

public sealed record PackageFactorDto(CoveragePackage Package, decimal PremiumFactor);

public sealed record CityCoefficientDto(string City, decimal Coefficient);
