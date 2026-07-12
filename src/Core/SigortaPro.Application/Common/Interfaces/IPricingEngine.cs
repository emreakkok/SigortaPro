using SigortaPro.Application.Common.Pricing;

namespace SigortaPro.Application.Common.Interfaces;

// Kural tabanlı mock fiyatlama motoru (ADR-008). Arayüz Application'da, implementasyon Infrastructure'da
// (ARCHITECTURE_RULES.md §6.1). Saf/deterministik bir fonksiyondur; aynı girdi her zaman aynı çıktıyı üretir.
public interface IPricingEngine
{
    PricingResult CalculatePremium(PricingRequest request);
}
