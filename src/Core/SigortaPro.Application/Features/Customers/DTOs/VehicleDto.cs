using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Customers.DTOs;

// UsagePurpose: kullanım amacı beyanı; bu alan eklenmeden kaydedilmiş araçlarda null.
public sealed record VehicleDto(
    Guid Id,
    string PlateNumber,
    string Brand,
    string Model,
    int ManufactureYear,
    int EnginePowerHp,
    VehicleUsage? UsagePurpose = null);
