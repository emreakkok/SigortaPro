namespace SigortaPro.Application.Common.Pricing;

// Prim dökümünde tek bir risk faktörünün etkisi: faktör adı, uygulanan çarpan ve açıklama.
public sealed record PricingBreakdownItem(
    string Factor,
    decimal Multiplier,
    string Description);
