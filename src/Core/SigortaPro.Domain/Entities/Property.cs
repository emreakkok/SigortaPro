using SigortaPro.Domain.Common;

namespace SigortaPro.Domain.Entities;

// TASKS.md'de tanımlı domain terimi ("konut risk objesi") korunuyor; CA1716 (VB.NET 'Property' anahtar sözcüğü çakışması) bilinçli olarak suppress edilmiştir.
#pragma warning disable CA1716
public class Property : BaseEntity, IAggregateRoot
#pragma warning restore CA1716
{
    protected Property()
    {
    }

    public Property(Guid customerId, Address address, int buildingAge, int squareMeters, int earthquakeZone)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        Address = address;
        BuildingAge = buildingAge;
        SquareMeters = squareMeters;
        EarthquakeZone = earthquakeZone;
    }

    public Guid CustomerId { get; private set; }
    public Customer? Customer { get; private set; }
    public Address Address { get; private set; } = null!;
    public int BuildingAge { get; private set; }
    public int SquareMeters { get; private set; }
    public int EarthquakeZone { get; private set; }

    public void UpdateDetails(Address address, int buildingAge, int squareMeters, int earthquakeZone)
    {
        Address = address;
        BuildingAge = buildingAge;
        SquareMeters = squareMeters;
        EarthquakeZone = earthquakeZone;
    }
}
