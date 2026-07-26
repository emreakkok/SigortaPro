using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

public class Vehicle : BaseEntity, IAggregateRoot
{
    protected Vehicle()
    {
    }

    public Vehicle(
        Guid customerId,
        string plateNumber,
        string brand,
        string model,
        int manufactureYear,
        int enginePowerHp,
        VehicleUsage? usagePurpose = null)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        PlateNumber = plateNumber;
        Brand = brand;
        Model = model;
        ManufactureYear = manufactureYear;
        EnginePowerHp = enginePowerHp;
        UsagePurpose = usagePurpose;
    }

    public Guid CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public string PlateNumber { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int ManufactureYear { get; private set; }
    public int EnginePowerHp { get; private set; }

    // ADR-057: Kullanım amacı BEYANI (Kasko/Trafik fiyatlamasını etkiler). Nullable'dır: bu alan
    // eklenmeden önce kaydedilmiş araçlarda null kalır ve o araçlardan üretilmiş ESKİ tekliflere yeni
    // faktör geriye dönük UYGULANMAZ. Yeni araç kaydında beyan zorunludur (varsayılan atanmaz).
    public VehicleUsage? UsagePurpose { get; private set; }

    public void UpdateDetails(
        string plateNumber,
        string brand,
        string model,
        int manufactureYear,
        int enginePowerHp,
        VehicleUsage? usagePurpose = null)
    {
        PlateNumber = plateNumber;
        Brand = brand;
        Model = model;
        ManufactureYear = manufactureYear;
        EnginePowerHp = enginePowerHp;
        UsagePurpose = usagePurpose;
    }
}
