using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;

namespace SigortaPro.Application.Features.Customers.Commands.AddVehicle;

// Oturum sahibi müşteri kendi profiline araç (Kasko/Trafik risk objesi) ekler.
public sealed record AddVehicleCommand(
    string PlateNumber,
    string Brand,
    string Model,
    int ManufactureYear,
    int EnginePowerHp) : ICommand<VehicleDto>;
