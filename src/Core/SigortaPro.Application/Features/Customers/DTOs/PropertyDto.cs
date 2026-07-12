namespace SigortaPro.Application.Features.Customers.DTOs;

public sealed record PropertyDto(
    Guid Id,
    AddressDto Address,
    int BuildingAge,
    int SquareMeters,
    int EarthquakeZone);
