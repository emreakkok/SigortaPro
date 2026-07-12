using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;

namespace SigortaPro.Application.Features.Customers.Commands.UpdateVehicle;

// Oturum sahibi müşteri kendi aracının bilgilerini günceller.
public sealed record UpdateVehicleCommand(
    Guid VehicleId,
    string PlateNumber,
    string Brand,
    string Model,
    int ManufactureYear,
    int EnginePowerHp) : ICommand<VehicleDto>;
