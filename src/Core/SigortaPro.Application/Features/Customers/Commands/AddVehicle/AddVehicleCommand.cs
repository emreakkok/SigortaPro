using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Customers.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Customers.Commands.AddVehicle;

// Oturum sahibi müşteri kendi profiline araç (Kasko/Trafik risk objesi) ekler.
// UsagePurpose (ADR-057): kullanım amacı BEYANI — Kasko/Trafik primini etkiler, bu yüzden zorunludur ve
// varsayılan atanmaz (kullanıcı bilinçli seçer). Mevcut araçlarda null kalabilir; geriye dönük uygulanmaz.
// CustomerId (acente destekli, additive): dolu ise araç bu müşteri ADINA eklenir ve çağıran yalnızca acente
// personeli olabilir (controller nested route'ta set eder). null = self-service (oturum sahibi müşteri).
public sealed record AddVehicleCommand(
    string PlateNumber,
    string Brand,
    string Model,
    int ManufactureYear,
    int EnginePowerHp,
    VehicleUsage? UsagePurpose = null,
    Guid? CustomerId = null) : ICommand<VehicleDto>;
