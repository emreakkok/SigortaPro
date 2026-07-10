using SigortaPro.Domain.Common;

namespace SigortaPro.Domain.Entities;

public class Vehicle : BaseEntity, IAggregateRoot
{
    protected Vehicle()
    {
    }

    public Vehicle(Guid customerId, string plateNumber, string brand, string model, int manufactureYear, int enginePowerHp)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        PlateNumber = plateNumber;
        Brand = brand;
        Model = model;
        ManufactureYear = manufactureYear;
        EnginePowerHp = enginePowerHp;
    }

    public Guid CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public string PlateNumber { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int ManufactureYear { get; private set; }
    public int EnginePowerHp { get; private set; }

    public void UpdateDetails(string plateNumber, string brand, string model, int manufactureYear, int enginePowerHp)
    {
        PlateNumber = plateNumber;
        Brand = brand;
        Model = model;
        ManufactureYear = manufactureYear;
        EnginePowerHp = enginePowerHp;
    }
}
