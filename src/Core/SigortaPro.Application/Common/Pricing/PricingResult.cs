using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Common.Pricing;

// Fiyatlama motorunun çıktısı: baz prim, uygulanan çarpanların dökümü, toplam prim ve risk skoru.
// TotalPremium = BasePremium × (Breakdown içindeki tüm çarpanların çarpımı), 2 ondalığa yuvarlanır.
public sealed record PricingResult(
    InsuranceBranch Branch,
    decimal BasePremium,
    decimal TotalPremium,
    RiskScore RiskScore,
    IReadOnlyList<PricingBreakdownItem> Breakdown);
