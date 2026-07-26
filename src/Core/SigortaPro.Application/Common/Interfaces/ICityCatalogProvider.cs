using SigortaPro.Application.Features.Cities.DTOs;

namespace SigortaPro.Application.Common.Interfaces;

// İl kataloğunu sağlar (ARCHITECTURE_RULES.md §6.1 — arayüz Application'da, implementasyon Infrastructure'da).
// Veri, Infrastructure'da gömülü JSON kaynağı olarak tutulur ve bir defa okunup In-Memory cache'lenir
// (Singleton — ADR-037). IVehicleCatalogProvider (ADR-036) deseninin birebir izidir. İleride ilçe desteği,
// CityDto'ya additive alan ekleyerek bu arayüz değişmeden sağlanabilir.
public interface ICityCatalogProvider
{
    CityCatalogDto GetCatalog();
}
