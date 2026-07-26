namespace SigortaPro.Application.Features.Vehicles.DTOs;

// Araç kataloğu: marka ve model listeleri. Frontend'in "Cascading Select" (aranabilir combobox) yapısını besler
// (ADR-036). Salt referans veridir; herhangi bir domain entity'sine veya veritabanına bağlı değildir.
public sealed record VehicleCatalogDto(IReadOnlyList<VehicleBrandDto> Brands);
