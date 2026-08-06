using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Customers.Commands.UpdateVehicle;

// Oturum sahibi müşteri kendi aracının bilgilerini günceller.
// UsagePurpose: kullanım amacı beyanı; güncellemede de zorunludur.
// Güncelleme YALNIZCA yeni teklifleri etkiler — mevcut teklifler girdiyi snapshot'ladığından değişmez.
public sealed record UpdateVehicleCommand(
    Guid VehicleId,
    string PlateNumber,
    string Brand,
    string Model,
    int ManufactureYear,
    int EnginePowerHp,
    VehicleUsage? UsagePurpose = null) : ICommand<VehicleDto>;
