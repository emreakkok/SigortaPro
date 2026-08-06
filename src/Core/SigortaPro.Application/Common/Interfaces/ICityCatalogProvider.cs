using SigortaPro.Application.Features.Cities.DTOs;

namespace SigortaPro.Application.Common.Interfaces;

// İl kataloğunu sağlar.
// Veri, Infrastructure'da gömülü JSON kaynağı olarak tutulur ve bir defa okunup In-Memory cache'lenir
// (Singleton). IVehicleCatalogProvider deseninin birebir izidir. İleride ilçe desteği,
// CityDto'ya additive alan ekleyerek bu arayüz değişmeden sağlanabilir.
public interface ICityCatalogProvider
{
    CityCatalogDto GetCatalog();
}
