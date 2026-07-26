using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Pricing.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Pricing.Commands.CreatePricingVersion;

// ADR-048: Fiyat değişikliği = YENİ tarife versiyonu. Mevcut versiyonlar hiçbir zaman güncellenmez;
// bu sayede geçmiş teklif/poliçe fiyatları kayıt düzeyinde korunur ve değişiklik geçmişi doğal oluşur.
public sealed record CreatePricingVersionCommand(
    DateTime EffectiveFrom,
    string? Note,
    IReadOnlyList<BranchRateInput> Rates) : ICommand<PricingVersionDto>;

public sealed record BranchRateInput(InsuranceBranch Branch, decimal BasePremium);
