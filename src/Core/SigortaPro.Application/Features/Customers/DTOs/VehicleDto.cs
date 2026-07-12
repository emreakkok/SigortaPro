namespace SigortaPro.Application.Features.Customers.DTOs;

public sealed record VehicleDto(
    Guid Id,
    string PlateNumber,
    string Brand,
    string Model,
    int ManufactureYear,
    int EnginePowerHp);
