using SigortaPro.Domain.Common;

namespace SigortaPro.Domain.Entities;

public class Coverage : BaseEntity
{
    protected Coverage()
    {
    }

    public Coverage(Guid insuranceProductId, string name, string? description, decimal defaultLimit)
    {
        Id = Guid.NewGuid();
        InsuranceProductId = insuranceProductId;
        Name = name;
        Description = description;
        DefaultLimit = defaultLimit;
    }

    public Guid InsuranceProductId { get; private set; }
    public InsuranceProduct? InsuranceProduct { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal DefaultLimit { get; private set; }
}
