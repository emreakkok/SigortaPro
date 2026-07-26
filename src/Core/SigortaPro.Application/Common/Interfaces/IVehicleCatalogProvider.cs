using SigortaPro.Application.Features.Vehicles.DTOs;

namespace SigortaPro.Application.Common.Interfaces;

// Araç marka/model kataloğunu sağlar (ARCHITECTURE_RULES.md §6.1 — arayüz Application'da, implementasyon
// Infrastructure'da). Veri, harici API veya yeni tablo yerine Infrastructure'da gömülü JSON kaynağı olarak
// tutulur ve bir defa okunup In-Memory cache'lenir (Singleton — ADR-036). IPricingEngine'in Application
// sözleşmesi + Infrastructure veri deseninin izidir (ADR-008).
public interface IVehicleCatalogProvider
{
    VehicleCatalogDto GetCatalog();
}
