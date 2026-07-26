namespace SigortaPro.Application.Features.Cities.DTOs;

// İl kataloğu (Türkiye'nin 81 ili). Adres formlarındaki il girişini aranabilir combobox'a besler (ADR-037).
// Salt referans veridir; herhangi bir domain entity'sine veya veritabanına bağlı değildir.
public sealed record CityCatalogDto(IReadOnlyList<CityDto> Cities);
