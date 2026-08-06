using SigortaPro.Application.Common.Pricing;

namespace SigortaPro.Application.Common.Interfaces;

// Kural tabanlı mock fiyatlama motoru. Arayüz Application'da, implementasyon Infrastructure'da
// . Saf/deterministik bir fonksiyondur; aynı girdi her zaman aynı çıktıyı üretir.
public interface IPricingEngine
{
    // (additive, opsiyonel parametre): `rates` verilirse baz primler o tarifeden okunur;
    // verilmezse yerleşik baseline tarife kullanılır (mevcut çağrı noktaları ve davranış değişmez).
    PricingResult CalculatePremium(PricingRequest request, PricingRateSet? rates = null);
}
