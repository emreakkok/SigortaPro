using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Pricing.DTOs;

// ADR-048: Bir tarife versiyonunun admin görünümü. Versiyonlar değişmezdir; "geçmiş" bu kayıtların
// kendisidir (ayrı audit tablosu yoktur).
public sealed record PricingVersionDto(
    Guid Id,
    int VersionNumber,
    DateTime EffectiveFrom,
    string? Note,
    string? CreatedByName,
    DateTime CreatedAt,
    // Bu versiyon şu an yürürlükte mi (yürürlüktekilerin en yenisi).
    bool IsCurrent,
    // Geçerlilik tarihi gelecekte mi (henüz yürürlüğe girmedi).
    bool IsScheduled,
    // ADR-049: Bu satır yayınlanmış bir versiyon değil, yerleşik (kod-sabit) baz tarifedir. Admin ekranı
    // "Varsayılan" yerine gerçek sayıları göstersin diye v0 olarak listelenir; Id/EffectiveFrom anlamsızdır.
    bool IsBaseline,
    IReadOnlyList<PricingBranchRateDto> Rates);

public sealed record PricingBranchRateDto(
    InsuranceBranch Branch,
    decimal BasePremium,
    // Bir önceki versiyona göre değişim (ilk versiyonda null) — admin etkiyi görsün diye.
    decimal? PreviousBasePremium);
