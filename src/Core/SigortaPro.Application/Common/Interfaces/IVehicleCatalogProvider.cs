using SigortaPro.Application.Features.Vehicles.DTOs;

namespace SigortaPro.Application.Common.Interfaces;

// Araç marka/model kataloğunu sağlar — arayüz Application'da, implementasyon
// Infrastructure'da). Veri, harici API veya yeni tablo yerine Infrastructure'da gömülü JSON kaynağı olarak
// tutulur ve bir defa okunup In-Memory cache'lenir (Singleton). IPricingEngine'in Application
// sözleşmesi + Infrastructure veri deseninin izidir.
public interface IVehicleCatalogProvider
{
    VehicleCatalogDto GetCatalog();
}
